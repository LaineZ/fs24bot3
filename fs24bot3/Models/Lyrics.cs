using Newtonsoft.Json;
using System;

namespace fs24bot3.Models;

public class LyricsResponse
{
    [JsonProperty("success")] public bool Success { get; set; }
    [JsonProperty("requestedtitle")] public string Requestedtitle { get; set; }
    [JsonProperty("requestedartist")] public string Requestedartist { get; set; }
    [JsonProperty("resolvedtitle")] public string Resolvedtitle { get; set; }
    [JsonProperty("resolvedartist")] public string Resolvedartist { get; set; }
    [JsonProperty("lyrics")] public string Lyrics { get; set; }
}