using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fs24bot3.Core
{
    class MessageUtils
    {
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
                splitted.Add(converted);
            }
            return splitted;
        }
    }
}
