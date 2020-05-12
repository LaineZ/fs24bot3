using System;
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


        public static string GenerateName(int len)
        {
            Random r = new Random();
            string[] consonants = { "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string[] vowels = { "a", "e", "i", "o", "u", "ae", "y" };
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
    }
}
