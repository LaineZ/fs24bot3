using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.Properties;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class StatCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }
        private readonly Regex WordRegex = new Regex(@"\S*");

        [Command("daystat")]
        [Description("Статистика за день")]
        public async Task Daystat(string dateString = "")
        {
            var res = DateTime.TryParse(dateString, out DateTime date);

            if (!res) { date = DateTime.Now; }

            var sms = await InternetServicesHelper.GetMessages(date);

            if (!sms.Any())
            {
                Context.SendSadMessage(Context.Channel, RandomMsgs.NotFoundMessages.Random());
                return;
            }

            List<string> stopwords = Resources.stopwords.Split("\n").ToList();
            int messageCount = sms.Count;
            string mostActives = string.Join(" ", sms.GroupBy(msg => msg.Nick).OrderByDescending(grp => grp.Count())
                        .Select(grp => grp.Key).Take(3));
            string concatedMessage = string.Join("\n", sms.Select(x => x.Message.TrimEnd())).ToLower();
            List<(string, string)> words = new();

            var users = Context.BotCtx.Connection.Table<SQL.UserStats>().ToList();
            var caputures = WordRegex.Matches(concatedMessage);

            foreach (Match match in caputures)
            {
                string word = MessageHelper.StripIRC(match.Value);
                if (word.Length > 4 && !stopwords.Any(s => word == s.TrimEnd()) && !stopwords.Any(s => word == s.TrimEnd())
                    && !users.Any(s => word.ToLower().Contains(s.Nick.ToLower())) && !word.EndsWith(":"))
                {
                    words.Add((word, PorterHelper.TransformingWord(word)));
                }
            }

            var str = res ? $"{date.Date.Year}-{date.Date.Month}-{date.Date.Day}" : "Cегодня";
            float avgWords = caputures.Count / messageCount;


            string mostUsedwords = string.Join(", ", words.GroupBy(word => word.Item2).OrderByDescending(grp => grp.Count()).Take(4).Select(value => value.Select(x => x.Item1).First()));
            await Context.SendMessage(Context.Channel,
                $"{str}: {messageCount} строк, {caputures.Count} слов, {concatedMessage.Length} символов, {avgWords} слов в строке. Самые активные: {mostActives}. Возможные темы: {mostUsedwords}");
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
                        await Context.SendMessage(Context.Channel, "Теги: " + string.Join(' ', userTags.Select(x => $"00,{x.Color}⚫{x.TagName}{IrcClrs.Reset}")));
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
