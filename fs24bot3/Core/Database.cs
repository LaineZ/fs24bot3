using fs24bot3.Helpers;
using fs24bot3.Models;
using Serilog;
using SQLite;
using System;

namespace fs24bot3.Core;
public class Database
{
    public static void InitDatabase(SQLiteConnection connection)
    {
        Log.Information("Initializing databases");
        connection.CreateTable<SQL.UserStats>();
        connection.CreateTable<SQL.CustomUserCommands>();
        connection.CreateTable<SQL.Tag>();
        connection.CreateTable<SQL.Tags>();
        connection.CreateTable<SQL.Item>();
        connection.CreateTable<SQL.Ignore>();
        connection.CreateTable<SQL.ScriptStorage>();
        connection.CreateTable<SQL.Reminds>();
        connection.CreateTable<SQL.UtfCharacters>();
        connection.CreateTable<SQL.UnhandledExceptions>();
        connection.CreateTable<SQL.Fishing>();
        connection.CreateTable<SQL.FishingNests>();
        connection.CreateTable<SQL.Warnings>();

        // creating ultimate inventory by @Fingercomp
        connection.Execute("CREATE TABLE IF NOT EXISTS Inventory (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Item NOT NULL REFERENCES Item (Name) ON DELETE CASCADE ON UPDATE CASCADE, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Nick, Item))");

        connection.Execute("CREATE TABLE IF NOT EXISTS LyricsCache (track TEXT, artist TEXT, lyrics TEXT, addedby TEXT, PRIMARY KEY (track, artist))");

        // generate fishing nests
        var rand = new Random();
        if (connection.Table<SQL.FishingNests>().Count() == 0)
        {
            Log.Information("Generating fishing nests...");
            connection.BeginTransaction();
            for (int i = 0; i < 100; i++)
            {
                connection.InsertOrReplace(new SQL.FishingNests() { Level = rand.Next(1, 3), FishCount = rand.Next(1, 20), FishingLineRequired = rand.Next(1, 10), Name = MessageHelper.GenerateName(rand.Next(2, 5)) });
            }
            connection.Commit();
        }
        Log.Information("Databases loaded!");
    }
}
