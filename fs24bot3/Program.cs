using Qmmands;
using System;
using Serilog;
using SQLite;
using System.Text;
using NetIRC;
using System.Threading.Tasks;
using NetIRC.Connection;
using NetIRC.Messages;
using System.Threading;
using fs24bot3.Models;
using System.IO;
using VkNet;
using VkNet.Model;
using VkNet.Enums.Filters;

namespace fs24bot3
{
    class Program
    {
        private static SQLiteConnection connection = new SQLiteConnection("fsdb.sqlite");
        private static SQLiteConnection CacheConnection;
        private static VkApi vk;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .MinimumLevel.Verbose()
            .CreateLogger();
            Console.OutputEncoding = Encoding.Unicode;
            Log.Information("fs24_bot3 has started");

            if (File.Exists("fscache.sqlite"))
            {
                Log.Information("Cleaning up cache!");
                File.Delete("fscache.sqlite");
            }

            CacheConnection = new SQLiteConnection("fscache.sqlite");

            Configuration.LoadConfiguration();
            connection.CreateTable<SQL.UserStats>();
            connection.CreateTable<SQL.CustomUserCommands>();
            connection.CreateTable<SQL.Tag>();
            connection.CreateTable<SQL.Item>();
            connection.CreateTable<SQL.Tags>();
            connection.CreateTable<SQL.Ignore>();
            CacheConnection.CreateTable<SQL.HttpCache>();
            CacheConnection.Close();
            CacheConnection.Dispose();

            Shop.Init(connection);

            // creating ultimate inventory by @Fingercomp
            connection.Execute("CREATE TABLE IF NOT EXISTS Inventory (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Item NOT NULL REFERENCES Item (Name) ON DELETE CASCADE ON UPDATE CASCADE, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Nick, Item))");
            connection.Execute("CREATE TABLE IF NOT EXISTS LyricsCache (track TEXT, artist TEXT, lyrics TEXT, addedby TEXT, PRIMARY KEY (track, artist))");


            _service.AddModule<GenericCommandsModule>();
            _service.AddModule<SystemCommandModule>();
            _service.AddModule<InventoryCommandsModule>();
            _service.AddModule<InternetCommandsModule>();
            _service.AddModule<NetstalkingCommandsModule>();


            Log.Information("Logging with vkapi...");
            vk = new VkApi();

            try
            {
            vk.Authorize(new ApiAuthParams
            {
                ApplicationId = ulong.Parse(Configuration.vkApiId),
                Login = Configuration.vkLogin,
                Password = Configuration.vkPassword,
                Settings = Settings.All,
            });
            }
            catch (Exception)
            {
                Log.Error("Failed to load vk api key that means you cannot use vk api functions, sorry...");
            }

            using (var client = new Client(new NetIRC.User(Configuration.name, "Sopli IRC 3.0"), new TcpClientConnection()))
            {
                client.OnRawDataReceived += Client_OnRawDataReceived;
                client.EventHub.PrivMsg += EventHub_PrivMsg;
                client.EventHub.RplWelcome += Client_OnWelcome;

                Log.Information("Connecting to: {0}:{1}", Configuration.network, (int)Configuration.port);
                Task.Run(() => client.ConnectAsync(Configuration.network, (int)Configuration.port));
                (new Thread(() => {
                    Log.Information("Thread started!");
                    while (true)
                    {
                        Thread.Sleep(5000);
                        Shop.Update(connection);
                    }
                })).Start();

                try
                {
                    Console.ReadKey();
                }
                catch (Exception)
                {
                    Console.Read();
                }
            }
        }

        private async static void Client_OnWelcome(Client client, IRCMessageEventArgs<RplWelcomeMessage> e)
        {
            await client.SendRaw("JOIN " + Configuration.channel);
            await client.SendAsync(new PrivMsgMessage("NickServ", "identify " + Configuration.nickservPass));
        }

        private async static void EventHub_PrivMsg(Client client, IRCMessageEventArgs<PrivMsgMessage> e)
        {
            var query = connection.Table<SQL.UserStats>().Where(v => v.Nick.Equals(e.IRCMessage.From));
            var queryIfExt = connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(e.IRCMessage.From)).Count();


            if (queryIfExt <= 0)
            {
                if (query.Count() <= 0 && e.IRCMessage.From != Configuration.name)
                {
                    Log.Warning("User {0} not found in database", e.IRCMessage.From);

                    var user = new SQL.UserStats()
                    {
                        Nick = e.IRCMessage.From,
                        Admin = 0,
                        AdminPassword = "changeme",
                        Level = 1,
                        Xp = 0,
                        Need = 300,
                    };

                    connection.Insert(user);
                }
                else
                {
                    UserOperations usr = new UserOperations(e.IRCMessage.From, connection);
                    bool newLevel = usr.IncreaseXp(e.IRCMessage.Message.Length + 1);
                    if (newLevel)
                    {
                        var random = new Random();
                        int index = random.Next(Shop.ShopItems.Count);
                        usr.AddItemToInv(Shop.ShopItems[index].Slug, 1);
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, e.IRCMessage.From + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name));
                    }
                }

                if (!CommandUtilities.HasPrefix(e.IRCMessage.Message.TrimEnd(), '@', out string output))
                    return;

                DateTime firstTime = DateTime.Now;
                var result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(e.IRCMessage, client, connection, CacheConnection, vk));
                DateTime elapsed = DateTime.Now;

                Log.Verbose("Perf: {0} ms", elapsed.Subtract(firstTime).TotalMilliseconds);

                switch (result)
                {
                    case ChecksFailedResult err:
                        var errStr = new StringBuilder();

                        foreach (var (check, error) in err.FailedChecks)
                        {
                            errStr.Append(error.Reason);
                        }
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Требования не выполнены: {errStr.ToString()}"));
                        break;
                    case TypeParseFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Ошибка типа в `{err.Parameter}`"));
                        break;
                    case ArgumentParseFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Ошибка парсера: `{err.Reason}`"));
                        break;
                    case OverloadsFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, "Для данной команды нету перегрузки!"));
                        break;
                    case CommandNotFoundResult _:
                        await Core.CustomCommandProcessor.ProcessCmd(e.IRCMessage, client, connection);
                        break;
                    case ExecutionFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Ошибка при выполнении команды: {err.Reason}: `{err.Exception.Message}`"));
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, err.Exception.StackTrace));
                        break;
                }
            }
            else
            {
                Log.Verbose("User tried send message but it ignored!");
            }
        }

        private static async void Client_OnRawDataReceived(Client client, string rawData)
        {
            Log.Information(rawData);
            var ircMessage = new ParsedIRCMessage(rawData);
            switch (ircMessage.Command)
            {
                case "KICK":
                    Log.Warning("I've got kick from {0} rejoining...", ircMessage.Prefix);
                    await client.SendRaw("JOIN " + Configuration.channel);
                    await client.SendAsync(new PrivMsgMessage(Configuration.channel, "За что?"));
                    break;
                default:
                    break;
            }
        }

        private static readonly CommandService _service = new CommandService();
    }
}
