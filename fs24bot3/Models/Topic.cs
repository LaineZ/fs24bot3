using Newtonsoft.Json;

namespace fs24bot3.Models;

public class Topic
{
    [JsonProperty("article")] public string Article { get; set; }

    [JsonProperty("language")] public string Language { get; set; }

    [JsonProperty("numOfTitles")] public int NumOfTitles { get; set; }

    public Topic(string article, string lauguage = "Russian", int numTitles = 1)
    {
        Article = article;
        Language = lauguage;
        NumOfTitles = numTitles;
    }
}