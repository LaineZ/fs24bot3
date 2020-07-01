using System;
using System.Collections.Generic;

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
            "Вас забанили.",
            "привет",
            "помеха",
            "нонрп развод",
            "Вот и договорились, предупреждали, теперь досвидания :) до встречи! :bwcount: #5962 :)",
            "Длина ника превышает норму (макс. 18 символов). Поменяйте и перезайдите",
            "OOC махинаци.",
            "Server closed the connection."
        };


        public static readonly List<string> MissMessages = new List<string>
        {
            "Вы не попали",
            "Вы не удержали гаечный ключ в руках...",
            "Вы промахнулись и не попали...",
            "Да все...",
            "Гаечный ключ рассыпался в руках..."
        };


        public static string GetRandomMessage(List<string> list)
        {
            var random = new Random();
            int index = random.Next(list.Count);
            return list[index];
        }
    }
}
