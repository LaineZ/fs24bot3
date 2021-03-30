using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;

namespace fs24bot3
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

            // Pass your service provider to the base command context.
            public CustomCommandContext(ParsedIRCMessage message, Client client, SQLiteConnection connection, List<ParsedIRCMessage> msgs = null, IServiceProvider provider = null) : base(provider)
            {
                Client = client;
                Connection = connection;
                Messages = msgs;
                Channel = message.Parameters[0];
                Sender = message.Prefix.From;
            }

            public async void SendMessage(string channel, string message)
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
                    SendMultiLineMessage(message);
                }
            }

            public void SendMessage(string message)
            {
                SendMessage(Channel, message);
            }


            public void SendSadMessage(string channel, string message)
            {
                SendMessage(channel, IrcColors.Gray + message);
            }

            public void SendErrorMessage(string channel, string message)
            {
                SendMessage(channel, IrcColors.Gray + message);
            }

            public async void SendMultiLineMessage(string content)
            {
                int count = 0;
                foreach (string outputstr in content.Split("\n"))
                {
                    if (!string.IsNullOrWhiteSpace(outputstr))
                    {
                        await Client.SendAsync(new PrivMsgMessage(Channel, Sender + ": " + outputstr));
                    }
                    count++;
                    if (count > 4)
                    {
                        string link = await http.UploadToTrashbin(content, "addplain");
                        await Client.SendAsync(new PrivMsgMessage(Channel, Sender + ": Полный вывод здесь: " + link));
                        break;
                    }
                }
            }
        }
    }
}
