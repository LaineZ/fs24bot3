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

        public static byte[] GetSection(ref byte[] array, int start, int end)
        {
            var result = new byte[end - start];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = array[i + start];
            }
            return result;
        }

        public static int GetCharStart(ref byte[] arr, int index)
        {
            if (index > arr.Length)
            {
                index = arr.Length - 1;
            }

            return (arr[index] & 0xC0) == 0x80 ? GetCharStart(ref arr, index - 1) : index;
        }

        public static IEnumerable<byte[]> GetByteSections(byte[] utf8Array, int byteSize)
        {
            var sectionStart = 0;
            var sectionEnd = 0;
            var sectionCount = (utf8Array.Length / byteSize) + 1;
            var sectionSize = (int)Math.Ceiling((double)utf8Array.Length / sectionCount);

            for (var i = 0; i < sectionCount; i++)
            {
                if (i == (sectionCount - 1))
                {
                    var lengthRem = utf8Array.Length - i * sectionSize;
                    sectionEnd = GetCharStart(ref utf8Array, i * sectionSize);
                    yield return GetSection(ref utf8Array, sectionStart, sectionEnd);
                    sectionStart = sectionEnd;
                    sectionEnd = utf8Array.Length;
                    yield return GetSection(ref utf8Array, sectionStart, sectionEnd);
                }
                else
                {
                    sectionEnd = GetCharStart(ref utf8Array, i * sectionSize);
                    yield return GetSection(ref utf8Array, sectionStart, sectionEnd);
                    sectionStart = sectionEnd;
                }
            }
        }
    }
}
