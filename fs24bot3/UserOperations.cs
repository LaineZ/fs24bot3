using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System.Linq;

namespace fs24bot3
{
    class UserOperations
    {
        public string Username;
        public SQLiteConnection Connect;

        public UserOperations(string username, SQLiteConnection Connection)
        {
            Username = username;
            Connect = Connection;
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

        public Models.SQL.UserStats GetUserInfo()
        {
            var query = Connect.Table<Models.SQL.UserStats>().Where(v => v.Nick.Equals(Username));
            foreach (var nick in query)
            {
                return nick;
            }
            return null;
        }

        public bool RemItemFromInv(string name, int count)
        {
            var userinfo = GetUserInfo();

            if (userinfo == null)
            {
                Log.Error("User " + Username + " not exsist!");
                return false;
            }

            var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);

            int itemToRemove = userInv.Items.FindIndex(item => item.Name.Equals(Shop.getItem(name).Name) && item.Count > count);
            if (itemToRemove > 0 && userInv.Items[itemToRemove].Count >= count)
            {
                userInv.Items[itemToRemove].Count -= count;
                Log.Information("User {0} removed {1} sucessfully!", name, count);
                Connect.Execute("UPDATE UserStats SET JsonInv = ? WHERE Nick = ?", JsonConvert.SerializeObject(userInv).ToString(), Username);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddItemToInv(string name, int count)
        {

            var userinfo = GetUserInfo();

            if (userinfo == null)
            {
                Log.Error("User " + Username + " not exsist!");
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
           
            Connect.Execute("UPDATE UserStats SET JsonInv = ? WHERE Nick = ?", JsonConvert.SerializeObject(userInv).ToString(), Username);
            return true;
        }
    }
}
