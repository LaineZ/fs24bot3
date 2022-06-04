using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public class HtmlTemplate
    {
        public string TemplateKey { get; }
        private string TemplateFragment { get; }
        private string Content { get; }
        public HtmlTemplate(string templateKey, string content)
        {
            TemplateKey = templateKey;
            var splitted = content.Split("\n").ToList();
            int start = splitted.FindIndex(0, x => x == "[" + templateKey + "]");
            int end = splitted.FindIndex(0, x => x == "/[" + templateKey + "]");

            for (int i = start; i < end; i++)
            {
                TemplateFragment += splitted[i];
                splitted.RemoveAt(i);
            }

            Content = string.Join("\n", splitted);

            Log.Verbose("{0}", TemplateFragment);
        }

        public string BuildWebpageFromTemplate<T>(List<T> formattedStrings) where
            T : IFormattable
        {
            string stro4ka = string.Empty;
            foreach (var item in formattedStrings)
            {
                item.ToString();
            }

            return stro4ka;
        }
    }
}
