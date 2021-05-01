using fs24bot3.Models;
using NetIRC;
using Qmmands;
using System;
using System.Threading.Tasks;

namespace fs24bot3.QmmandsProcessors
{
    public class CommandProcessor
    {
        public sealed class CustomCommandContext : CommandContext
        {
            public Client Client { get; }

            public string Channel;
            public string Sender;
            public Bot BotCtx;
            public bool PerformPpc = false;

            // Pass your service provider to the base command context.
            public CustomCommandContext(string target, ParsedIRCMessage message, Bot bot, bool perfppc = false, IServiceProvider provider = null) : base(provider)
            {
                BotCtx = bot;
                Channel = target;
                Sender = message.Prefix.From;
                if (perfppc)
                {
                    var usr = new Core.User(Sender, bot.Connection, this);
                    PerformPpc = usr.RemItemFromInv("beer", 1).Result;
                }
            }

            public async Task SendMessage(string channel, string message)
            {
                if (!PerformPpc)
                {
                    await BotCtx.SendMessage(channel, message);
                }
                else
                {
                    var txt = await Core.Transalator.TranslatePpc(message);
                    await BotCtx.SendMessage(channel, txt);
                }
            }

            public async void SendSadMessage(string channel, string message)
            {
                await BotCtx.SendMessage(channel, IrcColors.Gray + message);
            }

            public async void SendErrorMessage(string channel, string message)
            {
                await BotCtx.SendMessage(channel, IrcColors.Gray + message);
            }
        }
    }
}
