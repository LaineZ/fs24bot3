using fs24bot3.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

            string content = "[" + JsonConvert.SerializeObject(new BingTranlate.Request() { Text = text }) + "]";

            var request = new HttpRequestMessage() {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://microsoft-translator-text.p.rapidapi.com/translate?api-version=3.0&to=" + to + "&textType=plain&profanityAction=NoAction&from=" + from),
                Headers = {
                    { "x-rapidapi-key", Configuration.TranslateKey },
                    { "x-rapidapi-host", "microsoft-translator-text.p.rapidapi.com" },
                },
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };

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

                return JsonConvert.DeserializeObject<BingTranlate.Root>(responseString[1..^1]);
            }

            throw new Exception(responseString);
        }

        public async static Task<string> TranslatePpc(string text, string targetLang = "ru")
        {
            string[] translations = { "en", "pl", "pt", "ja", "de", "ru" };
            Random random = new Random();
            var translationsShuffled = translations.OrderBy(x => random.Next()).ToList();
            translationsShuffled.Add(targetLang);
            string translated = string.Join(" ", text.Split(" ").OrderBy(x => random.Next()).ToList());

            foreach (var tr in translationsShuffled)
            {
                try
                {
                    var translatorResponse = await TranslateBing(translated, "", tr);
                    translated = translatorResponse.translations.First().text;
                }
                catch (Exception)
                {
                    continue;
                }
            }

            return translated;
        }
    }
}
