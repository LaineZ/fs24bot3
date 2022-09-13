using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using NetIRC;
using NetIRC.Connection;
using NetIRC.Messages;
using Serilog;

namespace fs24bot3.Backend;
/// <summary>
/// IRC backend
/// </summary>
public class Irc : IMessagingClient
{
    public string Name { get; private set; }

    public Bot BotContext { get; }
    public Dictionary<string, string> Fmt { get; }

    private Client BotClient;

    public Irc()
    {
        BotContext = new Bot(this);
        Name = ConfigurationProvider.Config.Name;
        Fmt = new Dictionary<string, string>();
        // add irc colors
        
        Fmt.Add("b","");
        Fmt.Add("r","");
        Fmt.Add("white","00");
        Fmt.Add("black","01");
        Fmt.Add("blue","02");
        Fmt.Add("green","03");
        Fmt.Add("red", "04");
        Fmt.Add("brown","05");
        Fmt.Add("purple","06");
        Fmt.Add("orange","07");
        Fmt.Add("yellow","08");
        Fmt.Add("lime","09");
        Fmt.Add("teal","10");
        Fmt.Add("cyan","11");
        Fmt.Add("royal","12");
        Fmt.Add("pink","13");
        Fmt.Add("gray","14");
        Fmt.Add("silver","15");


        BotClient = new Client(new NetIRC.User(ConfigurationProvider.Config.Name, "Sopli IRC 3.0"), new TcpClientConnection(ConfigurationProvider.Config.Network, ConfigurationProvider.Config.Port));
        
        BotClient.RawDataReceived += Client_OnRawDataReceived;
        BotClient.IRCMessageParsed += Client_OnIRCMessageParsed;
        BotClient.RegistrationCompleted += Client_OnRegister;
    }
    
    
    private async void Client_OnIRCMessageParsed(Client client, ParsedIRCMessage message)
    {
        if (message.IRCCommand == IRCCommand.PRIVMSG)
        {
            var msg = new MessageGeneric(in message, in BotContext.Connection, Name);
            var prefix = msg.Sender.GetUserPrefix();

            if (!msg.Sender.UserIsIgnored())
            {
                if (msg.Kind == MessageKind.Message) { BotContext.MessageTrigger(msg); }

                bool ppc = msg.Body.StartsWith("p") && Transalator.AlloPpc;
                await BotContext.ExecuteCommand(msg, prefix, ppc);
            }
        }

        if (message.IRCCommand == IRCCommand.ERROR)
        {
            Log.Error("Connection closed due to error...");
            Environment.Exit(1);
        }

        if (message.NumericReply == IRCNumericReply.ERR_NICKNAMEINUSE)
        {
           SetupNick(Name + new Random().Next(int.MinValue, int.MaxValue));
        }

        if (message.NumericReply == IRCNumericReply.ERR_PASSWDMISMATCH)
        {
            await client.SendRaw("PASS " + ConfigurationProvider.Config.ServerPassword);
        }

        if (message.IRCCommand == IRCCommand.KICK)
        {
            if (message.Parameters[1] == Name)
            {
                Log.Warning("I've got kick from {0} rejoining...", message.Prefix);
                await client.SendRaw("JOIN " + message.Parameters[0]);
                await SendMessage(ConfigurationProvider.Config.Channel, "За что?");
            }
        }
    }

    private async void Client_OnRegister(object sender, EventArgs _)
    {
        JoinChannel(ConfigurationProvider.Config.Channel);
        await SendMessage("Nickserv", "IDENTIFY " + ConfigurationProvider.Config.NickservPass);

        //var res = Helpers.InternetServicesHelper.InPearls("алкоголь").Result.Random();
        //await Botara.SendMessage(ConfigurationProvider.Config.Channel, res);
    }

    private static void Client_OnRawDataReceived(Client client, string rawData)
    {
        Log.Information(rawData);
    }

    public async void SetupNick(string nickname)
    {
        await BotClient.SendRaw("NICK " + nickname);
        Name = nickname;
    }
    public async void JoinChannel(string name)
    {
        await BotClient.SendRaw("JOIN " + name);
    }
    public async void PartChannel(string name)
    {
        await BotClient.SendRaw("PART " + name);
    }

    public void Process()
    {   
        Log.Information("Connecting to: {0}:{1}", ConfigurationProvider.Config.Network, ConfigurationProvider.Config.Port);
        Task.Run(() => BotClient.ConnectAsync());
        BotContext.ProccessInfinite();
    }

    public async Task SendMessage(string channel, string message)
    {
        List<string> msgLines = message.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        int count = 0;

        foreach (string outputstr in msgLines)
        {
            var sb = new StringBuilder(outputstr);
            foreach (var (tag, value) in Fmt)
            {
                sb.Replace($"[{tag}]", value);
            }

            if (sb.Length < 1000)
            {
                await BotClient.SendAsync(new PrivMsgMessage(channel, sb.ToString()));
                count++;
            }
            else
            {
                string link = await InternetServicesHelper.UploadToTrashbin(
                    MessageHelper.StripIRC(message), "addplain");
                await BotClient.SendAsync(new PrivMsgMessage(channel, $"Слишком жесткое сообщение с длинной " +
                                                                      $"{sb.Length} символов! Психанул?!?!?!"));
                await BotClient.SendAsync(new PrivMsgMessage(channel, "Полный вывод: " + link));
                return;
            }

            if (count > 4)
            {
                string link = await InternetServicesHelper.UploadToTrashbin(MessageHelper.StripIRC(message), 
                    "addplain");
                await BotClient.SendAsync(new PrivMsgMessage(channel, "Полный вывод: " + link));
                return;
            }
        }
    }
}