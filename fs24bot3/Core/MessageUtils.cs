﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fs24bot3.Core
{
    class MessageUtils
    {
        private static String LimitByteLength(String input, Int32 maxLength)
        {
            return new String(input
                .TakeWhile((c, i) =>
                    Encoding.UTF8.GetByteCount(input.Substring(0, i + 1)) <= maxLength)
                .ToArray());
        }

        public static String FindWord(List<string> arr)
        {

            // Create Dictionary to store word  
            // and it's frequency  
            Dictionary<String, int> hs =
                new Dictionary<String, int>();

            // Iterate through array of words  
            for (int i = 0; i < arr.Count - 1; i++)
            {
                // If word already exist in Dictionary  
                // then increase it's count by 1  
                if (hs.ContainsKey(arr[i]))
                {
                    hs[arr[i]] = hs[arr[i]] + 1;
                }

                // Otherwise add word to Dictionary  
                else
                {
                    hs.Add(arr[i], 1);
                }
            }

            // Create set to iterate over Dictionary  
            String key = "";
            int value = 0;

            foreach (KeyValuePair<String, int> me in hs)
            {
                // Check for word having highest frequency  
                if (me.Value > value)
                {
                    value = me.Value;
                    key = me.Key;
                }
            }

            // Return word having highest frequency  
            return key;
        }

        public static List<string> SplitMessage(string value, int chunkLength)
        {
            List<string> splitted = new List<string>();

            byte[] stringByte = Encoding.UTF8.GetBytes(value);

            byte[][] chunks = stringByte.Select((value, index) =>
            new { PairNum = Math.Floor(index / (decimal)chunkLength), value })
                .GroupBy(pair => pair.PairNum)
                .Select(grp => grp.Select(g => g.value).ToArray())
                .ToArray();

            foreach (var chunk in chunks)
            {
                string converted = Encoding.UTF8.GetString(chunk, 0, chunk.Length);
                splitted.Add(LimitByteLength(converted, chunkLength));
            }
            return splitted;
        }


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


        public static string GenerateNameRus(int len)
        {
            return GenerateBaseName(len, new string[] { "б", "в", "г", "д", "ж", "з", "й", "к", "л", "м", "н", "п", "р", "с", "т", "ф", "х", "ц", "ч", "ш", "щ" },
            new string[] { "а", "у", "о", "ы", "и", "э", "я", "ю", "ё", "е" });
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

            return inputBuilder.ToString();
        }
    }
}
