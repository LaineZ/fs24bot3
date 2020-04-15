using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3
{
    class HttpTools
    {
        HttpClient client = new HttpClient();
        private SQLiteConnection Connection = new SQLiteConnection("fscache.sqlite");

        public async Task<String> MakeRequestAsync(String url)
        {

            var query = Connection.Table<Models.SQL.HttpCache>().Where(v => v.URL.Equals(url));

            foreach (var output in query)
            {
                return output.Output;
            }


            String responseText = await Task.Run(() =>
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:72.0) Gecko/20100101 Firefox/72.0";
                    WebResponse response = request.GetResponse();
                    Stream responseStream = response.GetResponseStream();
                    return new StreamReader(responseStream).ReadToEnd();
                }
                catch (Exception)
                {
                    return null;
                }
            });
            try
            {
                if (Configuration.cacheing)
                {
                    Connection.Insert(new Models.SQL.HttpCache() { URL = url, Output = responseText });
                }
                else
                {
                    Log.Verbose("Caching is disabled");
                }
            }
            catch (SQLiteException e)
            {
                Log.Warning("Http caching failed because: {0}", e.Message);
            }

            return responseText;
        }

        public async Task<string> UploadToTrashbin(string data)
        {
            try
            {
                HttpClient client = new HttpClient();

                HttpContent c = new StringContent(data, Encoding.UTF8);

                var response = await client.PostAsync(Configuration.trashbinUrl + "/add", c);

                var responseString = await response.Content.ReadAsStringAsync();

                if (int.TryParse(responseString, out _))
                {
                    return Configuration.trashbinUrl + "/" + responseString;
                }
                else
                {
                    return responseString + " Статус код: " + response.StatusCode;
                }
            }
            catch (Exception)
            {
                return "Сервер недоступен for some reason: " + Configuration.trashbinUrl;
            }
        }
           

        public async Task<String> UploadToPastebin(string data)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };


            Models.PastebinUpload.RootObject paste = new Models.PastebinUpload.RootObject();

            paste.sections = new List<Models.PastebinUpload.Section>();

            var section = new Models.PastebinUpload.Section();

            section.name = "fs24 paste";
            section.contents = data;
            section.syntax = "autodetect";

            paste.description = "A paste pasted by fs24_bot";

            paste.sections.Add(section);

            HttpContent c = new StringContent(JsonConvert.SerializeObject(paste), Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("X-Auth-Token", "al8Pc66kMncvlsUcrveEEMlobZzH8R8sLrF0qpXFq");

            var response = await client.PostAsync("https://api.paste.ee/v1/pastes", c);
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonOutput = JsonConvert.DeserializeObject<Models.PastebinUpload.Output>(responseString, settings);

            Console.WriteLine(responseString);

            if (jsonOutput != null)
            {
                Console.WriteLine(jsonOutput.link);
                return jsonOutput.link;
            }
            else
            {
                throw new Exception("Invalid Data supplyed: " + responseString);
            }
        }

    }
}