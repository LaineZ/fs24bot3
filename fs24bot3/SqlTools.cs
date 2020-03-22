using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System.Linq;

namespace fs24bot3
{
    class SQLTools
    {
        public bool increaseXp(SQLiteConnection connect, string username, int count)
        {
            var query = connect.Table<Models.SQLUser.UserStats>().Where(v => v.Nick.Equals(username));

            foreach (var nick in query)
            {
                connect.Execute("UPDATE UserStats SET Xp = Xp + ? WHERE Nick = ?", count, nick.Nick);

                Log.Verbose("{4}: Setting level: {0} {1} Current data: {2}/{3}", count, nick.Level, nick.Xp, nick.Need, nick.Nick);

                if (nick.Xp >= nick.Need)
                {
                    connect.Execute("UPDATE UserStats SET Level = Level + 1 WHERE Nick = ?", nick.Nick);
                    connect.Execute("UPDATE UserStats SET Xp = 0 WHERE Nick = ?", nick.Nick);
                    connect.Execute("UPDATE UserStats SET Need = Level * 120 WHERE Nick = ?", nick.Nick);
                    return true;
                }
            }
            return false;
        }

        public void setLevel(SQLiteConnection connect, string username, int level)
        {
            connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, username);
            connect.Execute("UPDATE UserStats SET Need = Level * 120 WHERE Nick = ?", username);
        }

        public Models.SQLUser.UserStats getUserInfo(SQLiteConnection connect, string username)
        {
            var query = connect.Table<Models.SQLUser.UserStats>().Where(v => v.Nick.Equals(username));
            foreach (var nick in query)
            {
                return nick;
            }
            return null;
        }

        public bool RemItemFromInv(string name, string username, int count, SQLite.SQLiteConnection connect)
        {
            SQLTools sql = new SQLTools();

            var userinfo = sql.getUserInfo(connect, username);

            if (userinfo == null)
            {
                Log.Error("User " + username + " not exsist!");
                return false;
            }

            var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);

            int itemToRemove = userInv.Items.FindIndex(item => item.Name.Equals(Shop.getItem(name).Name) && item.Count > count);
            userInv.Items[itemToRemove].Count -= count; 

            connect.Execute("UPDATE UserStats SET JsonInv = ? WHERE Nick = ?", JsonConvert.SerializeObject(userInv).ToString(), username);
            return true;
        }

        public bool AddItemToInv(string name, string username, int count, SQLite.SQLiteConnection connect)
        {
            SQLTools sql = new SQLTools();

            var userinfo = sql.getUserInfo(connect, username);

            if (userinfo == null)
            {
                Log.Error("User " + username + " not exsist!");
                return false;
            }

            var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);

            bool append = false;

            foreach (var items in userInv.Items)
            {
                if (items.Name == Shop.getItem(name).Name)
                {
                    items.Count += count;
                    Log.Verbose("appending {0} count: {1}", items.Name, count);
                    append = true;
                    break;
                }
            }
            if (!append)
            {
                Log.Verbose("creaing {0} count: {1}", name, count);
                userInv.Items.Add(new Models.ItemInventory.Item() { Name = Shop.getItem(name).Name, Count = count });
            }
           
            connect.Execute("UPDATE UserStats SET JsonInv = ? WHERE Nick = ?", JsonConvert.SerializeObject(userInv).ToString(),  username);
            return true;
        }
    }
}
