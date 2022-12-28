using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using fs24bot3.Models;
using Serilog;
using Tomlyn;

namespace fs24bot3.Backend;

public class DiscordConfiguration
{
    [DataMember(Name = "token")]
    public string Token { get; set; }

    public DiscordConfiguration()
    {
        Token = "";
    }
}


public class Discord : IMessagingClient
{
    private const string ConfigPath = "discord.toml";
    public string Name { get; private set; }
    public Bot BotContext { get; }

    public Dictionary<string, string> Fmt { get; }

    private DiscordConfiguration Config { get; }

    private DiscordClient BotClient { get; }

    public Discord()
    {
        if (File.Exists(ConfigPath))
        {
            var loadedconfig = Toml.ToModel<DiscordConfiguration>(File.ReadAllText(ConfigPath));
            Config = loadedconfig;
            Log.Information("Configuration loaded!");
        }
        else
        {
            Config = new DiscordConfiguration();
            Log.Warning("Discord backend was unable to find configuration file, I will create it for you");
            File.WriteAllText(ConfigPath, Toml.FromModel(Config));
        }

        var config = new DSharpPlus.DiscordConfiguration()
        {
            Token = Config.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.All
        };

        BotClient = new DiscordClient(config);
        BotContext = new Bot(this);

        BotClient.Ready += Ready;
        BotClient.MessageCreated += MessageCreated;
    }

    private async Task Ready(DiscordClient client, EventArgs args)
    {
        Name = client.CurrentUser.Username;
        Log.Information("Connected!");
    }


    private async Task MessageCreated(DiscordClient client, MessageCreateEventArgs args)
    {
        var user = new Core.User(args.Author.Mention, in BotContext.Connection);
        var prefix = user.GetUserPrefix();
        var messageKind = MessageKind.Message;

        if (args.Message.Channel.IsPrivate)
        {
            messageKind = MessageKind.MessagePersonal; 
        }
        
        var msg = new MessageGeneric(args.Message.Content, args.Channel.Id.ToString(), user, messageKind);

        if (!msg.Sender.UserIsIgnored())
        {
            if (msg.Kind == MessageKind.Message) { BotContext.MessageTrigger(msg); }

            bool ppc = msg.Body.StartsWith("p");
            await BotContext.ExecuteCommand(msg, prefix, ppc);
        }

    }

    public async Task SendMessage(string channel, string message)
    {
        var res = ulong.TryParse(channel, out var id);
        if (!res)
        {
            Log.Warning("Unable to parse channel id: {0}", channel);
            return;
        }
        var ch = await BotClient.GetChannelAsync(id);
        await BotClient.SendMessageAsync(ch, message);
    }

    public void Process()
    {
        Log.Information("Connecting to Discord...");
        Task.Run(() => BotClient.ConnectAsync());
        BotContext.ProccessInfinite();
    }
}