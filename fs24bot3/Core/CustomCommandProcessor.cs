﻿using fs24bot3.Models;
using NetIRC;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public class CustomCommandProcessor
    {
        private Client Client { get; }
        private SQLite.SQLiteConnection Connect { get; }
        private List<ParsedIRCMessage> MessageBus { get; }

        private CustomExecutor CustomExecutor { get; }

        public CustomCommandProcessor(Client client, SQLite.SQLiteConnection connect, List<ParsedIRCMessage> messageBus)
        {
            Client = client;
            Connect = connect;
            MessageBus = messageBus;
            CustomExecutor = new CustomExecutor(Client, Connect);
            Log.Information("Custom command processor enabled!");
        }

        public bool ProcessCmd(string senderNick, string channel, string message)
        {
            if (message.StartsWith("@"))
            {
                var argsArray = message.Split(" ").ToList();
                string cmdname = argsArray[0];
                //Log.Verbose("Issused command: {0}", cmdname);
                var cmd = Connect.Table<SQL.CustomUserCommands>().SingleOrDefault(x => x.Command == cmdname);

                if (cmd != null)
                {
                    argsArray.RemoveAt(0); // removing command name

                    if (cmd.IsLua == 0)
                    {
                        CustomExecutor.Execute(cmd, senderNick, channel, string.Join(" ", argsArray));
                        return true;
                    }
                    else
                    {
                        new LuaExecutor(Client, Connect, MessageBus, cmd).Execute(senderNick, channel, message, string.Join(" ", argsArray));
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
