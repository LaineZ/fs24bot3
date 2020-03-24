using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace fs24bot3.Core
{
    public class Lyrics
    {
        public string Artist;
        public string Track;

        public Lyrics(string artist, string track)
        {
            Artist = artist;
            Track = track;
        }

        private string Retry(string rule, HtmlDocument doc)
        {
            HtmlNodeCollection retry = doc.DocumentNode.SelectNodes(rule);
            if (retry != null)
            {
                foreach (var node in retry)
                {
                    Log.Verbose(node.InnerText);
                    StringWriter lyricsWriter = new StringWriter();
                    HttpUtility.HtmlDecode(node.InnerText, lyricsWriter);
                    return lyricsWriter.ToString();
                }
            }
            return null;
        }

        private async Task<(string, bool)> GetLyricsInternal(string url)
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync("https://lyrics.fandom.com/wiki/" + url);
            HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[@class='lyricbox']");
            if (divContainer != null)
            {
                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//br"))
                    node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

                foreach (var node in divContainer)
                {
                    Log.Verbose(node.InnerText);
                    StringWriter lyricsWriter = new StringWriter();
                    HttpUtility.HtmlDecode(node.InnerText, lyricsWriter);
                    return (lyricsWriter.ToString(), false);
                }
            }

            else
            {
                // "//ul[@class='redirectText']"
                string retryRedirect;
                string[] retryVariants = { "//ul[@class='redirectText']", "//span[@class='alternative-suggestion']/a" };
                foreach (string variants in retryVariants)
                {
                    retryRedirect = Retry(variants, doc);
                    if (retryRedirect != null)
                    {
                        Log.Information("Trying get lyrics with type: {0} got: {1}", variants, retryRedirect);
                        return await GetLyricsInternal(retryRedirect);
                    }
                }
            }

            throw new Exception("Lyrics not found!");
        }

        public async Task<string> GetLyrics()
        {
            (string lyrics, bool redirect) = await GetLyricsInternal(Artist + ":" + Track);
            if (!redirect)
            {
                return lyrics;
            }
            else
            {
                (string lyricsFixed, bool _) = await GetLyricsInternal(lyrics);
                return lyricsFixed;
            }
        }
    }
}
