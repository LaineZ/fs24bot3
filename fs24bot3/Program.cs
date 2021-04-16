using fs24bot3.Commands;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
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
        private static readonly SQLiteConnection Connection = new SQLiteConnection("fsdb.sqlite");
        private static readonly List<ParsedIRCMessage> MessageBus = new List<ParsedIRCMessage>();
        private static Core.CustomCommandProcessor CustomCommandProcessor;

        private static Client client;

        static async private void RandomLyics(Client client)
        {
            var query = Connection.Table<SQL.LyricsCache>().ToList();

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

            Log.Information("fs24_bot 3 by 140bpmdubstep");

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.OutputEncoding = Encoding.Unicode;
            }

            Core.Database.InitDatabase(Connection);

            _service.AddModule<GenericCommandsModule>();
            _service.AddModule<SystemCommandModule>();
            _service.AddModule<InventoryCommandsModule>();
            _service.AddModule<InternetCommandsModule>();
            _service.AddModule<NetstalkingCommandsModule>();
            _service.AddModule<FishCommandsModule>();
            _service.AddModule<CustomCommandsModule>();
            _service.AddModule<StatCommandModule>();

            client = new Client(new User(Configuration.name, "Sopli IRC 3.0"), new TcpClientConnection());

            client.OnRawDataReceived += Client_OnRawDataReceived;
            client.OnIRCMessageParsed += Client_OnIRCMessageParsed;
            client.RegistrationCompleted += Client_OnRegister;

            Log.Information("Connecting to: {0}:{1}", Configuration.network, (int)Configuration.port);

            Task.Run(() => client.ConnectAsync(Configuration.network, (int)Configuration.port));

            new Thread(async () =>
            {
                Log.Information("Reminds thread started!");
                while (true)
                {
                    Thread.Sleep(1000);
                    var query = Connection.Table<SQL.Reminds>();

                    foreach (var item in query)
                    {
                        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        dtDateTime = dtDateTime.AddSeconds(item.RemindDate).ToLocalTime();
                        if (dtDateTime <= DateTime.Now)
                        {
                            await client.SendAsync(new PrivMsgMessage(Configuration.channel, $"{item.Nick}: {item.Message}!"));
                            Connection.Delete(item);
                        }

                    }
                }
            }).Start();

            CustomCommandProcessor = new Core.CustomCommandProcessor(client, Connection, MessageBus);

            Log.Information("First init is ok!");
            while (true)
            {
                Thread.Sleep(Shop.Tickrate);
                var query = Connection.Table<SQL.UserStats>();
                foreach (var users in query)
                {
                    var onTick = new EventProcessors.OnTick(users.Nick, Connection);
                    onTick.UpdateUserPaydays();
                }
                Shop.UpdateShop();
                if (DateTime.Now.Minute == 0)
                {
                    Log.Verbose("Cleaning messages!");
                    MessageBus.Clear();
                }
            }
        }

        private async static void Client_OnIRCMessageParsed(Client client, ParsedIRCMessage message)
        {
            if (message.IRCCommand == IRCCommand.PRIVMSG)
            {
                string nick = message.Prefix.From;
                string target = message.Parameters[0];

                if (message.Parameters[0] == client.User.Nick)
                {
                    target = message.Prefix.From;
                }

                var queryIfExt = Connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(nick)).Count();

                if (queryIfExt <= 0)
                {
                    MessageBus.Add(message);
                    new Thread(() =>
                    {
                        if (target != client.User.Nick)
                        {
                            EventProcessors.OnMsgEvent events = new EventProcessors.OnMsgEvent(client, nick, target, message.Trailing.Trim(), Connection);
                            events.DestroyWallRandomly();
                            events.LevelInscrease();
                            events.GiveWaterFromPumps();
                        }
                    }).Start();
                }

                if (!CommandUtilities.HasPrefix(message.Trailing.TrimEnd().TrimStart('p'), '@', out string output))
                    return;

                var result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(target, message, client, Connection, MessageBus, message.Trailing.StartsWith("p")));
                switch (result)
                {
                    case ChecksFailedResult err:
                        var errStr = new StringBuilder();

                        foreach (var (check, error) in err.FailedChecks)
                        {
                            errStr.Append(error.FailureReason);
                        }
                        await client.SendAsync(new PrivMsgMessage(target, $"Требования не выполнены: {errStr}"));
                        break;
                    case TypeParseFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, $"Ошибка типа в `{err.Parameter}` необходимый тип `{err.Parameter.Type}` вы же ввели `{err.Value.GetType()}`"));
                        break;
                    case ArgumentParseFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, $"Ошибка парсера: `{err.FailureReason}`"));
                        break;
                    case OverloadsFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, "Для данной команды нету перегрузки!"));
                        break;
                    case CommandNotFoundResult err:
                        bool customSuccess = await CustomCommandProcessor.ProcessCmd(nick, target, message.Trailing.TrimEnd());
                        break;
                    case CommandExecutionFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, $"{IrcColors.Red}Ошибка: {err.Exception.Message}"));
                        await client.SendAsync(new PrivMsgMessage(target, err.Exception.StackTrace));
                        break;
                }
            }

            if (message.IRCCommand == IRCCommand.ERROR)
            {
                Log.Error("Connection closed due to error... Exiting");
                Environment.Exit(1);
            }

            if (message.IRCCommand == IRCCommand.KICK)
            {
                if (message.Parameters[1] == Configuration.name)
                {
                    Log.Warning("I've got kick from {0} rejoining...", message.Prefix);
                    await client.SendRaw("JOIN " + Configuration.channel);
                    await client.SendAsync(new PrivMsgMessage(Configuration.channel, "За что?"));
                }
            }
        }

        private async static void Client_OnRegister(object sender, EventArgs _)
        {
            await client.SendRaw("JOIN " + Configuration.channel);
            await client.SendAsync(new PrivMsgMessage("NickServ", "identify " + Configuration.nickservPass));
            RandomLyics(client);
        }

        private static void Client_OnRawDataReceived(Client client, string rawData)
        {
            Log.Information(rawData);
        }

        private static readonly CommandService _service = new CommandService();
    }
}
