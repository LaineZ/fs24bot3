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
using System.Text;
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
                new Thread(() =>
                {
                    if (target != BotClient.User.Nick)
                    {
                        EventProcessors.OnMsgEvent events = new EventProcessors.OnMsgEvent(BotClient, nick, target, message.Trailing.Trim(), Connection);
                        events.DestroyWallRandomly(Shop);
                        events.LevelInscrease(Shop);
                    }
                }).Start();
            }

            MessageBus.Add(message);
        }


        private string LimitByteLength(String input, Int32 maxLength)
        {
            return new String(input
                .TakeWhile((c, i) =>
                    Encoding.UTF8.GetByteCount(input.Substring(0, i + 1)) <= maxLength)
                .ToArray());
        }

        private List<string> SplitMessage(string value, int chunkLength)
        {
            if (value.Length < chunkLength) { return new List<string>() { value }; }

            List<string> splitted = new List<string>();

            byte[] stringByte = Encoding.UTF8.GetBytes(value);

            byte[][] chunks = stringByte.Select((value, index) =>
            new { PairNum = Math.Floor(index / (decimal)chunkLength), value })
                .GroupBy(pair => pair.PairNum)
                .Select(grp => grp.Select(g => g.value).ToArray())
                .ToArray();

            foreach (var chunk in chunks)
            {
                string converted = Encoding.UTF8.GetString(chunk, 0, chunk.Length);
                splitted.Add(LimitByteLength(converted, chunkLength));
            }
            return splitted;
        }

        public async Task SendMessage(string channel, string message)
        {
            List<string> msgLines = message.Split("\n").ToList();
            int count = 0;

            foreach (string outputstr in msgLines)
            {
                if (!string.IsNullOrWhiteSpace(outputstr))
                {
                    foreach (var msg in SplitMessage(outputstr, 255))
                    {
                        await BotClient.SendAsync(new PrivMsgMessage(channel, msg));
                        count++;

                        if (count > 4)
                        {
                            string link = await http.UploadToTrashbin(Core.MessageUtils.StripIRC(message), "addplain");
                            await BotClient.SendAsync(new PrivMsgMessage(channel, "Полный вывод здесь: " + link));
                            return;
                        }
                    }
                }
            }
        }
    }
}
