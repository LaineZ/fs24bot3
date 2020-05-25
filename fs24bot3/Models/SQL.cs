using SQLite;
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
            [PrimaryKey]
            public string TagName { get; set; }

            public string Color { get; set; }
            public string Username { get; set; }
            [Column("Count")]
            public int TagCount { get; set; }
        }

        internal class Tags
        {
            [PrimaryKey]
            public string Username { get; set; }
            public string JsonTag { get; set; }
        }

        // ultimate table99999
        internal class Ignore
        {
            [PrimaryKey]
            public string Username { get; set; }
        }

        internal class LyricsCache
        {
            [Column("track")]
            public string Track { get; set; }
            [Column("artist")]
            public string Artist { get; set; }
            [Column("lyrics")]
            public string Lyrics { get; set; }
            [Column("addedby")]
            public string AddedBy { get; set; }
        }

        // The next evolution in the world!!!
        internal class HttpCache
        {
            [PrimaryKey]
            public string URL { get; set; }
            public string Output { get; set; }
        }

        internal class UserSearchIgnores
        {
            [PrimaryKey]
            public string Username { get; set; }
            public string Urls { get; set; }
        }

        internal class UserFishingRods
        {
            [PrimaryKey]
            public string Username { get; set; }
            public string RodName { get; set; }
            public int RodDurabillity { get; set; }
            public string Nest { get; set; }
        }

        internal class FishingRods
        {
            [PrimaryKey]
            public string RodName { get; set; }
            public int RodDurabillity { get; set; }
            public int FishingLine { get; set; }
            public int HookSize { get; set; }
            public int Price { get; set; }
        }

        internal class FishingNests
        {
            [PrimaryKey]
            public string Name { get; set; }
            public int Level { get; set; }
            public int FishingLineRequired { get; set; }
            public int FishCount { get; set; }
        }
    }
}
