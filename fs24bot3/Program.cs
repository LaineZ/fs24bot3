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
using Newtonsoft.Json;
using System.Collections.Generic;

namespace fs24bot3
{
    class Program
    {
        private static SQLiteConnection connection = new SQLiteConnection("fsdb.sqlite");
        private static VkApi vk;
        private static List<PrivMsgMessage> MessageBus = new List<PrivMsgMessage>();

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .MinimumLevel.ControlledBy(Configuration.LoggerSw)
            .CreateLogger();
            Console.OutputEncoding = Encoding.Unicode;

            Core.Database.InitDatabase(connection);

            _service.AddModule<GenericCommandsModule>();
            _service.AddModule<SystemCommandModule>();
            _service.AddModule<InventoryCommandsModule>();
            _service.AddModule<InternetCommandsModule>();
            _service.AddModule<NetstalkingCommandsModule>();
            _service.AddModule<FishCommandsModule>();

            vk = new HttpTools().LogInVKAPI();

            using var client = new Client(new User(Configuration.name, "Sopli IRC 3.0"), new TcpClientConnection());

            client.OnRawDataReceived += Client_OnRawDataReceived;
            client.EventHub.PrivMsg += EventHub_PrivMsg;
            client.EventHub.RplWelcome += Client_OnWelcome;

            Log.Information("Connecting to: {0}:{1}", Configuration.network, (int)Configuration.port);
            Task.Run(() => client.ConnectAsync(Configuration.network, (int)Configuration.port));
            new Thread(() =>
            {
                Log.Information("Thread started!");
                while (true)
                {
                    Thread.Sleep(Shop.Tickrate);
                    Shop.Update(connection);
                }
            }).Start();

            try
            {
                Console.ReadKey();
            }
            catch (Exception)
            {
                Console.Read();
            }
        }

        private async static void Client_OnWelcome(Client client, IRCMessageEventArgs<RplWelcomeMessage> e)
        {
            await client.SendRaw("JOIN " + Configuration.channel);
            await client.SendAsync(new PrivMsgMessage("NickServ", "identify " + Configuration.nickservPass));
        }

        private async static void EventHub_PrivMsg(Client client, IRCMessageEventArgs<PrivMsgMessage> e)
        {
            if (MessageBus.Count < 1000)
            {
                MessageBus.Add(e.IRCMessage);
            }
            else
            {
                MessageBus.Clear();
            }

            var query = connection.Table<SQL.UserStats>().Where(v => v.Nick.Equals(e.IRCMessage.From));
            var queryIfExt = connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(e.IRCMessage.From)).Count();


            if (queryIfExt <= 0)
            {
                if (!CommandUtilities.HasPrefix(e.IRCMessage.Message.TrimEnd(), '@', out string output))
                    return;

                var result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(e.IRCMessage, client, connection, vk));
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
                        bool customSuccess = await Core.CustomCommandProcessor.ProcessCmd(e.IRCMessage, client, connection, MessageBus);
                        break;
                    case ExecutionFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, $"{err.Reason}: `{err.Exception.Message}`"));
                        await client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, err.Exception.StackTrace));
                        break;
                }

                (new Thread(() =>
                {
                    Core.HourstatHelper.InsertMessage(e.IRCMessage.Message, DateTime.Now);

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
                            client.SendAsync(new PrivMsgMessage(e.IRCMessage.To, e.IRCMessage.From + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name));
                        }
                    }
                })).Start();
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
                    if (ircMessage.Parameters[1] == Configuration.name)
                    {
                        Log.Warning("I've got kick from {0} rejoining...", ircMessage.Prefix);
                        await client.SendRaw("JOIN " + Configuration.channel);
                        await client.SendAsync(new PrivMsgMessage(Configuration.channel, "За что?"));
                    }
                    break;
                case "ERROR":
                    Log.Fatal("Connection closed due to error");
                    break;
                default:
                    break;
            }
        }

        private static readonly CommandService _service = new CommandService();
    }
}
