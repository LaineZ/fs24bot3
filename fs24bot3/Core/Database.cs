using fs24bot3.Models;
using Serilog;
using SQLite;
using System;

namespace fs24bot3.Core
{
    public class Database
    {
        public static void InitDatabase(SQLiteConnection connection)
        {
            Log.Information("Initializing databases");
            Configuration.LoadConfiguration();
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

            // creating ultimate inventory by @Fingercomp
            connection.Execute("CREATE TABLE IF NOT EXISTS Inventory (Nick NOT NULL REFERENCES UserStats (Nick) ON DELETE CASCADE ON UPDATE CASCADE, Item NOT NULL REFERENCES Item (Name) ON DELETE CASCADE ON UPDATE CASCADE, Count INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (Nick, Item))");

            connection.Execute("CREATE TABLE IF NOT EXISTS LyricsCache (track TEXT, artist TEXT, lyrics TEXT, addedby TEXT, PRIMARY KEY (track, artist))");

            // generate fishing nests
            var rand = new Random();
            if (connection.Table<SQL.FishingNests>().Count() == 0) {
                Log.Information("Generating fishing nests...");
                for (int i = 0; i < 100; i++)
                {
                    connection.InsertOrReplace(new SQL.FishingNests() { Level = rand.Next(1, 3), FishCount = rand.Next(1, 20), FishingLineRequired = rand.Next(1, 10), Name = Core.MessageUtils.GenerateName(rand.Next(2, 5)) });   
                }
            }
            Log.Information("Databases loaded!");
        }

        public static string GetRandomLyric(SQLiteConnection connection)
        {
            var query = connection.Table<SQL.LyricsCache>().ToList();
            string outputmsg = "";

            if (query.Count > 0)
            {
                Random rand = new Random();
                string[] lyrics = query[rand.Next(0, query.Count - 1)].Lyrics.Split("\n");
                int baseoffset = rand.Next(0, lyrics.Length - 1);

                for (int i = 0; i < rand.Next(1, 5); i++)
                {
                    if (lyrics.Length > baseoffset + i) { outputmsg += " " + lyrics[baseoffset + i].Trim(); }
                }
            }

            return outputmsg;
        }
    }
}
