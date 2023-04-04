using Newtonsoft.Json;
using System.Collections.Generic;


namespace fs24bot3.Models;

public class Youtube
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class HttpHeaders
    {
        [JsonProperty("User-Agent")]
        public string UserAgent { get; set; }
        public string Accept { get; set; }

        [JsonProperty("Accept-Language")]
        public string AcceptLanguage { get; set; }

        [JsonProperty("Accept-Charset")]
        public string AcceptCharset { get; set; }

        [JsonProperty("Accept-Encoding")]
        public string AcceptEncoding { get; set; }
    }

    public class DownloaderOptions
    {
        public int http_chunk_size { get; set; }
    }

    public class RequestedFormat
    {
        public string format_note { get; set; }
        public string acodec { get; set; }
        public double tbr { get; set; }
        public int? width { get; set; }
        public double? fps { get; set; }
        public double vbr { get; set; }
        public string container { get; set; }
        public HttpHeaders http_headers { get; set; }
        public int? asr { get; set; }
        public string format_id { get; set; }
        public string vcodec { get; set; }
        public string format { get; set; }
        public int? height { get; set; }
        public string url { get; set; }
        public string ext { get; set; }
        public double quality { get; set; }
        public long filesize { get; set; }
        public string protocol { get; set; }
        public DownloaderOptions downloader_options { get; set; }
        public double? abr { get; set; }
    }

    public class Format
    {
        public string format_note { get; set; }
        public string acodec { get; set; }
        public double tbr { get; set; }
        public int? width { get; set; }
        public double? fps { get; set; }
        public string format { get; set; }
        public string vcodec { get; set; }
        public string container { get; set; }
        public double abr { get; set; }
        public HttpHeaders http_headers { get; set; }
        public string format_id { get; set; }
        public int? asr { get; set; }
        public string ext { get; set; }
        public int? height { get; set; }
        public string url { get; set; }
        public long filesize { get; set; }
        public double quality { get; set; }
        public string protocol { get; set; }
        public DownloaderOptions downloader_options { get; set; }
        public double? vbr { get; set; }
    }

    public class Thumbnail
    {
        public string resolution { get; set; }
        public int width { get; set; }
        public string id { get; set; }
        public int height { get; set; }
        public string url { get; set; }
    }

    public class Root
    {
        public object resolution { get; set; }
        public object is_live { get; set; }
        public List<string> categories { get; set; }
        public List<RequestedFormat> requested_formats { get; set; }
        public string format_id { get; set; }
        public double abr { get; set; }
        public int height { get; set; }
        public double fps { get; set; }
        public object playlist { get; set; }
        public List<Format> formats { get; set; }
        public string uploader_url { get; set; }
        public string channel { get; set; }
        public List<object> tags { get; set; }
        public int width { get; set; }
        public int view_count { get; set; }
        public int duration { get; set; }
        public string fulltitle { get; set; }
        public string webpage_url { get; set; }
        public string channel_id { get; set; }
        public string _filename { get; set; }
        public object stretched_ratio { get; set; }
        public string uploader { get; set; }
        public string title { get; set; }
        public string acodec { get; set; }
        public string description { get; set; }
        public string ext { get; set; }
        public string webpage_url_basename { get; set; }
        public double vbr { get; set; }
        public List<Thumbnail> thumbnails { get; set; }
        public string thumbnail { get; set; }
        public string vcodec { get; set; }
        public string extractor_key { get; set; }
        public object playlist_index { get; set; }
        public object average_rating { get; set; }
        public int like_count { get; set; }
        public string uploader_id { get; set; }
        public int age_limit { get; set; }
        public string id { get; set; }
        public string display_id { get; set; }
        public string format { get; set; }
        public string channel_url { get; set; }
        public string upload_date { get; set; }
        public string extractor { get; set; }
        public object requested_subtitles { get; set; }
    }


}
