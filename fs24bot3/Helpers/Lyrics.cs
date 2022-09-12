using fs24bot3.Core;
using fs24bot3.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace fs24bot3.Helpers;
public class Lyrics
{
    public string Artist;
    public string Track;
    
    private SQLiteConnection Connection;
    private static readonly HttpClient client = new HttpClient();
    
    public Lyrics(string artist, string track, in SQLiteConnection connect)
    {
        Artist = artist;
        Track = track;
        Connection = connect;
    }

    public async Task<string> GetLyrics()
    {
        var query = Connection
            .Table<SQL.LyricsCache>()
            .FirstOrDefault(v => v.Artist.ToLower() == Artist.ToLower() && 
                                 v.Track.ToLower() == Track.ToLower());
        
        if (query != null)
        {
            Log.Verbose("Use cached lyrics");
            return query.Lyrics;
        } ;
        
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://powerlyrics.p.rapidapi.com/getlyricsfromtitleandartist?title=" + Track
            + "&artist=" + Artist),
            Headers = {
                { "x-rapidapi-key", ConfigurationProvider.Config.Services.RapidApiKey },
                { "x-rapidapi-host", "powerlyrics.p.rapidapi.com" },
            },
        };

        var response = await client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        
        var json = JsonConvert.DeserializeObject<LyricsResponse>(responseString, 
            JsonSerializerHelper.OPTIMIMAL_SETTINGS);


        if (json != null && json.Success)
        {
            var lyricsToCache = new SQL.LyricsCache()
            {
                AddedBy = null,
                Artist = Artist,
                Track = Track,
                Lyrics = json.Lyrics
            };

            Connection.Insert(lyricsToCache);
            return json.Lyrics;
        }

        return "Instrumental";
    }
}
