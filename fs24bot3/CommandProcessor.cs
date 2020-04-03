using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using NetIRC.Messages;
using NetIRC.Connection;

namespace fs24bot3
{
    public class CommandProcessor
    {
        public sealed class CustomCommandContext : CommandContext
        {
            public PrivMsgMessage Message { get; }
            public NetIRC.Client Client { get; }
            readonly HttpTools http = new HttpTools();

            public string Channel => Message.To;
            public SQLiteConnection Connection;

            // Pass your service provider to the base command context.
            public CustomCommandContext(PrivMsgMessage message, NetIRC.Client client, SQLiteConnection connection, IServiceProvider provider = null) : base(provider)
            {
                Message = message;
                Client = client;
                Connection = connection;
            }
            // created just for compatibilty
            public async void SendMessage(string channel, string message)
            {
                await Client.SendAsync(new PrivMsgMessage(channel, message));
            }

            public async void SendMultiLineMessage(string content)
            {
                int count = 0;
                foreach (string outputstr in content.Split("\n"))
                {
                    await Client.SendAsync(new PrivMsgMessage(this.Channel, this.Message.From + ": " + outputstr));
                    count++;
                    if (count > 4)
                    {
                        string link = await http.UploadToPastebin(content);
                        await Client.SendAsync(new PrivMsgMessage(this.Channel, this.Message.From + ": Полный вывод здесь: " + link));
                        break;
                    }
                }
            }
        }
    }
}
