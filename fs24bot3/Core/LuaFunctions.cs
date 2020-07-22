using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fs24bot3.Models;
using NetIRC.Messages;
using Serilog;
using SQLite;

namespace fs24bot3.Core
{
    public class LuaFunctions
    {
        private SQLiteConnection Connection;
        private string Caller;
        private List<PrivMsgMessage> MessageBus;

        public LuaFunctions(SQLiteConnection connection, string caller, List<PrivMsgMessage> messageBus)
        {
           Connection = connection;
           Caller = caller;
           MessageBus = messageBus;
        }

        public string[] GetCommandOutput(string input, string command)
        {
            StringBuilder argsFinal = new StringBuilder(Connection.Table<SQL.CustomUserCommands>().SingleOrDefault(x => x.Command == command && x.IsLua == 0).Output);

            Random random = new Random();
            var arr = Connection.Table<SQL.UserStats>().ToList();
            var nick = MessageUtils.AntiHightlight(arr[random.Next(0, arr.Count - 1)].Nick);

            argsFinal.Replace("#USERINPUT", input);
            argsFinal.Replace("#USERNAME", Caller);
            argsFinal.Replace("#RNDNICK", nick);
            argsFinal.Replace("#RNG", random.Next(int.MinValue, int.MaxValue).ToString());


            Log.Verbose("OUTPUT: {0}", argsFinal.ToString());

            return argsFinal.ToString().Split("||");
        }

        public PrivMsgMessage[] GetMessageBus()
        {
            return MessageBus.ToArray();
        }
    }
}
