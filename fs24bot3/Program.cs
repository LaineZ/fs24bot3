using IrcClientCore;
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

            Shop.Init();

            Log.Information("fs24_bot3 has started");

            IrcServer server = new IrcServer();

            Configuration.LoadConfiguration();

            connection = new SQLiteConnection("fsdb.sqlite");
            connection.CreateTable<Models.SQLUser.UserStats>();

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

            while (!_socket.ReadOrWriteFailed)
            {
                System.Threading.Thread.Sleep(1000);
                Shop.Update();
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

                if (connected && message.User != "*")
                {
                    var query = connection.Table<Models.SQLUser.UserStats>().Where(v => v.Nick.Equals(message.User));

                    if (query.Count() <= 0)
                    {
                        Log.Warning("User {0} not found in database", message.User);

                        var inv = new Models.ItemInventory.Inventory() { Items = new List<Models.ItemInventory.Item>() };

                        // just add random item to init invertory properly...
                        inv.Items.Add(new Models.ItemInventory.Item() { Name = Shop.getItem("money").Name, Count = 10});

                        var user = new Models.SQLUser.UserStats()
                        {
                            Nick = message.User,
                            Admin = 0,
                            AdminPassword = "changeme",
                            Level = 1,
                            Xp = 0,
                            Need = 300,
                            JsonInv = JsonConvert.SerializeObject(inv).ToString()
                        };

                        connection.Insert(user);

                    }
                    else
                    {
                        UserOperations usr = new UserOperations(message.User, connection);
                        usr.IncreaseXp(message.Text.Length + 1);
                    }

                    if (!CommandUtilities.HasPrefix(message.Text.TrimEnd(), '@', out string output))
                        return;

                    IResult result = await _service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(message, _socket, connection));
                    if (result is FailedResult failedResult && !(result is CommandNotFoundResult _))
                        _socket.SendMessage(_currentChannel, failedResult.Reason + " command: " + output);
                }
            }
        }
    }
}
