using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
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
            public SQLiteConnection Connection;
            readonly HttpTools http = new HttpTools();
            public List<ParsedIRCMessage> Messages = new List<ParsedIRCMessage>();
            public bool PerformPpc = false;

            // Pass your service provider to the base command context.
            public CustomCommandContext(string target, ParsedIRCMessage message, Client client, SQLiteConnection connection, List<ParsedIRCMessage> msgs = null, bool perfppc = false, IServiceProvider provider = null) : base(provider)
            {
                Client = client;
                Connection = connection;
                Messages = msgs;
                Channel = target;
                Sender = message.Prefix.From;
                if (perfppc)
                {
                    var usr = new Core.User(Sender, connection, this);
                    PerformPpc = usr.RemItemFromInv("beer", 1).Result;
                }
            }

            private async Task SendMessageInternal(string channel, string message)
            {
                if (!message.Contains("\n"))
                {
                    if (message.Length > 250)
                    {
                        foreach (var slice in Core.MessageUtils.SplitMessage(message, 450))
                        {
                            await Client.SendAsync(new PrivMsgMessage(channel, slice));
                        }
                    }
                    else
                    {
                        await Client.SendAsync(new PrivMsgMessage(channel, message));
                    }
                }
                else
                {
                    int count = 0;
                    foreach (string outputstr in message.Split("\n"))
                    {
                        if (!string.IsNullOrWhiteSpace(outputstr))
                        {
                            await Client.SendAsync(new PrivMsgMessage(Channel, outputstr));
                            count++;
                        }
                        if (count > 4)
                        {
                            string link = await http.UploadToTrashbin(message, "addplain");
                            await Client.SendAsync(new PrivMsgMessage(Channel, Sender + ": Полный вывод здесь: " + link));
                            break;
                        }
                    }
                }
            }

            public async Task SendMessage(string channel, string message)
            {
                if (!PerformPpc)
                {
                    await SendMessageInternal(channel, message);
                }
                else
                {
                    var txt = await Core.Transalator.TranslatePpc(message);
                    await SendMessageInternal(channel, txt);
                }
            }

            public async void SendSadMessage(string channel, string message)
            {
                await SendMessage(channel, IrcColors.Gray + message);
            }

            public async void SendErrorMessage(string channel, string message)
            {
                await SendMessage(channel, IrcColors.Gray + message);
            }
        }
    }
}
