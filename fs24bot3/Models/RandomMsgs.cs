using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public static class RandomMsgs
    {
        public static readonly List<string> BanMessages = new List<string>
        {
            "Слишком жесткие дивизии",
            "Укрепления выше 5 лвл",
            "В альянсе состоит больше 3 человек",
            "Провода не по сетке",
            "Не понравился админу",
            "1.140",
            "Вас забанили."
        };


        public static string GetRandomMessage(List<string> list)
        {
            var random = new Random();
            int index = random.Next(list.Count);
            return list[index];
        }
    }
}
