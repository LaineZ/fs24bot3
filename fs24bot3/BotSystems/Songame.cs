using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using fs24bot3.Models;
using SQLite;

namespace fs24bot3.BotSystems
{
    public class Songame
    {
        public int Tries { get; set; }
        [PrimaryKey]
        public string SongameString { get; set; }


        public string RemoveArticles(string line)
        {
            string[] art = new string[] { "the", "are", "a", "an", "i" };
            foreach (string word in art)
            {
                Regex regexArticle = new Regex(@"\b" + word + @"\b");
                line = regexArticle.Replace(line.ToLower(), " ");
            }

            // remove double spaces
            Regex regex = new Regex("[ ]{2,}");
            return new string(regex.Replace(line.Trim(), " ").ToCharArray().Where(c => !char.IsPunctuation(c)).ToArray());
        }

        public Songame(SQLiteConnection connect)
        {
            Tries = 5;
            SongameString = string.Empty;
            int timeout = 10;
            List<SQL.LyricsCache> query = connect.Query<SQL.LyricsCache>("SELECT * FROM LyricsCache");

            if (SongameString.Length == 0)
            {
                while (SongameString.Length == 0 && timeout > 0)
                {
                    if (query.Count > 0)
                    {
                        string[] lyrics = query.Random().Lyrics.Split("\n");

                        foreach (string line in lyrics)
                        {
                            if (Regex.IsMatch(line, @"^([A-Za-z\s]*)$"))
                            {
                                SongameString = RemoveArticles(line);
                                break;
                            }
                        }

                    }
                    timeout--;
                }
            }
        }
    }
}