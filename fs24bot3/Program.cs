using fs24bot3.Commands;
using fs24bot3.Models;
using NetIRC;
using NetIRC.Connection;
using NetIRC.Messages;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fs24bot3
{
    class Program
    {
        private static readonly SQLiteConnection connection = new SQLiteConnection("fsdb.sqlite");
        private static List<PrivMsgMessage> MessageBus = new List<PrivMsgMessage>();
        private static Core.CustomCommandProcessor CustomCommandProcessor;

        static async void RandomLyics(Client client)
        {
            var query = connection.Table<SQL.LyricsCache>().ToList();

            if (query.Count > 0)
            {
                Random rand = new Random();
                string[] lyrics = query[rand.Next(0, query.Count - 1)].Lyrics.Split("\n");
                int baseoffset = rand.Next(0, lyrics.Length - 1);
                string outputmsg = "";

                for (int i = 0; i < rand.Next(1, 5); i++)
                {
                    if (lyrics.Length > baseoffset + i) { outputmsg += " " + lyrics[baseoffset + i].Trim(); }
                }

                await client.SendAsync(new PrivMsgMessage(Configuration.channel, outputmsg));
            }
        }

        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.ControlledBy(Configuration.LoggerSw)
            .CreateLogger();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.OutputEncoding = Encoding.Unicode;
            }

            Core.Database.InitDatabase(connection);

            _service.AddModule<GenericCommandsModule>();
            _service.AddModule<SystemCommandModule>();
            _service.AddModule<InventoryCommandsModule>();
            _service.AddModule<InternetCommandsModule>();
            _service.AddModule<NetstalkingCommandsModule>();
            _service.AddModule<FishCommandsModule>();
            _service.AddModule<CustomCommandsModule>();
            using var client = new Client(new User(Configuration.name, "Sopli IRC 3.0"), new TcpClientConnection());

            client.OnRawDataReceived += Client_OnRawDataReceived;
            client.EventHub.PrivMsg += EventHub_PrivMsg;
            client.EventHub.RplWelcome += Client_OnWelcome;

            Log.Information("Connecting to: {0}:{1}", Configuration.network, (int)Configuration.port);

            Task.Run(() => client.ConnectAsync(Configuration.network, (int)Configuration.port));

            new Thread(async () =>
            {
                Log.Information("Reminds thread started!");
                while (true)
                {
                    Thread.Sleep(1000);
                    var query = connection.Table<SQL.Reminds>();

                    foreach (var item in query)
                    {
                        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        dtDateTime = dtDateTime.AddSeconds(item.RemindDate).ToLocalTime();
                        if (dtDateTime <= DateTime.Now)
                        {
                            await client.SendAsync(new PrivMsgMessage(Configuration.channel, $"{item.Nick}: {item.Message}!"));
                            connection.Delete(item);
                        }

                    }
                }
            }).Start();

            CustomCommandProcessor = new Core.CustomCommandProcessor(client, connection, MessageBus);

            Log.Information("Running in loop!");
            while (true)
            {
                Thread.Sleep(Shop.Tickrate);
                Shop.Update(connection);
                
                if (DateTime.Now.Minute == 0)
                {
                    Log.Verbose("Cleaning messages!");
                    MessageBus.Clear();
                }
            }
        }

        private async static void Client_OnWelcome(Client client, IRCMessageEventArgs<RplWelcomeMessage> e)
        {
            await client.SendRaw("JOIN " + Configuration.channel);
            await client.SendAsync(new PrivMsgMessage("NickServ", "identify " + Configuration.nickservPass));
            RandomLyics(client);
        }

        private async static void EventHub_PrivMsg(Client client, IRCMessageEventArgs<PrivMsgMessage> e)
        {
            var query = connection.Table<SQL.UserStats>().Where(v => v.Nick.Equals(e.IRCMessage.From));
            var queryIfExt = connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(e.IRCMessage.From)).Count();


            if (queryIfExt <= 0)
            {
                MessageBus.Add(e.IRCMessage);
                new Thread(() =>
                {
                    if (e.IRCMessage.To != Configuration.name)
                    {
                        UserOperations usr = new UserOperations(e.IRCMessage.From, connection);
                        usr.CreateAccountIfNotExist();
                        usr.SetLastMessage();
                        bool newLevel = usr.IncreaseXp(e.IRCMessage.Message.Length * new Random().Next(1, 3) + 1);
                        if (newLevel)
                        {
                            var random = new Random();
                            int index = random.Next(Shop.ShopItems.Count);
                            usr.AddItemToInv(Shop.ShopItems[index].Slug, 1);
                            client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, e.IRCMessage.From + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name));
                        }
                    }
                    else
                    {
                        Log.Verbose("Message was sent in PM, Level increasing IGNORED!!!");
                    }
                }).Start();
            }
            else
            {
                Log.Verbose("User tried send message but it ignored!");
            }


            if (!CommandUtilities.HasPrefix(e.IRCMessage.Message.TrimEnd(), '@', out string output))
                return;

            var result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(e.IRCMessage, client, connection, MessageBus));
            switch (result)
            {
                case ChecksFailedResult err:
                    var errStr = new StringBuilder();

                    foreach (var (check, error) in err.FailedChecks)
                    {
                        errStr.Append(error.Reason);
                    }
                    await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Требования не выполнены: {errStr}"));
                    break;
                case TypeParseFailedResult err:
                    await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Ошибка типа в `{err.Parameter}` необходимый тип `{err.Parameter.Type}` вы же ввели `{err.Value.GetType()}`"));
                    break;
                case ArgumentParseFailedResult err:
                    await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"Ошибка парсера: `{err.Reason}`"));
                    break;
                case OverloadsFailedResult err:
                    await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, "Для данной команды нету перегрузки!"));
                    break;
                case CommandNotFoundResult err:
                    bool customSuccess = await CustomCommandProcessor.ProcessCmd(e.IRCMessage);
                    break;
                case ExecutionFailedResult err:
                    await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"{IrcColors.Red}Ошибка: {err.Exception.Message}"));
                    await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, err.Exception.StackTrace));
                    break;
            }
        }

        private static async void Client_OnRawDataReceived(Client client, string rawData)
        {
            Log.Information(rawData);
            var ircMessage = new ParsedIRCMessage(rawData);
            switch (ircMessage.Command)
            {
                case "KICK":
                    if (ircMessage.Parameters[1] == Configuration.name)
                    {
                        Log.Warning("I've got kick from {0} rejoining...", ircMessage.Prefix);
                        await client.SendRaw("JOIN " + Configuration.channel);
                        await client.SendAsync(new PrivMsgMessage(Configuration.channel, "За что?"));
                    }
                    break;
                case "ERROR":
                    Log.Error("Connection closed due to error... Exiting");
                    Environment.Exit(1);
                    break;
                default:
                    break;
            }
        }

        private static readonly CommandService _service = new CommandService();
    }
}
