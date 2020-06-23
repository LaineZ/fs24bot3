using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace fs24bot3
{
    class HttpTools
    {
        HttpClient client = new HttpClient();
        CookieContainer cookies = new CookieContainer();

        public async Task<String> MakeRequestAsync(String url)
        {
            String responseText = await Task.Run(() =>
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.CookieContainer = cookies;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:76.0) Gecko/20100101 Firefox/76.0";
                    WebResponse response = request.GetResponse();

                    Stream responseStream = response.GetResponseStream();
                    return new StreamReader(responseStream).ReadToEnd();
                }
                catch (Exception)
                {
                    return null;
                }
            });

            return responseText;
        }

        public async Task<string> UploadToTrashbin(string data, string route = "add")
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpContent c = new StringContent(data, Encoding.UTF8);

                var response = await client.PostAsync(Configuration.trashbinUrl + "/" + route, c);

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

        public VkApi LogInVKAPI()
        {
            Log.Information("Logging with vkapi...");
            var vk = new VkApi();

            try
            {
                vk.Authorize(new ApiAuthParams
                {
                    ApplicationId = ulong.Parse(Configuration.vkApiId),
                    Login = Configuration.vkLogin,
                    Password = Configuration.vkPassword,
                    Settings = Settings.All,
                });
                return vk;
            }
            catch (Exception)
            {
                Log.Error("Failed to load vk api key that means you cannot use vk api functions, sorry...");
                return vk;
            }
        }

    }
}