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
        public Random Random { get; }
        public Bot BotCtx { get; }
        public bool PerformPpc { get; }
        public bool FromBridge { get; }
        public Core.User User { get; }

        // Pass your service provider to the base command context.
        public CustomCommandContext(Bot bot, in MessageGeneric message, bool perfppc = false, IServiceProvider provider = null) : base(provider)
        {
            BotCtx = bot;
            Channel = message.Target;
            Random = new Random();
            FromBridge = message.Kind == MessageKind.MessageFromBridge;
            User = message.Sender;
            if (perfppc)
            {
                PerformPpc = User.RemItemFromInv(BotCtx.Shop, "beer", 1).Result;
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            if (!PerformPpc)
            {
                await BotCtx.Client.SendMessage(channel, message);
            }
            else
            {
                var txt = await Core.Transalator.TranslatePpc(MessageHelper.StripIRC(message));
                await BotCtx.Client.SendMessage(channel, txt);
            }
        }

        public async Task SendMessage(string message)
        {
            if (!PerformPpc)
            {
                await BotCtx.Client.SendMessage(Channel, message);
            }
            else
            {
                var txt = await Core.Transalator.TranslatePpc(MessageHelper.StripIRC(message));
                await BotCtx.Client.SendMessage(Channel, txt);
            }
        }

        public async void SendSadMessage(string channel, string message = "")
        {
            if (!message.Any())
            {
                await BotCtx.Client.SendMessage(channel,"[gray]" + RandomMsgs.NotFoundMessages.Random());
            }
            else
            {
                await BotCtx.Client.SendMessage(channel, "[gray]" + message);
            }
        }

        public async void SendErrorMessage(string channel, string message)
        {
            await BotCtx.Client.SendMessage(channel, "[red]" + message);
        }
    }
}
