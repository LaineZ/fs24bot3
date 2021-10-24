using fs24bot3.Core;
using fs24bot3.Models;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fs24bot3
{
    class MultiUser
    {
        public SQLiteConnection Connect;

        public MultiUser(SQLiteConnection connection)
        {
            Connect = connection;
        }

        public double GetItemAvg(string itemname = "money")
        {
            List<int> money = new List<int>();

            var query = Connect.Table<SQL.UserStats>();
            foreach (var users in query)
            {
                User user = new User(users.Nick, Connect);
                var itemToCount = user.GetInventory().Find(item => item.Item == itemname);
                if (itemToCount != null)
                {
                    money.Add(itemToCount.ItemCount);
                }
            }
            return money.Any() ? money.Average() : 0;
        }
    }
}
