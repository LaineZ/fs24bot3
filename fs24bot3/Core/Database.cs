﻿using fs24bot3.Helpers;
using fs24bot3.Models;
using HandlebarsDotNet;
using Serilog;
using SQLite;
using System;
using System.Linq;

namespace fs24bot3.Core;
public class Database
{
    public static void InitDatabase(in SQLiteConnection connection)
    {
        Log.Information("Initializing databases");
        connection.CreateTable<SQL.Cache>();
        connection.CreateTable<SQL.UserStats>();
        connection.CreateTable<SQL.CustomUserCommands>();
        connection.CreateTable<SQL.Tag>();
        connection.CreateTable<SQL.Item>();
        connection.CreateTable<Permissions>();
        connection.CreateTable<SQL.ScriptStorage>();
        connection.CreateTable<SQL.Goals>();
        
        connection.DropTable<SQL.Messages>();
        connection.CreateTable<SQL.Messages>();

        try
        {
            connection.CreateTable<SQL.Reminds>();
        }
        catch (SQLiteException)
        {
            // Reminds: Migrate from old one to new
            connection.Execute("ALTER TABLE Reminds RENAME TO RemindsOld");
            connection.CreateTable<SQL.Reminds>();
            var query = connection.Table<SQL.RemindsOld>().ToList();

            foreach (var item in query)
            {
                connection.Insert(new SQL.Reminds
                {
                    Channel = item.Channel,
                    Message = item.Message,
                    Nick = item.Nick,
                    RemindDate = item.RemindDate
                });
            }

            connection.DropTable<SQL.RemindsOld>();
        }
        
        connection.CreateTable<SQL.UtfCharacters>();
        connection.CreateTable<SQL.Fishing>();
        connection.CreateTable<SQL.FishingNests>();
        connection.CreateTable<SQL.Warnings>();

        // creating ultimate inventory by @Fingercomp
        connection.Execute("CREATE TABLE IF NOT EXISTS Inventory (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Item NOT NULL REFERENCES Item (Name) ON DELETE CASCADE ON UPDATE CASCADE, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Nick, Item))");
        connection.Execute("CREATE TABLE IF NOT EXISTS Tags (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Tag NOT NULL REFERENCES Tag (Name) ON DELETE CASCADE ON UPDATE CASCADE, PRIMARY KEY (Nick, Tag))");

        connection.Execute("CREATE TABLE IF NOT EXISTS LyricsCache (track TEXT, artist TEXT, lyrics TEXT, addedby TEXT, PRIMARY KEY (track, artist))");

        // generate fishing nests
        var rand = new Random();
        if (!connection.Table<SQL.FishingNests>().Any())
        {
            Log.Information("Generating fishing nests...");
            connection.BeginTransaction();
            for (int i = 0; i < 100; i++)
            {
                connection.InsertOrReplace(
                    new SQL.FishingNests()
                    {
                        Level = rand.Next(1, 3), 
                        FishCount = rand.Next(1, 20), 
                        FishingLineRequired = rand.Next(1, 10), 
                        Name = MessageHelper.GenerateName(rand.Next(2, 5))
                    });
            }
            connection.Commit();
        }
        Log.Information("Vacuuming database this can take some time...");
        connection.Execute("VACUUM");
        Log.Information("Databases loaded!");
    }
}
