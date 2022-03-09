using fs24bot3.Models;
using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace fs24bot3.Helpers
{
    class InternetServicesHelper
    {
        private static HttpTools http = new HttpTools();
        private static Regex LogsRegex = new Regex(@"^\[(\d{2}:\d{2}:\d{2})\] <([^>]+)> (.+)", RegexOptions.Compiled);

        public static async Task<List<string>> InPearls(string category = "", int page = 0)
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync("https://www.inpearls.ru/" + category + "?page=" + page);
            HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[@class=\"text\"]");
            var nodes = doc.DocumentNode.SelectNodes("//br");

            List<string> pearls = new List<string>();
            Log.Verbose("Page: {0}", page);

            if (divContainer != null && nodes != null)
            {
                foreach (HtmlNode node in nodes)
                    node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

                foreach (var node in divContainer)
                {
                    if (node.InnerText.Split("\n").Length <= 2)
                    {
                        pearls.Add(http.RecursiveHtmlDecode(node.InnerText));
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"Категории `{category}`");
            }

            return pearls;
        }

        public static async Task<List<FomalhautMessage>> GetMessages(DateTime dateTime)
        {
            var output = await http.MakeRequestAsync("https://logs.fomalhaut.me/download/" + dateTime.ToString("yyyy-MM-dd") + ".log");
            var list = new List<FomalhautMessage>();

            foreach (var item in output.Split("\n"))
            {
                var captures = LogsRegex.Match(item);
                var time = captures.Groups[1].Value;
                var nick = captures.Groups[2].Value;
                var message = captures.Groups[3].Value;
                if (!string.IsNullOrWhiteSpace(nick) && !string.IsNullOrWhiteSpace(message))
                {
                    list.Add(new FomalhautMessage { Date = dateTime.Add(TimeSpan.Parse(time)), Message = message, Nick = nick, Kind = Kind.Message });
                }
                else
                {
                    // cannot parse message, looks like a server message or action
                    Log.Warning("Message {0} cannot be parsed!", item);
                }
            }

            return list;
        }

        public static MailSearch.RootObject PerformDecode(string code)
        {
            string startString = "go.dataJson = {";
            string stopString = "};";

            string searchDataTemp = code.Substring(code.IndexOf(startString) + startString.Length - 1);
            string searchData = searchDataTemp.Substring(0, searchDataTemp.IndexOf(stopString) + 1);

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                return JsonConvert.DeserializeObject<MailSearch.RootObject>(searchData, settings);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
