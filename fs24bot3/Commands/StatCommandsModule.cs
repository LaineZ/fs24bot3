using fs24bot3.Models;
using Qmmands;
using System.Collections.Generic;
using System.Linq;

namespace fs24bot3.Commands
{
    public sealed class StatCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("hourstat")]
        [Description("Статистика за час")]
        public void Hourstat()
        {
            List<string> stopwords = new List<string>() { "что", "как", "все", "она", "так", "его", "только", "мне", 
                "было", "вот", "меня", "еще", "нет", "ему", "теперь", "когда", "даже", "вдруг", 
                "если", "уже", "или", "быть", "был", "него", "вас", "нибудь", "опять", "вам", 
                "ведь", "там", "потом", "себя", "ничего", "может", "они", "тут", "где", 
                "есть", "надо", "ней", "для", "мы", "тебя", "чем", "была", "сам", "чтоб", 
                "без", "будто", "чего", "раз", "тоже", "себе", "под", "будет", "тогда", "кто", 
                "этот", "это", "того", "потому", "этого", "какой", "совсем", "ним", "здесь", 
                "этом", "один", "почти", "мой", "тем", "чтобы", "нее", "сейчас", "были", 
                "куда", "зачем", "всех", "никогда", "можно", "при", 
                "наконец", "два", "другой", "хоть", "после", 
                "над", "больше", "тот", "через", "эти", "нас", "про", 
                "всего", "них", "какая", "много", "разве", "три", "эту", "моя", 
                "впрочем", "хорошо", "свою", "этой", "перед", "иногда", "лучше", "чуть", 
                "том", "нельзя", "такой", "более", "всегда", "конечно", "всю" };

            int messageCount = Context.Messages.Count;
            string mostActive = Context.Messages.GroupBy(msg => msg.From).OrderByDescending(grp => grp.Count())
                        .Select(grp => grp.Key).First();
            string concatedMessage = string.Join("\n", Context.Messages.Select(x => x.Message));
            string[] words = concatedMessage.Split(" ");
            string mostUsedword = words.Where(word => word.Length > 2 && !stopwords.Any(s => word.Equals(s))).GroupBy(word => word).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).FirstOrDefault() ?? "не знаю";
            Context.SendMessage(Context.Channel, $"Статистика за текущий час: Сообщений: {messageCount}, Слов: {words.Length}, Символов: {concatedMessage.Length}, Самый активный: {mostActive}, Возможная тема: {mostUsedword}");
        }

        [Command("me")]
        [Description("Макроэкономические показатели")]
        public void Economy()
        {
            Context.SendMessage(Context.Channel, $"Число зарплат: {Shop.PaydaysCount} Денежная масса: {Shop.GetItemAvg(Context.Connection)} Последнее время выполнения Shop.Update(): {Shop.TickSpeed.TotalMilliseconds} ms Период выполнения Shop.Update() {Shop.Tickrate} ms Покупок/Продаж {Shop.Buys}/{Shop.Sells}");
        }

        [Command("stat", "stats")]
        [Description("Статы пользователя или себя")]
        public void Userstat(string nick = null)
        {
            string userNick = nick ?? Context.Message.From;
            UserOperations usr = new UserOperations(userNick, Context.Connection);

            var data = usr.GetUserInfo();
            if (data != null)
            {
                Context.SendMessage(Context.Channel, $"Статистика: {data.Nick} Уровень: {data.Level} XP: {data.Xp} / {data.Need}");
                try
                {
                    var userTags = usr.GetUserTags();
                    if (userTags.Count > 0)
                    {
                        Context.SendMessage(Context.Channel, "Теги: " + string.Join(' ', userTags.Select(x => $"{x.Color},00⚫{x.TagName}{IrcColors.Reset}")));
                    }
                }
                catch (Core.Exceptions.UserNotFoundException)
                {
                    Context.SendMessage(Context.Channel, "Теги: Нет");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "Пользователя не существует (это как вообще? даже тебя что ли не существует?)");
            }
        }        
    }
}
