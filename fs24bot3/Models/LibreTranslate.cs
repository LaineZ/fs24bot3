using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Models
{
    public class LibreTranslate
    {
        public class DetectedLanguage
        {
            [JsonProperty("language")]
            public string Language { get; set; }
        }

        public class Response
        {
            [JsonProperty("detectedLanguage")]
            public DetectedLanguage DetectedLanguage { get; set; }

            [JsonProperty("translatedText")]
            public string TranslatedText { get; set; }
        }


        public class Request
        {
            [JsonProperty("q")]
            public string RequestText { get; set; }

            [JsonProperty("source")]
            public string Source { get; set; }

            [JsonProperty("target")]
            public string Target { get; set; }

            [JsonProperty("format")]
            public string Format { get; set; }

            [JsonProperty("api_key")]
            public string ApiKey { get; set; }
        }

    }
}
