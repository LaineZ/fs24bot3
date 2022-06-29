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
            var query = Connect.Table<SQL.Inventory>().Where(x => x.Item == itemname).Average(x => x.ItemCount);
            return query;
        }
    }
}
