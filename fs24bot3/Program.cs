﻿using IrcClientCore;
using IrcClientCore.Commands;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Serilog;
using SQLite;
using System.IO;
using Newtonsoft.Json;
using SQLiteNetExtensions.Extensions;
using System.Text;

namespace fs24bot3
{
    class Program
    {

        public static ObservableCollection<Message> _channelBuffers = null;
        public static Irc _socket = null;
        public static CommandManager handler = null;
        private static string _currentChannel;
        private static bool connected = false;
        private static SQLiteConnection connection;

        internal static void SwitchChannel(string channel)
        {
            if (_channelBuffers != null)
            {
                _channelBuffers.CollectionChanged -= ChannelBuffersOnCollectionChanged;
            }
            _currentChannel = channel;

            if (channel == "")
            {
                _channelBuffers = _socket.ChannelList.ServerLog.Buffers as ObservableCollection<Message>;
            }
            else
            {
                _channelBuffers = _socket.ChannelList[_currentChannel].Buffers as ObservableCollection<Message>;
            }

            PrintMessages(_channelBuffers);

            if (_channelBuffers != null) _channelBuffers.CollectionChanged += ChannelBuffersOnCollectionChanged;
        }

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.ColoredConsole()
            .MinimumLevel.Verbose()
            .CreateLogger();
            Console.OutputEncoding = Encoding.Unicode;

            Log.Information("fs24_bot3 has started");
            IrcServer server = new IrcServer();

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

            server.Hostname = Configuration.network;
            server.Name = "irc network";
            server.Username = Configuration.name;
            server.Channels = Configuration.channel;
            server.ShouldReconnect = Configuration.reconnect;
            server.Ssl = Configuration.ssl;
            server.Port = Convert.ToInt32(Configuration.port);


            _socket = new IrcSocket(server);
            _socket.Connect();

            handler = _socket.CommandManager;

            SwitchChannel("");
            _service.AddModule<GenericCommandsModule>();
            _service.AddModule<SystemCommandModule>();
            _service.AddModule<InventoryCommandsModule>();
            _service.AddModule<InternetCommandsModule>();

            while (!_socket.ReadOrWriteFailed)
            {
                System.Threading.Thread.Sleep(1000);
                //Shop.Update(connection);
            }

            Console.WriteLine("Socket connection lost....");
        }

        private static void ChannelBuffersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            PrintMessages(args.NewItems.OfType<Message>());
        }

        private static readonly CommandService _service = new CommandService();

        private static async void PrintMessages(IEnumerable<Message> messages)
        {

            foreach (var message in messages)
            {
                if (!connected && message.Text.Split(" ").Length > 1 && message.Text.Split(" ")[1] == "366" && message.User == "*")
                {
                    SwitchChannel(Configuration.channel);
                    connected = true;
                }

                var queryIfExt = connection.Table<Models.SQL.Ignore>().Where(v => v.Username.Equals(message.User));

                if (connected && message.User != "*" && queryIfExt.Count() <= 0 && message.User != Configuration.name)
                {
                    var query = connection.Table<Models.SQL.UserStats>().Where(v => v.Nick.Equals(message.User));

                    if (query.Count() <= 0)
                    {
                        Log.Warning("User {0} not found in database", message.User);



                        var user = new Models.SQL.UserStats()
                        {
                            Nick = message.User,
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
                        UserOperations usr = new UserOperations(message.User, connection);
                        bool newLevel = usr.IncreaseXp(message.Text.Length + 1);
                        if (newLevel)
                        {
                            var random = new Random();
                            int index = random.Next(Shop.ShopItems.Count);
                            usr.AddItemToInv(Shop.ShopItems[index].Slug, 1);
                            _socket.SendMessage(_currentChannel, message.User + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name);
                        }
                    }

                    if (!CommandUtilities.HasPrefix(message.Text.TrimEnd(), '@', out string output))
                        return;

                    IResult result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(message, _socket, connection));
                    if (result is FailedResult failedResult)
                    {
                        Core.CustomCommandProcessor.ProcessCmd(_socket, message, connection);
                        if (!(result is CommandNotFoundResult _))
                        {
                            _socket.SendMessage(_currentChannel, failedResult.Reason + " command line: " + output);
                        }
                    }
                }
            }
        }
    }
}
