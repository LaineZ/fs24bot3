using HtmlAgilityPack;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using SQLite;
using fs24bot3.Models;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;

namespace fs24bot3.Core
{
    public class Lyrics
    {
        public string Artist;
        public string Track;
        private SQLiteConnection Connection;

        public Lyrics(string artist, string track, SQLiteConnection connect)
        {
            Artist = artist.Replace(" ", "-");
            Track = track.Replace(" ", "-");
            Connection = connect;
        }

        public async Task<string> GetLyrics()
        {
            var query = Connection.Table<SQL.LyricsCache>().Where(v => v.Artist.ToLower().Equals(Artist.ToLower()) && v.Track.ToLower().Equals(Track.ToLower())).FirstOrDefault();
            if (query != null)
            {
                Log.Verbose("Use cached lyrics");
                return query.Lyrics;
            }
            else
            {
                Log.Verbose("Using internet");
                string lyric = "";
                var web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync("https://genius.com/" + Artist + "-" + Track  + "-lyrics");
                HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[contains(@class, \"Lyrics__Container\")]");
                if (divContainer != null)
                {
                    foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//br"))
                        node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

                    foreach (var node in divContainer)
                    {
                        Log.Verbose(node.InnerText);
                        StringWriter writer = new StringWriter();

                        HttpUtility.HtmlDecode(node.InnerText, writer);

                        lyric += writer.ToString();
                    }
                }
                else
                {
                    throw new Exception("Lyrics not found!");
                }

                var lyricsToCache = new SQL.LyricsCache()
                {
                    AddedBy = null,
                    Artist = Artist,
                    Track = Track,
                    Lyrics = lyric
                };
                Connection.Insert(lyricsToCache);

                return lyric;
            }
        }
    }
}
