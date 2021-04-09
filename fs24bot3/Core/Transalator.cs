using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public static class Transalator
    {
        public async static Task<dynamic> Translate(string text, string fromLang = "auto-detect", string toLang = "auto-detect")
        {
            HttpClient client = new HttpClient();

            var formVariables = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("fromLang", fromLang),
                new KeyValuePair<string, string>("to", toLang),
                new KeyValuePair<string, string>("text", text)
            };
            var formContent = new FormUrlEncodedContent(formVariables);

            var response = await client.PostAsync("https://www.bing.com/ttranslatev3?isVertical=1&=&IG=3E71308DE83C4B7EAF0A9A024F08A591&IID=translator.5026.1", formContent);
            var responseString = await response.Content.ReadAsStringAsync();

            Log.Verbose(responseString);

            if (responseString.Any() && response.StatusCode == System.Net.HttpStatusCode.OK)
            {

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    Error = delegate (object sender, ErrorEventArgs args)
                    {
                        Log.Error("JSON ERROR: {0}", args.ErrorContext.Error.Message);
                    },
                };

                dynamic translatedOutput = JsonConvert.DeserializeObject<dynamic>(responseString);
                return translatedOutput[0].translations[0];
            }

            throw new Exception(responseString);

        }

        public async static Task<string> TranslatePpc(string text)
        {
            string[] translations = { "ru", "ar", "pl", "fr", "ja", "es", "ro", "de", "ru" };
            string translated = text;

            foreach (var tr in translations)
            {
                var translatorResponse = await Translate(translated, "auto-detect", tr);
                translated = translatorResponse.text;
            }

            return translated;
        }
    }
}
