using SQLite;
using SQLiteNetExtensions.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    internal class SQL
    {

        internal class UserStats
        {
            [SQLite.PrimaryKey]
            public string Nick { get; set; }

            public int Level { get; set; }
            public int Xp { get; set; }
            public int Need { get; set; }
            public int Admin { get; set; }
            public string AdminPassword { get; set; }
            public string JsonInv { get; set; }
        }

        internal class CustomUserCommands
        {
            [SQLite.PrimaryKey]
            public string Command { get; set; }

            public string Output { get; set; }
            public string Nick { get; set; }
        }
    }
}
