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

            public string Channel => Message.Channel;
            public SQLiteConnection Connection;

            // Pass your service provider to the base command context.
            public CustomCommandContext(Message message, Irc socket, SQLiteConnection connection, IServiceProvider provider = null) : base(provider)
            {
                Message = message;
                Socket = socket;
                Connection = connection;
            }
        }
    }
}
