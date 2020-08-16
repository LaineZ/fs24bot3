using fs24bot3.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public static class Transalator
    {
        public async static Task<YandexTranslate.RootObject> Translate(string lang, string text)
        {
            HttpClient client = new HttpClient();
            var formVariables = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("text", text)
            };
            var formContent = new FormUrlEncodedContent(formVariables);

            var response = await client.PostAsync("https://translate.yandex.net/api/v1.5/tr.json/translate?lang=" + lang + "&key=" + Configuration.yandexTrKey, formContent);
            var responseString = await response.Content.ReadAsStringAsync();

            Log.Verbose(responseString);

            var translatedOutput = JsonConvert.DeserializeObject<YandexTranslate.RootObject>(responseString);

            if (translatedOutput.text != null)
            {
                return translatedOutput;
            }
            else
            {
                throw new Exception("Translate error:" + translatedOutput);
            }
        }
    }
}
