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
            string[] translations = { "en", "pt", "ja", "de", "ar", "ru"};
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
