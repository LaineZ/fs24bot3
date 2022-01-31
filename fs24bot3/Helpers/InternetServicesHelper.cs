using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Helpers
{
    class InternetServicesHelper
    {
        private HttpTools http = new HttpTools();

        public async Task<List<string>> InPearls(string category = "", int page = 0)
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
    }
}
