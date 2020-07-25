using fs24bot3.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Text;

namespace fs24bot3.Core
{
    public static class MailSearchDecoder
    {
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

        public static string BoldToIrc(string input)
        {
            StringBuilder textResult = new StringBuilder(input);
            textResult.Replace("<b>", IrcColors.Bold);
            textResult.Replace("</b>", IrcColors.Reset);

            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(textResult.ToString());

            return doc.DocumentNode.InnerText;
        }
    }
}
