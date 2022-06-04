using fs24bot3.Core;
using NetIRC;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using fs24bot3.Helpers;

namespace fs24bot3
{
    class Program
    {
        public static Bot Botara;

        static void Main()
        {
#if DEBUG
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Verbose()
            .CreateLogger();
#else
             Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Information()
            .CreateLogger();
#endif

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.OutputEncoding = Encoding.Unicode;
            }

            Log.Information("fs24_bot 3 by 140bpmdubstep");
            ConfigurationProvider.LoadConfiguration();

            Botara = new Bot();

            Botara.BotClient.RawDataReceived += Client_OnRawDataReceived;
            Botara.BotClient.IRCMessageParsed += Client_OnIRCMessageParsed;
            Botara.BotClient.RegistrationCompleted += Client_OnRegister;

            Log.Information("Connecting to: {0}:{1}", ConfigurationProvider.Config.Network, ConfigurationProvider.Config.Port);
            Task.Run(() => Botara.BotClient.ConnectAsync());
            Log.Information("First initialization is okay!");

            Botara.ProccessInfinite();
        }

        private async static void Client_OnIRCMessageParsed(Client client, ParsedIRCMessage message)
        {
            if (message.IRCCommand == IRCCommand.PRIVMSG)
            {
                string nick = message.Prefix.From;
                string target = message.Parameters[0];

                var user = new Core.User(nick, Botara.Connection, null);
                var prefix = user.GetUserPrefix();
                var messageString = message.Trailing.TrimEnd();

                if (nick == ConfigurationProvider.Config.BridgeNickname)
                {
                    // trim bridged user nickname like
                    // <cheburator> //bpm140//: @ms привет
                    var msg = messageString.Split(":").ToList();
                    messageString = msg.Last().TrimStart(' ');
                    nick = "@[" + MessageHelper.StripIRC(msg.First()) + "]";
                    Log.Verbose("Message from the bridge: {0} from {1}", messageString, nick);
                }
                else
                {
                    if (!user.UserIsIgnored()) { Botara.MessageTrigger(nick, target, message); }
                }

                if (message.Parameters[0] == client.User.Nick)
                {
                    target = message.Prefix.From;
                }

                if (!user.UserIsIgnored())
                {
                    bool ppc = messageString.StartsWith("p") && Transalator.AlloPpc;
                    await Botara.ExecuteCommand(nick, target, messageString, message, prefix, ppc);
                }
            }

            if (message.IRCCommand == IRCCommand.ERROR)
            {
                Log.Error("Connection closed due to error...");
                Environment.Exit(1);
            }

            if (message.NumericReply == IRCNumericReply.ERR_NICKNAMEINUSE)
            {
                Botara.SetupNick(Botara.Name + new Random().Next(int.MinValue, int.MaxValue));
            }

            if (message.NumericReply == IRCNumericReply.ERR_PASSWDMISMATCH)
            {
                await client.SendRaw("PASS " + ConfigurationProvider.Config.ServerPassword);
            }

            if (message.IRCCommand == IRCCommand.KICK)
            {
                if (message.Parameters[1] == Botara.Name)
                {
                    Log.Warning("I've got kick from {0} rejoining...", message.Prefix);
                    await client.SendRaw("JOIN " + message.Parameters[0]);
                    await Botara.SendMessage(ConfigurationProvider.Config.Channel, "За что?");
                }
            }
        }

        private async static void Client_OnRegister(object sender, EventArgs _)
        {
            await Botara.BotClient.SendRaw("JOIN " + ConfigurationProvider.Config.Channel);
            await Botara.SendMessage("Nickserv", "IDENTIFY " + ConfigurationProvider.Config.NickservPass);

            //var res = Helpers.InternetServicesHelper.InPearls("алкоголь").Result.Random();
            //await Botara.SendMessage(ConfigurationProvider.Config.Channel, res);
        }

        private static void Client_OnRawDataReceived(Client client, string rawData)
        {
            Log.Information(rawData);
        }
    }
}
