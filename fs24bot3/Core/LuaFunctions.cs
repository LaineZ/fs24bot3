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
        private readonly SQLiteConnection Connection;
        private string Caller { get; set; }
        private List<PrivMsgMessage> MessageBus { get; set; }
        private string Command { get; set; }

        public LuaFunctions(SQLiteConnection connection, string caller, string commandname, List<PrivMsgMessage> messageBus)
        {
            Connection = connection;
            Caller = caller;
            MessageBus = messageBus;
            Command = commandname;
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

        public string GetLocalStorage()
        {
            var query = Connection.Table<SQL.ScriptStorage>().Where(v => v.Nick.Equals(Caller) && v.Command == Command);

            if (query.Any())
            {
                return query.First().Data;
            }
            
            return null;
        }

        public bool SetLocalStorage(string data)
        {
            if (Encoding.Unicode.GetByteCount(data) < 1024)
            {
                Connection.Insert(new SQL.ScriptStorage() { Command = Command, Nick = Caller, Data = data });
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AppendLocalStorage(string data)
        {
            string totalData = GetLocalStorage() + data;
            if (Encoding.Unicode.GetByteCount(totalData) < 1024)
            {
                Connection.Insert(new SQL.ScriptStorage() { Command = Command, Nick = Caller, Data = totalData });
                return true;
            }
            else
            {
                return false;
            }
        }

        public PrivMsgMessage[] GetMessageBus()
        {
            return MessageBus.ToArray();
        }
    }
}
