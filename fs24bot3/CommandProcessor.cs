using IrcClientCore;
using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3
{
    public class CommandProcessor
    {
        public sealed class CustomCommandContext : CommandContext
        {
            public Message Message { get; }
            public Irc Socket { get; }
            readonly HttpTools http = new HttpTools();

            public string Channel => Message.Channel;
            public SQLiteConnection Connection;

            // Pass your service provider to the base command context.
            public CustomCommandContext(Message message, Irc socket, SQLiteConnection connection, IServiceProvider provider = null) : base(provider)
            {
                Message = message;
                Socket = socket;
                Connection = connection;
            }


            public async void SendMultiLineMessage(string content)
            {
                int count = 0;
                foreach (string outputstr in content.Split("\n"))
                {
                    this.Socket.SendMessage(this.Message.User + ": " + this.Channel, outputstr);
                    count++;
                    if (count > 4)
                    {
                        string link = await http.UploadToPastebin(content);
                        this.Socket.SendMessage(this.Channel, this.Message.User + ": Полный вывод здесь: " + link);
                        break;
                    }
                }
            }
        }
    }
}
