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
            [PrimaryKey]
            public string Nick { get; set; }

            public int Level { get; set; }
            public int Xp { get; set; }
            public int Need { get; set; }
            public int Admin { get; set; }
            public string AdminPassword { get; set; }
        }

        // ultimate table 99999
        internal class Item
        {
            [PrimaryKey]
            public string Name { get; set; }
        }


        internal class Inventory
        {
            public string Nick { get; set; }
            public string Item { get; set; }
            [Column("Count")]
            public int ItemCount { get; set; }
        }

        internal class CustomUserCommands
        {
            [PrimaryKey]
            public string Command { get; set; }

            public string Output { get; set; }
            public string Nick { get; set; }
        }

        internal class Tag
        {
            [SQLite.PrimaryKey]
            public string TagName { get; set; }

            public string Color { get; set; }
            public string Username { get; set; }
            [Column("Count")]
            public int TagCount { get; set; }
        }

        internal class Tags
        {
            [SQLite.PrimaryKey]
            public string Username { get; set; }
            public string JsonTag { get; set; }
        }
    }
}
