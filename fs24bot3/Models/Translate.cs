using System.Collections.Generic;
using Newtonsoft.Json;

namespace fs24bot3.Models;

public class Translate
{
    public class Request
    {
        [JsonProperty("texts")] public List<string> Texts { get; set; }

        [JsonProperty("tl")] public string Tl { get; set; }

        [JsonProperty("sl")] public string Sl { get; set; }

        public Request(string source, string target, string text)
        {
            Sl = source;
            Tl = target;
            Texts = new List<string> { text };
        }
    }

    public class Response
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("texts")]
        public List<string> Texts { get; set; }

        [JsonProperty("tl")]
        public string Tl { get; set; }
    }

}
