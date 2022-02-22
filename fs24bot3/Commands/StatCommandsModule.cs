using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.Properties;
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
            List<string> stopwords = Resources.stopwords.Split("\n").ToList();
            int messageCount = Context.BotCtx.MessageBus.Count;
            string mostActives = string.Join(" ", Context.BotCtx.MessageBus.GroupBy(msg => msg.Prefix.From).OrderByDescending(grp => grp.Count())
                        .Select(grp => grp.Key).Take(3));
            string concatedMessage = string.Join("\n", Context.BotCtx.MessageBus.Select(x => x.Trailing.TrimEnd()));
            string[] words = concatedMessage.Split(" ");
            string mostUsedwords = string.Join(", ", words.Where(word => word.Length > 2 && !stopwords.Any(s => word.Equals(s)))
                .GroupBy(word => word).OrderByDescending(grp => grp.Count()).Take(5).Select(grp => grp.Key.Replace("\n", ""))); // idk why need replacing
            await Context.SendMessage(Context.Channel, $"Статистика за текущий час: Сообщений: {messageCount}, Слов: {words.Length}, Символов: {concatedMessage.Length}, Самые активные: {mostActives}, Возможные темы: {mostUsedwords}");
        }

        [Command("me")]
        [Description("Макроэкономические показатели")]
        public async Task Economy()
        {
            await Context.SendMessage(Context.Channel, $"Число зарплат: {Context.BotCtx.Shop.PaydaysCount} Денежная масса: {new MultiUser(Context.BotCtx.Connection).GetItemAvg("money")} Покупок/Продаж {Context.BotCtx.Shop.Buys}/{Context.BotCtx.Shop.Sells}");
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
            User usr = new User(userNick, Context.BotCtx.Connection);

            var data = usr.GetUserInfo();
            if (data != null)
            {
                await Context.SendMessage(Context.Channel, $"Статистика: {data.Nick} Уровень: {data.Level} XP: {data.Xp} / {data.Need}");
                try
                {
                    var userTags = usr.GetUserTags();
                    if (userTags.Count > 0)
                    {
                        await Context.SendMessage(Context.Channel, "Теги: " + string.Join(' ', userTags.Select(x => $"{x.Color},00⚫{x.TagName}{IrcClrs.Reset}")));
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
