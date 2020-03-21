using IrcClientCore;
using IrcClientCore.Commands;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace fs24bot3
{
    class Program
    {

        public static ObservableCollection<Message> _channelBuffers = null;
        public static Irc _socket = null;
        public static CommandManager handler = null;
        private static string _currentChannel;
        private static bool connected = false;

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
            IrcServer server = new IrcServer();

            Configuration.LoadConfiguration();

            server.Hostname = Configuration.network;
            server.Name = "irc network";
            server.Username = Configuration.name;
            server.Channels = Configuration.channel;
            server.ShouldReconnect = Configuration.reconnect;
            server.Ssl = Configuration.ssl;
            server.Port = Convert.ToInt32(Configuration.port);

            SqlTools sql = new SqlTools("fsdb.sqlite");

            sql.init();

            _socket = new IrcSocket(server);
            _socket.Connect();

            handler = _socket.CommandManager;

            SwitchChannel("");
            _service.AddModule<CommandModule>();

            while (!_socket.ReadOrWriteFailed)
            {

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

                if (connected)
                {
                    if (!CommandUtilities.HasPrefix(message.Text.TrimEnd(), '@', out string output))
                        return;

                    IResult result = await _service.ExecuteAsync(output, new CustomCommandContext(message, _socket));
                    if (result is FailedResult failedResult)
                        _socket.SendMessage(_currentChannel, failedResult.Reason + ": " + output);
                }
            }
        }
    }
}
