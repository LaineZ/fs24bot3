using System.Collections.Generic;
using Newtonsoft.Json;

namespace fs24bot3.Models;

public class Searx
{
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Result
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("engine")]
        public string Engine { get; set; }

        [JsonProperty("parsed_url")]
        public List<string> ParsedUrl { get; set; }

        [JsonProperty("template")]
        public string Template { get; set; }

        [JsonProperty("engines")]
        public List<string> Engines { get; set; }

        [JsonProperty("positions")]
        public List<int> Positions { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("pretty_url")]
        public string PrettyUrl { get; set; }

        [JsonProperty("open_group")]
        public bool OpenGroup { get; set; }

        [JsonProperty("close_group")]
        public bool? CloseGroup { get; set; }
    }

    public class Root
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("number_of_results")]
        public int NumberOfResults { get; set; }

        [JsonProperty("results")]
        public List<Result> Results { get; set; }

        [JsonProperty("answers")]
        public List<object> Answers { get; set; }

        [JsonProperty("corrections")]
        public List<object> Corrections { get; set; }

        [JsonProperty("infoboxes")]
        public List<object> Infoboxes { get; set; }

        [JsonProperty("suggestions")]
        public List<object> Suggestions { get; set; }

        [JsonProperty("unresponsive_engines")]
        public List<List<string>> UnresponsiveEngines { get; set; }
    }



}