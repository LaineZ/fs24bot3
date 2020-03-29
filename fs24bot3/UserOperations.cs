using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System.Linq;
using SQLiteNetExtensions.Extensions;

namespace fs24bot3
{
    class UserOperations
    {
        public string Username;
        public SQLiteConnection Connect;
        IrcClientCore.Irc Socket;
        IrcClientCore.Message Message;


        public UserOperations(string username, SQLiteConnection connection, IrcClientCore.Irc socket = null, IrcClientCore.Message message = null)
        {
            Username = username;
            Connect = connection;
            Socket = socket;
            Message = message;
        }


        public bool IncreaseXp(int count)
        {
            var query = Connect.Table<Models.SQL.UserStats>().Where(v => v.Nick.Equals(Username));

            foreach (var nick in query)
            {
                Connect.Execute("UPDATE UserStats SET Xp = Xp + ? WHERE Nick = ?", count, nick.Nick);

                Log.Verbose("{4}: Setting level: {0} {1} Current data: {2}/{3}", count, nick.Level, nick.Xp, nick.Need, nick.Nick);

                if (nick.Xp >= nick.Need)
                {
                    Connect.Execute("UPDATE UserStats SET Level = Level + 1 WHERE Nick = ?", nick.Nick);
                    Connect.Execute("UPDATE UserStats SET Xp = 0 WHERE Nick = ?", nick.Nick);
                    Connect.Execute("UPDATE UserStats SET Need = Level * 120 WHERE Nick = ?", nick.Nick);
                    return true;
                }
            }
            return false;
        }

        public void SetLevel(int level)
        {
            Connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, Username);
            Connect.Execute("UPDATE UserStats SET Need = Level * 120 WHERE Nick = ?", Username);
        }

        public bool AddItemToInv(string name, int count)
        {
            throw new NotImplementedException();
        }

        public bool RemItemFromInv(string name, int count)
        {
            throw new NotImplementedException();
        }

        public Models.SQL.UserStats GetUserInfo()
        {
            throw new NotImplementedException();
        }

        internal bool AddTag(string tagname, int v)
        {
            throw new NotImplementedException();
        }
    }
}
