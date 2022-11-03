using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using fs24bot3.Helpers;
using fs24bot3.Models;
using NetIRC;
using NetIRC.Connection;
using NetIRC.Messages;
using Serilog;
using Tomlyn;

namespace fs24bot3.Backend;


public class IrcConfiguration
{
    [DataMember(Name = "name")]
    public string Name { get; set; }
    [DataMember(Name = "network")]
    public string Network { get; set; }
    [DataMember(Name = "channel")]
    public string Channel { get; set; }
    [DataMember(Name = "port")]
    public int Port { get; set; }
    [DataMember(Name = "nickserv_pass")]
    public string NickservPass { get; set; }
    [DataMember(Name = "server_pass")]
    public string ServerPassword { get; set; }

    public IrcConfiguration()
    {
        Name = "fs24bot";
        Network = "irc.esper.net";
        Channel = "#fl-studio";
        Port = 6667;
        NickservPass = "zxcvbm1";
        ServerPassword = "zxcvbm1";
    }
}

/// <summary>
/// IRC backend
/// </summary>
public class Irc : IMessagingClient
{
    private const string CONFIG_PATH = "irc.toml";
    public string Name { get; private set; }

    public Bot BotContext { get; }
    public Dictionary<string, string> Fmt { get; }

    private Client BotClient { get; }
    private IrcConfiguration Config { get; }

    public Irc()
    {
        BotContext = new Bot(this);
        if (File.Exists(CONFIG_PATH))
        {
            var loadedconfig = Toml.ToModel<IrcConfiguration>(File.ReadAllText(CONFIG_PATH));
            Config = loadedconfig;
            Log.Information("Configuration loaded!");
        }
        else
        {
            Config = new IrcConfiguration();
            Log.Warning("IRC backend was unable to find configuration file, I will create it for you");
            File.WriteAllText(CONFIG_PATH, Toml.FromModel(Config));
        }
        
        Fmt = new Dictionary<string, string>
        {
            // add irc colors
            { "b", "" },
            { "r", "" },
            { "white", "00" },
            { "black", "01" },
            { "blue", "02" },
            { "green", "03" },
            { "red", "04" },
            { "brown", "05" },
            { "purple", "06" },
            { "orange", "07" },
            { "yellow", "08" },
            { "lime", "09" },
            { "teal", "10" },
            { "cyan", "11" },
            { "royal", "12" },
            { "pink", "13" },
            { "gray", "14" },
            { "silver", "15" }
        };

        SetupNick(Config.Name);

        BotClient = new Client(new NetIRC.User(Name, "Sopli IRC 3.0"), 
            new TcpClientConnection(Config.Network, Config.Port));
        
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

                bool ppc = msg.Body.StartsWith("p");
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
            await client.SendRaw("PASS " + Config.ServerPassword);
        }

        if (message.IRCCommand == IRCCommand.KICK && message.Parameters[1] == Name)
        {
            Log.Warning("I've got kick from {0} rejoining...", message.Prefix);
                await client.SendRaw("JOIN " + message.Parameters[0]);
                await SendMessage(message.Parameters[0], "За что?");
        }
    }

    private async void Client_OnRegister(object sender, EventArgs _)
    {
        JoinChannel(Config.Channel);
        await SendMessage("Nickserv", "IDENTIFY " + Config.NickservPass);
        //var res = Helpers.InternetServicesHelper.InPearls("алкоголь").Result.Random();
        //await Botara.SendMessage(ConfigurationProvider.Config.Channel, res);
    }

    private static void Client_OnRawDataReceived(Client client, string rawData)
    {
        Log.Information(rawData);
    }

    public async void SetupNick(string nickname)
    {
        Name = nickname;
        if (BotClient != null)
        {
            await BotClient.SendRaw("NICK " + nickname);
        }
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
        Log.Information("Connecting to: {0}:{1}", Config.Network, Config.Port);
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