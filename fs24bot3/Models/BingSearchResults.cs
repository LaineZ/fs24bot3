using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fs24bot3.Models;

public class BingSearchResults
{
    public class Item
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("answerType")]
        public string AnswerType { get; set; }

        [JsonProperty("resultIndex")]
        public int ResultIndex { get; set; }

        [JsonProperty("value")]
        public Value Value { get; set; }
    }

    public class Mainline
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }

    public class QueryContext
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("originalQuery")]
        public string OriginalQuery { get; set; }

        [JsonProperty("askUserForLocation")]
        public bool AskUserForLocation { get; set; }
    }

    public class RankingResponse
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("mainline")]
        public Mainline Mainline { get; set; }
    }

    public class Root
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("queryContext")]
        public QueryContext QueryContext { get; set; }

        [JsonProperty("webPages")]
        public WebPages WebPages { get; set; }

        [JsonProperty("rankingResponse")]
        public RankingResponse RankingResponse { get; set; }
    }

    public class Value
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("isFamilyFriendly")]
        public bool IsFamilyFriendly { get; set; }

        [JsonProperty("displayUrl")]
        public string DisplayUrl { get; set; }

        [JsonProperty("snippet")]
        public string Snippet { get; set; }

        [JsonProperty("dateLastCrawled")]
        public DateTime DateLastCrawled { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("isNavigational")]
        public bool IsNavigational { get; set; }

        [JsonProperty("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }
    }

    public class WebPages
    {
        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("webSearchUrl")]
        public string WebSearchUrl { get; set; }

        [JsonProperty("totalEstimatedMatches")]
        public ulong TotalEstimatedMatches { get; set; }

        [JsonProperty("value")]
        public List<Value> Value { get; set; }

        [JsonProperty("someResultsRemoved")]
        public bool SomeResultsRemoved { get; set; }
    }
}