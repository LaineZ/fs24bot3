using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using fs24bot3.Core;
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
    [DataMember(Name = "name")] public string Name { get; set; }
    [DataMember(Name = "network")] public string Network { get; set; }
    [DataMember(Name = "channel")] public string Channel { get; set; }
    [DataMember(Name = "port")] public int Port { get; set; }
    [DataMember(Name = "nickserv_pass")] public string NickservPass { get; set; }
    [DataMember(Name = "server_pass")] public string ServerPassword { get; set; }

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
    private const string ConfigPath = "irc.toml";
    public string Name { get; private set; }

    public Bot BotContext { get; }
    public Dictionary<string, string> Fmt { get; }

    private Client BotClient { get; }
    private IrcConfiguration Config { get; }

    public Irc()
    {
        BotContext = new Bot(this);
        if (File.Exists(ConfigPath))
        {
            var loadedconfig = Toml.ToModel<IrcConfiguration>(File.ReadAllText(ConfigPath));
            Config = loadedconfig;
            Log.Information("Configuration loaded!");
        }
        else
        {
            Config = new IrcConfiguration();
            Log.Warning("IRC backend was unable to find configuration file, I will create it for you");
            File.WriteAllText(ConfigPath, Toml.FromModel(Config));
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
            var prefix = ConfigurationProvider.Config.Prefix;
            var permissions = msg.Sender.GetPermissions();
            BotContext.MessageTrigger(msg);

            if (permissions.ExecuteCommands)
            {
                await BotContext.ExecuteCommand(msg, prefix);
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
        var sb = new StringBuilder(message);
        foreach (var (tag, value) in Fmt)
        {
            sb.Replace($"[{tag}]", value);
        }

        List<string> msgLines = sb.ToString().Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        int count = 0;

        foreach (string outputstr in msgLines)
        {
            var currentSplit = MessageHelper.SplitByWords(outputstr);

            foreach (var split in currentSplit)
            {
                await BotClient.SendAsync(new PrivMsgMessage(channel, split));
                count += 1;

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


    /// <summary>
    /// Ensures authorization in IRC
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public async Task<bool> EnsureAuthorization(Core.User user)
    {
        var tcs = new TaskCompletionSource<bool>();
        ParsedIRCMessageHandler messageHandler = null;

        // ACC returns parsable information about a user's
        // login status. Note that on many networks, /whois
        // shows similar information faster and more reliably.
        // ACC also returns the unique entity ID of the given account.
        // The answer is in the form <nick> [-> account] ACC <digit> <EID>,
        // where <digit> is one of the following:
        //    0 - account or user does not exist
        //    1 - account exists but user is not logged in
        //    2 - user is not logged in but recognized (see ACCESS)
        //    3 - user is logged in
        // If the account is omitted the user's nick is used and
        // the " -> account" portion of the reply is omitted.
        // Account * means the account the user is logged in with.
        // example:
        // Totoro ACC 1 AAAAAXXX

        try
        {
            messageHandler = (client, message) =>
            {
                if (message.Prefix.From == "NickServ")
                {
                    var split = message.Trailing.Split(" ");

                    tcs.SetResult(split[2] == "3");

                    BotClient.IRCMessageParsed -= messageHandler;
                }
            };

            BotClient.IRCMessageParsed += messageHandler;

            await SendMessage("NickServ", $"ACC {user.Username}");
            return await tcs.Task;
        }
        catch (Exception e)
        {
            Log.Error("Unable to verify account status for {0}: {1}", user.Username, e);
            return await Task.FromResult(false);
        }
    }
}