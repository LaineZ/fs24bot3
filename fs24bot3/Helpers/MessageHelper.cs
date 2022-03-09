using fs24bot3.Models;
using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace fs24bot3.Helpers
{
    class MessageHelper
    {
        private static string GenerateBaseName(int len, string[] consonants, string[] vowels)
        {
            Random r = new Random();
            string Name = "";
            Name += consonants[r.Next(consonants.Length)].ToUpper();
            Name += vowels[r.Next(vowels.Length)];
            int b = 2; //b tells how many times a new letter has been added. It's 2 right now because the first two letters are already in the name.
            while (b < len)
            {
                Name += consonants[r.Next(consonants.Length)];
                b++;
                Name += vowels[r.Next(vowels.Length)];
                b++;
            }

            return Name;
        }

        public static string GenerateName(int len)
        {
            return GenerateBaseName(len, new string[] { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" },
            new string[] { "a", "e", "i", "o", "u", "ae", "y" });
        }

        public static string StripIRC(string input)
        {
            return Regex.Replace(input, @"[\x02\x1F\x0F\x16]|\x03(\d\d?(,\d\d?)?)?", String.Empty);    
        }

        public static string GenerateNameRus(int len)
        {
            return GenerateBaseName(len, new string[] { "б", "в", "г", "д", "ж", "з", "й", "к", "л", "м", "н", "п", "р", "с", "т", "ф", "х", "ц", "ч", "ш", "щ" },
            new string[] { "а", "у", "о", "ы", "и", "э", "я", "ю", "ё", "е" });
        }

        public static string AllEnumOptionsToString(Enum en)
        {
            return string.Join(", ", Enum.GetValues(en.GetType()));
        }

        public static string AntiHightlight(string input)
        {
            StringBuilder inputBuilder = new StringBuilder(input);

            inputBuilder.Replace('а', 'a'); 
            inputBuilder.Replace('А', 'A'); 
            inputBuilder.Replace('Н', 'H'); 
            inputBuilder.Replace('В', 'B'); 
            inputBuilder.Replace('х', 'x'); 
            inputBuilder.Replace('Х', 'X'); 
            inputBuilder.Replace('E', 'E'); 
            inputBuilder.Replace('р', 'p'); 
            inputBuilder.Replace('М', 'M');
            inputBuilder.Replace('К', 'K'); 
            inputBuilder.Replace('Т', 'T'); 
            inputBuilder.Replace('В', 'B'); 
            inputBuilder.Replace('с', 'c'); 
            inputBuilder.Replace('С', 'C');
            inputBuilder.Replace('О', 'O');
            inputBuilder.Replace('о', 'o');

            return inputBuilder.ToString();
        }

        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            //Log.Verbose("{0} {1} score: {2}", s, t, d[n, m]);
            return d[n, m];
        }
        public static string BoldToIrc(string input)
        {
            // very sketchy html-like irc message formatter =)
            StringBuilder textResult = new StringBuilder(input);

            textResult.Replace("<b>", IrcClrs.Bold);
            textResult.Replace("</b>", IrcClrs.Reset);

            HtmlDocument doc = new HtmlDocument();

            doc.LoadHtml(textResult.ToString());

            return doc.DocumentNode.InnerText;
        }
    }
}
