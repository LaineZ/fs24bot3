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
            [ManyToMany(typeof(Item))]
            public List<Item> Inv { get; set; }
        }

        internal class CustomUserCommands
        {
            [SQLite.PrimaryKey]
            public string Command { get; set; }

            public string Output { get; set; }
            public string Nick { get; set; }
        }

        public class Item
        {
            [PrimaryKey]
            public string Name { get; set; }

            public int Count { get; set; }

            [ForeignKey(typeof(SQL.UserStats))]
            public string Username { get; set; }
        }

        internal class Tag
        {
            [SQLite.PrimaryKey]
            public string TagName { get; set; }

            public string Color { get; set; }
            public string Username { get; set; }    
            public int Count { get; set; }
        }

        internal class Tags
        {
            [SQLite.PrimaryKey]
            public string Username { get; set; }
            public string JsonTag { get; set; }
        }
    }
}
