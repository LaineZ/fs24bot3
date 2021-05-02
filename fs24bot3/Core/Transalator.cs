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


        public async static Task<dynamic> Translate(string text, string fromLang = "auto-detect", string toLang = "auto-detect")
        {
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

        private async static Task<string> LibreTranslate(string text, string fromLang = "auto", string toLang = "auto")
        {
            var data = new Models.LibreTranslate.TranslateQuery()
            {
                q = text,
                source = fromLang,
                target = toLang
            };

            HttpContent c = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://libretranslate.com/translate", c);
            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString.Any() && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var translatedOutput = JsonConvert.DeserializeObject<Models.LibreTranslate.TranslateOut>(responseString);
                Log.Verbose(translatedOutput.translatedText);
                return translatedOutput.translatedText;
            }

            throw new Exception("Translate server error: " + response.StatusCode.ToString());
        }

        public async static Task<string> TranslatePpc(string text)
        {
            string[] translations = { "en", "pt", "ja", "de", "ar", "ru"};
            string translated = text;

            foreach (var tr in translations)
            { 

                var translatorResponse = await LibreTranslate(translated, "auto", tr);
                translated = translatorResponse;
            }

            return translated;
        }
    }
}
