using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace fs24bot3
{
    class HttpTools
    {
        readonly CookieContainer cookies = new CookieContainer();

        public string RecursiveHtmlDecode(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;
            var tmp = HttpUtility.HtmlDecode(str);
            while (tmp != str)
            {
                str = tmp;
                tmp = HttpUtility.HtmlDecode(str);
            }
            return str; //completely decoded string
        }

        public IPEndPoint ParseHostname(string host)
        {
            var hostname = host.Split(":");
            var port = 25565;
            if (hostname.Length > 1) { _ = int.TryParse(hostname[1], out port); }
            if (IPAddress.TryParse(hostname[0], out IPAddress ip))
            {
                return new IPEndPoint(ip, port);
            }
            else
            {
                return new IPEndPoint(Dns.GetHostEntry(hostname[0]).AddressList[0], port);
            }
        }

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
                catch (Exception e)
                {
                    Log.Warning("Request to address {0} failed: {1}", url, e.Message);
                    return null;
                }
            });

            return responseText;
        }

        public async Task<WebResponse> GetResponseAsync(String url)
        {
            WebResponse response = await Task.Run(() =>
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.CookieContainer = cookies;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:76.0) Gecko/20100101 Firefox/76.0";
                    return request.GetResponse();
                }
                catch (Exception)
                {
                    Log.Warning("Request failed to address: {0}", url);
                    return null;
                }
            });

            return response;
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

        public async Task<string> GetTextPlainResponse(string rawurl)
        {
            var response = await GetResponseAsync(rawurl);
            if (response != null)
            {
                if (response.ContentType == "text/plain")
                {
                    Stream responseStream = response.GetResponseStream();
                    return new StreamReader(responseStream).ReadToEnd();
                }
            }
            return null;
        }
    }
}