using fs24bot3.Models;
using NetIRC;
using Qmmands;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using fs24bot3.Helpers;
using System.Collections.Generic;

namespace fs24bot3.QmmandsProcessors;

public class CommandProcessor
{
    public sealed class CustomCommandContext : CommandContext
    {
        public string Channel { get; }
        public string Sender { get; }
        public Random Random { get; }
        public Bot BotCtx { get; }
        public bool PerformPpc { get; }
        public bool FromBridge { get; }
        public Core.User User { get; set; }

        // Pass your service provider to the base command context.
        public CustomCommandContext(string target, string sender, Bot bot, bool perfppc = false, bool fromBridge = false, IServiceProvider provider = null) : base(provider)
        {
            BotCtx = bot;
            Channel = target;
            Sender = sender;
            Random = new Random();
            FromBridge = fromBridge;
            User = new Core.User(Sender, bot.Connection, this);
            if (perfppc)
            {
                PerformPpc = User.RemItemFromInv(BotCtx.Shop, "beer", 1).Result;
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
                var txt = await Core.Transalator.TranslatePpc(MessageHelper.StripIRC(message));
                await BotCtx.SendMessage(channel, txt);
            }
        }

        public async Task SendMessage(string message)
        {
            if (!PerformPpc)
            {
                await BotCtx.SendMessage(Channel, message);
            }
            else
            {
                var txt = await Core.Transalator.TranslatePpc(MessageHelper.StripIRC(message));
                await BotCtx.SendMessage(Channel, txt);
            }
        }

        public async void SendSadMessage(string channel, string message = "")
        {
            if (!message.Any())
            {
                await BotCtx.SendMessage(channel, IrcClrs.Gray + RandomMsgs.NotFoundMessages.Random());
            }
            else
            {
                await BotCtx.SendMessage(channel, IrcClrs.Gray + message);
            }
        }

        public async void SendErrorMessage(string channel, string message)
        {
            await BotCtx.SendMessage(channel, IrcClrs.Red + message);
        }
    }
}
