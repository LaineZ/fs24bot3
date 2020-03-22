using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class SQLUser
    {

        public class UserStats
        {
            public string Nick { get; set; }
            public int Level { get; set; }
            public int Xp { get; set; }
            public int Need { get; set; }
            public int Admin { get; set; }
            public string AdminPassword { get; set; }
            public string JsonInv { get; set; }
        }
    }
}
