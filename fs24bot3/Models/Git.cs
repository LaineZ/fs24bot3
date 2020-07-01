using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fs24bot3.Models
{
    public class Git
    {
        public partial class Root
        {
            [JsonProperty("ref")]
            public string Ref { get; set; }

            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("object")]
            public Object Object { get; set; }
        }

        public partial class Object
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("sha")]
            public string Sha { get; set; }

            [JsonProperty("url")]
            public Uri Url { get; set; }
        }
    }
}
