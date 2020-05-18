﻿using fs24bot3.Models;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fs24bot3.Core
{
    public static class Database
    {
        public static void InitDatabase(SQLiteConnection connection)
        {
            Log.Information("Initializing databases");
            var CacheConnection = new SQLiteConnection("fscache.sqlite");

            Configuration.LoadConfiguration();
            connection.CreateTable<SQL.UserStats>();
            connection.CreateTable<SQL.CustomUserCommands>();
            connection.CreateTable<SQL.Tag>();
            connection.CreateTable<SQL.Item>();
            connection.CreateTable<SQL.Tags>();
            connection.CreateTable<SQL.Ignore>();
            connection.CreateTable<SQL.FishingRods>();
            connection.CreateTable<SQL.FishingNests>();
            connection.CreateTable<SQL.UserFishingRods>();
            CacheConnection.CreateTable<SQL.HttpCache>();
            CacheConnection.Close();
            CacheConnection.Dispose();

            Shop.Init(connection);

            // creating ultimate inventory by @Fingercomp
            connection.Execute("CREATE TABLE IF NOT EXISTS Inventory (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Item NOT NULL REFERENCES Item (Name) ON DELETE CASCADE ON UPDATE CASCADE, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Nick, Item))");

            connection.Execute("CREATE TABLE IF NOT EXISTS LyricsCache (track TEXT, artist TEXT, lyrics TEXT, addedby TEXT, PRIMARY KEY (track, artist))");
            Log.Information("Databases loaded!");
        }
    }
}