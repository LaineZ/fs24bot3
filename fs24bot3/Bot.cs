using fs24bot3.BotSystems;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fs24bot3
{
    public class Bot
    {

        public SQLiteConnection Connection = new SQLiteConnection("fsdb.sqlite");
        public List<ParsedIRCMessage> MessageBus = new List<ParsedIRCMessage>();
        public Core.CustomCommandProcessor CustomCommandProcessor;
        public readonly CommandService Service = new CommandService();
        public Client BotClient { get; }
        readonly HttpTools http = new HttpTools();
        public Shop Shop;
        public Songame SongGame;
        public int Tickrate = 15000;
        public Bot()
        {
            Service.AddModule<GenericCommandsModule>();
            Service.AddModule<SystemCommandModule>();
            Service.AddModule<InventoryCommandsModule>();
            Service.AddModule<InternetCommandsModule>();
            Service.AddModule<NetstalkingCommandsModule>();
            Service.AddModule<CustomCommandsModule>();
            Service.AddModule<StatCommandModule>();
            Service.AddModule<BandcampCommandsModule>();
            Service.AddModule<TranslateCommandModule>();
            Service.AddModule<FishCommandsModule>();

            Core.Database.InitDatabase(Connection);
            BotClient = new Client(new User(Configuration.name, "Sopli IRC 3.0"), new TcpClientConnection());
            CustomCommandProcessor = new Core.CustomCommandProcessor(BotClient, Connection, MessageBus);

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
                            string ch = item.Channel ?? Configuration.channel;
                            await SendMessage(ch, $"{item.Nick}: {item.Message}!");
                            Connection.Delete(item);
                        }
                    }
                }
            }).Start();
        }

        public void ProccessInfinite()
        {
            // start shop
            Shop = new Shop(this);
            SongGame = new Songame(Connection);

            while (true)
            {
                Thread.Sleep(Tickrate);
                var query = Connection.Table<SQL.UserStats>();
                foreach (var users in query)
                {
                    var onTick = new EventProcessors.OnTick(users.Nick, Connection);
                    onTick.UpdateUserPaydays(Shop);
                    onTick.RemoveLevelOneAccs();
                }
                Shop.UpdateShop();
                if (DateTime.Now.Minute == 0)
                {
                    if (MessageBus.Any())
                    {
                        Log.Verbose("Cleaning messages!");
                        MessageBus.Clear();
                    }
                }
            }
        }
        public void MessageTrigger(string nick, string target, ParsedIRCMessage message)
        {
            var queryIfExt = Connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(nick)).Count();
            if (queryIfExt <= 0)
            {
                MessageBus.Add(message);
                new Thread(() =>
                {
                    if (target != BotClient.User.Nick)
                    {
                        EventProcessors.OnMsgEvent events = new EventProcessors.OnMsgEvent(BotClient, nick, target, message.Trailing.Trim(), Connection);
                        events.DestroyWallRandomly(Shop);
                        events.LevelInscrease(Shop);
                        events.GiveWaterFromPumps(Shop);
                    }
                }).Start();
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            if (!message.Contains("\n"))
            {
                if (message.Length > 250)
                {
                    foreach (var slice in Core.MessageUtils.SplitMessage(message, 450))
                    {
                        await BotClient.SendAsync(new PrivMsgMessage(channel, slice));
                    }
                }
                else
                {
                    await BotClient.SendAsync(new PrivMsgMessage(channel, message));
                }
            }
            else
            {
                int count = 0;
                foreach (string outputstr in message.Split("\n"))
                {
                    if (!string.IsNullOrWhiteSpace(outputstr))
                    {
                        await BotClient.SendAsync(new PrivMsgMessage(channel, outputstr));
                        count++;
                    }
                    if (count > 4)
                    {
                        string link = await http.UploadToTrashbin(Core.MessageUtils.StripIRC(message), "addplain");
                        await BotClient.SendAsync(new PrivMsgMessage(channel, "Полный вывод здесь: " + link));
                        break;
                    }
                }
            }
        }
    }
}
