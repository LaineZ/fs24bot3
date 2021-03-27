using fs24bot3.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace fs24bot3.Core
{
    public class Lyrics
    {
        public string Artist;
        public string Track;
        private SQLiteConnection Connection;

        public Lyrics(string artist, string track, SQLiteConnection connect)
        {
            Artist = artist;
            Track = track;
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
                string lyric = string.Empty;
                string url = string.Empty;
                Regex removeVerse = new Regex("\\[.*\\]");

                string response = await new HttpTools().MakeRequestAsync("https://genius.com/api/search/multi?q= " + Artist + " - " + Track);

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                Models.Lyrics search = JsonConvert.DeserializeObject<Models.Lyrics>(response, settings);


                foreach (var item in search.Response.Sections[0].Hits)
                {
                    if (item.Result.Type == "song")
                    {
                        url = item.Result.Path;
                        Log.Information(url);
                        break;
                    }
                }

                if (string.IsNullOrEmpty(url))
                {
                    throw new Exception("Song not found!");
                }

                var web = new HtmlWeb();
                var doc = await web.LoadFromWebAsync("https://genius.com" + url);
                File.WriteAllText("debug.txt", doc.Text);
                HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[contains(@class, \"Lyrics__Container\")]");

                // workaround because genius.com have 2 formats? with lyrics class and Lyrics__Container class
                if (divContainer == null)
                {
                    divContainer = doc.DocumentNode.SelectNodes("//div[@class=\"lyrics\"]");
                }

                foreach (var node in divContainer)
                {
                    lyric += HttpUtility.HtmlDecode(node.InnerHtml.Replace("<br>", "\n"));
                }

                lyric = Regex.Replace(lyric, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                lyric = Regex.Replace(lyric, @"<[^>]*>", String.Empty, RegexOptions.Multiline);

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
