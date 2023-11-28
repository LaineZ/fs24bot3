﻿using fs24bot3.ItemTraits;
using SQLite;
using System;

namespace fs24bot3.Models;

public class SQL
{
    public class UserStats
    {
        [PrimaryKey]
        public string Nick { get; set; }
        public int Level { get; set; }
        public int Xp { get; set; }
        public int Need { get; set; }
        public int Admin { get; set; }
        public int LastMsg { get; set; }
        public string Prefix { get; set; }
        public string Timezone { get; set; }
        public string City { get; set; }
    }

    public class Item
    {
        [PrimaryKey]
        public string Name { get; set; }

        public string ShopID { get; set; }

        public static explicit operator Item(BasicItem v)
        {
            return new Item() { Name = v.Name };
        }
    }


    public class ScriptStorage
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Command { get; set; }
        public string Nick { get; set; }
        public string Data { get; set; }
    }

    public class Chars
    {
        [PrimaryKey]
        public string Symbol { get; set; }
        public string Hexcode { get; set; }
        public string Name { get; set; }
    }

    public class Warnings
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public string Message { get; set; }
        public string Nick { get; set; }
    }

    public class Reminds
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public long RemindDate { get; set; }
        public string Nick { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
    }

    // for migration
    public class RemindsOld
    {
        [PrimaryKey]
        public uint RemindDate { get; set; }
        public string Nick { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
    }

    public class Inventory
    {
        public string Nick { get; set; }
        public string Item { get; set; }
        [Column("Count")]
        public int ItemCount { get; set; }
    }

    public class CustomUserCommands
    {
        [PrimaryKey]
        public string Command { get; set; }

        public string Output { get; set; }
        public string Nick { get; set; }
        public int IsLua { get; set; }
    }

    public class Tag
    {
        [PrimaryKey]
        public string Name { get; set; }
        public uint Color { get; set; }
        public string CreatedBy { get; set; }
    }

    public class Tags
    {
        [PrimaryKey]
        public string Nick { get; set; }
        public string Tag { get; set; }
    }

    // ultimate table99999
    public class Ignore
    {
        [PrimaryKey]
        public string Username { get; set; }
    }

    public class UtfCharacters
    {
        [PrimaryKey]
        public string HexCode { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

    public class FishingNests
    {
        [PrimaryKey]
        public string Name { get; set; }
        public int Level { get; set; }
        public int FishingLineRequired { get; set; }
        public int FishCount { get; set; }
    }

    public class Fishing
    {
        [PrimaryKey]
        public string Nick { get; set; }
        public int Level { get; set; }
        public string NestName { get; set; }
    }

    public class UnhandledExceptions
    {
        [PrimaryKey]
        public string Date { get; set; }
        public string ErrorMessage { get; set; }
        public string Username { get; set; }
        public string Input { get; set; }

        public UnhandledExceptions(string err, string username, string input)
        {
            Date = DateTime.Now.ToString();
            ErrorMessage = err;
            Username = username;
            Input = input;
        }
    }
}
