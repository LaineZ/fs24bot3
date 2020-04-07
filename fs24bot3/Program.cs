using Qmmands;
using System;
using Serilog;
using SQLite;
using System.Text;
using NetIRC;
using System.Threading.Tasks;
using NetIRC.Connection;
using NetIRC.Messages;

namespace fs24bot3
{
    class Program
    {
        private static SQLiteConnection connection;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .MinimumLevel.Verbose()
            .CreateLogger();
            Console.OutputEncoding = Encoding.Unicode;

            Log.Information("fs24_bot3 has started");

            Configuration.LoadConfiguration();

            connection = new SQLiteConnection("fsdb.sqlite");
            connection.CreateTable<Models.SQL.UserStats>();
            connection.CreateTable<Models.SQL.CustomUserCommands>();
            connection.CreateTable<Models.SQL.Tag>();
            connection.CreateTable<Models.SQL.Item>();
            connection.CreateTable<Models.SQL.Tags>();
            connection.CreateTable<Models.SQL.Ignore>();

            Shop.Init(connection);

            // creating ultimate inventory by @Fingercomp
            connection.Execute("CREATE TABLE IF NOT EXISTS Inventory (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Item NOT NULL REFERENCES Item (Name) ON DELETE CASCADE ON UPDATE CASCADE, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Nick, Item))");

            

            _service.AddModule<GenericCommandsModule>();
            _service.AddModule<SystemCommandModule>();
            _service.AddModule<InventoryCommandsModule>();
            _service.AddModule<InternetCommandsModule>();

            using (var client = new Client(new User(Configuration.name, "NetIRC"), new TcpClientConnection()))
            {
                client.OnRawDataReceived += Client_OnRawDataReceived;
                client.EventHub.PrivMsg += EventHub_PrivMsg;
                client.EventHub.RplWelcome += Client_OnWelcome;
                Log.Information("Connecting to: {0}:{1}", Configuration.network, (int)Configuration.port);
                Task.Run(() => client.ConnectAsync(Configuration.network, (int)Configuration.port));

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
        }

        private async static void EventHub_PrivMsg(Client client, IRCMessageEventArgs<PrivMsgMessage> e)
        {
            Log.Verbose(e.IRCMessage.From);
            var query = connection.Table<Models.SQL.UserStats>().Where(v => v.Nick.Equals(e.IRCMessage.From));

                    if (query.Count() <= 0)
                    {
                        Log.Warning("User {0} not found in database", e.IRCMessage.From);



                        var user = new Models.SQL.UserStats()
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

                    IResult result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(e.IRCMessage, client, connection));
                    if (result is FailedResult failedResult)
                    {
                        await Core.CustomCommandProcessor.ProcessCmd(e.IRCMessage, client, connection);
                        if (!(result is CommandNotFoundResult _))
                        {
                            await client.SendAsync( new PrivMsgMessage(e.IRCMessage.To, failedResult.Reason + " command line: " + output));
                         }       
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
                    break;
                default:
                    break;
            }
        }

        private static readonly CommandService _service = new CommandService();
    }
}
