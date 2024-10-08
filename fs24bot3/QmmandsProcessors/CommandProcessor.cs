﻿using fs24bot3.Models;
using NetIRC;
using Qmmands;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using fs24bot3.Helpers;
using System.Collections.Generic;
using System.Threading;
using fs24bot3.Core;

namespace fs24bot3.QmmandsProcessors;

public class CommandProcessor
{
    public sealed class CustomCommandContext : CommandContext
    {
        public string Channel { get; }
        public Random Random { get; }
        public Bot BotCtx { get; }
        public bool FromBridge { get; }
        public Core.User User { get; }
        
        public HttpTools HttpTools { get; }
        public InternetServicesHelper ServicesHelper { get; }
        public bool IsAuthorizedAction { get;  }


        // Pass your service provider to the base command context.
        public CustomCommandContext(Bot bot, in MessageGeneric message, bool authorized, IServiceProvider provider = null) : base(provider)
        {
            BotCtx = bot;
            Channel = message.Target;
            Random = new Random();
            FromBridge = message.Kind == MessageKind.MessageFromBridge;
            HttpTools = new HttpTools();
            ServicesHelper = new InternetServicesHelper(HttpTools);
            User = message.Sender;
            IsAuthorizedAction = authorized;
        }

        public async Task SendMessage(string channel, string message)
        {
            await BotCtx.Client.SendMessage(channel, message);
        }

        public async Task SendMessage(string message)
        {
            await SendMessage(Channel, message);
        }

        public async Task SendSadMessage(string channel, string message = "")
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

        public async Task SendSadMessage()
        {
            await SendSadMessage(Channel);
        }

        public async Task SendErrorMessage(string message)
        {
            await SendErrorMessage(Channel, "[red]" + message);
        }

        public async Task SendErrorMessage(string channel, string message)
        {
            await BotCtx.Client.SendMessage(channel, "[red]" + message);
        }
    }
}
