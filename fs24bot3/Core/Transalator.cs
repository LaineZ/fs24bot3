using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public static class Transalator
    {
        public static bool AlloPpc = true;
        private static readonly HttpClient client = new HttpClient();

        public async static Task<dynamic> TranslateBing(string text, string fromLang = "auto-detect", string toLang = "auto-detect")
        {
            var formVariables = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("fromLang", fromLang),
                new KeyValuePair<string, string>("to", toLang),
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("token", "S9IELVyAtekLubrRI1JOMXrMKFkWm0zA"),
                new KeyValuePair<string, string>("key", "1622358632959"),
            };
            var formContent = new FormUrlEncodedContent(formVariables);

            var response = await client.PostAsync("https://www.bing.com/ttranslatev3?isVertical=1&=&IG=194E5197E1524D0B882A621E1C70D325&IID=translator.5023.3", formContent);
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


        public async static Task<string> Translate(string text, string fromLang = "auto", string toLang = "auto")
        {
            var data = new Models.Translate.TranslateQuery()
            {
                q = text.TrimEnd(),
                source = fromLang,
                target = toLang
            };

            HttpResponseMessage response = new HttpResponseMessage();

            foreach (var url in new string[] { "https://translate.dafnik.me/", "https://translate.astian.org/" })
            {
                HttpContent c = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                response = await client.PostAsync(url + "translate", c);
                var responseString = await response.Content.ReadAsStringAsync();

                Log.Verbose("CODE: {0}", response.StatusCode);

                if (responseString.Any() && response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var translatedOutput = JsonConvert.DeserializeObject<Models.Translate.TranslateOut>(responseString);
                    Log.Verbose(translatedOutput.translatedText);
                    return translatedOutput.translatedText;
                }
            }

            throw new Exception("Translate server error! " + response.StatusCode);
        }

        public async static Task<string> TranslatePpc(string text)
        {
            string[] translations = { "en", "pt", "ja", "de", "ar", "ru" };
            string translated = text;

            foreach (var tr in translations)
            {

                var translatorResponse = await Translate(translated, "auto", tr);
                translated = translatorResponse;
            }

            return translated;
        }
    }
}
