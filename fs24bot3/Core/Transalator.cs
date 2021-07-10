using fs24bot3.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public static class Transalator
    {
        public static bool AlloPpc = true;
        private static readonly HttpClient client = new HttpClient();

        public async static Task<BingTranlate.Root> TranslateBing(string text, string from = "", string to = "")
        {

            var request = new HttpRequestMessage() {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://microsoft-translator-text.p.rapidapi.com/translate?api-version=3.0&to=" + to + "&textType=plain&profanityAction=NoAction&from=" + from),
                Headers = {
                    { "x-rapidapi-key", Configuration.translateKey },
                    { "x-rapidapi-host", "microsoft-translator-text.p.rapidapi.com" },
                },
                Content = new StringContent("[ { \"Text\": \"" + text + "\" } ]", Encoding.UTF8, "application/json"),
            };

            //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);
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

                return JsonConvert.DeserializeObject<BingTranlate.Root>(responseString.Substring(1, responseString.Length-2));
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

        public async static Task<string> TranslatePpc(string text, string targetLang = "ru")
        {
            string[] translations = { "en", "pt", "ja", "de", "ar", targetLang };
            string translated = text;

            foreach (var tr in translations)
            {

                var translatorResponse = await TranslateBing(translated, "auto", tr);
                translated = translatorResponse.translations[0].text;
            }

            return translated;
        }
    }
}
