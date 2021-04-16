using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class StatCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("hourstat")]
        [Description("Статистика за час")]
        public async Task Hourstat()
        {
            List<string> stopwords = new List<string>() { "что", "как", "все", "она", "так", "его", "только", "мне", 
                "было", "вот", "меня", "еще", "нет", "ему", "теперь", "когда", "даже", "вдруг", 
                "если", "уже", "или", "быть", "был", "него", "вас", "нибудь", "опять", "вам", 
                "ведь", "там", "потом", "себя", "ничего", "может", "они", "тут", "где", 
                "есть", "надо", "ней", "для", "мы", "тебя", "чем", "была", "сам", "чтоб", 
                "без", "будто", "чего", "раз", "тоже", "себе", "под", "будет", "тогда", "кто", 
                "этот", "это", "того", "потому", "этого", "какой", "совсем", "ним", "здесь", 
                "этом", "один", "почти", "мой", "тем", "чтобы", "нее", "сейчас", "были", 
                "куда", "зачем", "всех", "никогда", "можно", "при", "блин", "капец",
                "наконец", "два", "другой", "хоть", "после", "the", "then",
                "над", "больше", "тот", "через", "эти", "нас", "про", 
                "всего", "них", "какая", "много", "разве", "три", "эту", "моя", 
                "впрочем", "хорошо", "свою", "этой", "перед", "иногда", "лучше", "чуть", 
                "том", "нельзя", "такой", "более", "всегда", "конечно", "всю" };

            int messageCount = Context.Messages.Count;
            string mostActives = string.Join(" ", Context.Messages.GroupBy(msg => msg.Prefix.From).OrderByDescending(grp => grp.Count())
                        .Select(grp => grp.Key).Take(3));
            string concatedMessage = string.Join("\n", Context.Messages.Select(x => x.Trailing.TrimEnd()));
            string[] words = concatedMessage.Split(" ");
            string mostUsedwords = string.Join(", ", words.Where(word => word.Length > 2 && !stopwords.Any(s => word.Equals(s))).GroupBy(word => word).OrderByDescending(grp => grp.Count()).Take(5).Select(grp => grp.Key));
            await Context.SendMessage(Context.Channel, $"Статистика за текущий час: Сообщений: {messageCount}, Слов: {words.Length}, Символов: {concatedMessage.Length}, Самый активные: {mostActives}, Возможная темы: {mostUsedwords}");
        }

        [Command("me")]
        [Description("Макроэкономические показатели")]
        public async Task Economy()
        {
            await Context.SendMessage(Context.Channel, $"Число зарплат: {Shop.PaydaysCount} Денежная масса: {new MultiUser(Context.Connection).GetItemAvg()} Последнее время выполнения обновления данных о пользователях: {Shop.TickSpeed.TotalMilliseconds} ms Период выполнения Shop.Update() {Shop.Tickrate} ms Покупок/Продаж {Shop.Buys}/{Shop.Sells}");
        }

        [Command("mem")]
        [Description("Использование памяти")]
        public async Task MemoryUsage()
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            await Context.SendMessage(Context.Channel, string.Join(" | ", proc.GetType().GetProperties().Where(x => x.Name.EndsWith("64")).Select(prop => $"{prop.Name.Replace("64", "")} = {(long)prop.GetValue(proc, null) / 1024 / 1024} MiB")));
        }

        [Command("stat", "stats")]
        [Description("Статы пользователя или себя")]
        public async Task Userstat(string nick = null)
        {
            string userNick = nick ?? Context.Sender;
            User usr = new User(userNick, Context.Connection);

            var data = usr.GetUserInfo();
            if (data != null)
            {
                await Context.SendMessage(Context.Channel, $"Статистика: {data.Nick} Уровень: {data.Level} XP: {data.Xp} / {data.Need}");
                try
                {
                    var userTags = usr.GetUserTags();
                    if (userTags.Count > 0)
                    {
                        await Context.SendMessage(Context.Channel, "Теги: " + string.Join(' ', userTags.Select(x => $"{x.Color},00⚫{x.TagName}{IrcColors.Reset}")));
                    }
                }
                catch (Exceptions.UserNotFoundException)
                {
                    await Context.SendMessage(Context.Channel, "Теги: Нет");
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Пользователя не существует (это как вообще? даже тебя что ли не существует?)");
            }
        }        
    }
}
