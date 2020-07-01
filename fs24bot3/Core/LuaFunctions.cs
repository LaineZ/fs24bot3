using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fs24bot3.Models;
using Serilog;
using SQLite;

namespace fs24bot3.Core
{
    public class LuaFunctions
    {
        private SQLiteConnection Connection;
        private string Caller;

        public LuaFunctions(SQLiteConnection connection, string caller)
        {
           Connection = connection;
           Caller = caller;
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
    }
}
