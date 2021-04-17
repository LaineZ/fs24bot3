using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    class Searx
    {
        public class Result
        {
            public string url { get; set; }
            public string title { get; set; }
            public string content { get; set; }
            public string engine { get; set; }
            public List<string> parsed_url { get; set; }
            public List<string> engines { get; set; }
            public List<int> positions { get; set; }
            public double score { get; set; }
            public string category { get; set; }
            public string pretty_url { get; set; }
        }

        public class Url
        {
            public string title { get; set; }
            public string url { get; set; }
        }

        public class Infobox
        {
            public string infobox { get; set; }
            public string id { get; set; }
            public string content { get; set; }
            public string img_src { get; set; }
            public List<Url> urls { get; set; }
            public string engine { get; set; }
            public List<string> engines { get; set; }
            public List<object> attributes { get; set; }
        }

        public class Root
        {
            public string query { get; set; }
            public double number_of_results { get; set; }
            public List<Result> results { get; set; }
            public List<object> answers { get; set; }
            public List<object> corrections { get; set; }
            public List<Infobox> infoboxes { get; set; }
            public List<string> suggestions { get; set; }
            public List<object> unresponsive_engines { get; set; }
        }
    }
}
