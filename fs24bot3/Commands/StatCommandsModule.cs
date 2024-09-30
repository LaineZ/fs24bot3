using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.Properties;
using fs24bot3.QmmandsProcessors;
using HandlebarsDotNet.Extensions;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class StatCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    public CommandService Service { get; set; }
    private readonly Regex WordRegex = new Regex(@"\S*");
    
    private string FmtTag(SQL.Tags tag)
    {
        var t = Context.BotCtx.Connection.Table<SQL.Tag>()
                .FirstOrDefault(x => x.Name == tag.Tag);
        return $"00,{t.Color}⚫{t.Name}[r]";
    }

    [Command("topic")]
    [Description("Текущая тема разговора")]
    [Cooldown(1, 2, CooldownMeasure.Minutes, Bot.CooldownBucketType.Global)]
    public async Task Topic(int window = 100)
    {
        var sms = Context.BotCtx.Connection.Table<SQL.Messages>().ToList();
        var messages = sms.TakeLast(Math.Clamp(window, 5, 100)).Where(x => x.Nick != Context.BotCtx.Client.Name && x.Nick != "fs24_bot");
        var concat = new StringBuilder();

        foreach (var item in messages.Select(x => $"{x.Nick}: {x.Message}"))
        {
            if (concat.Length > 4097)
            {
                break;
            }
            concat.Append(item);
        }

        if (concat.Length < 10)
        {
            await Context.SendSadMessage();
            return;
        }

        var response = await Context.HttpTools.PostJson("https://tools.originality.ai/tool-title-generator/title-generator-backend/generate.php", new Topic(concat.ToString()));
        string topic = JsonConvert.DeserializeObject<string>(response);
        await Context.SendMessage($"Сейчас тема разговора: [b]{topic}");
    }

    [Command("daystat")]
    [Description("Статистика за день")]
    public async Task Daystat(string dateString = "")
    {
        var res = DateTime.TryParse(dateString, out DateTime date);

        if (!res) { date = DateTime.Now; }

        var sms = await Context.ServicesHelper.GetMessagesSprout(date);

        if (!sms.Any())
        {
            await Context.SendSadMessage();
            return;
        }

        List<string> stopwords = Resources.stopwords.Split("\n").ToList();
        int messageCount = sms.Count;
        string mostActives = string.Join(" ", sms.GroupBy(msg => msg.Nick)
            .OrderByDescending(grp => grp.Count())
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


        string mostUsedwords = string.Join(", ", words
            .GroupBy(word => word.Item2)
            .OrderByDescending(grp => grp.Count())
            .Take(4)
            .Select(value => value.Select(x => x.Item1).First()));
        await Context.SendMessage(Context.Channel,
            $"{str}: {messageCount} строк, {caputures.Count} слов, {concatedMessage.Length} символов, " +
            $"{avgWords} слов в строке. Самые активные: {mostActives}. Возможные темы: {mostUsedwords}");
    }

    [Command("me")]
    [Description("Макроэкономические показатели")]
    public async Task Economy()
    {
        await Context.SendMessage(Context.Channel, 
            $"Число зарплат: {Context.BotCtx.Shop.PaydaysCount} " +
            $"Денежная масса: {new MultiUser(Context.BotCtx.Connection).GetItemAvg("money")} " +
            $"Покупок/Продаж {Context.BotCtx.Shop.Buys}/{Context.BotCtx.Shop.Sells}");
    }

    [Command("stat", "stats")]
    [Description("Статы пользователя или себя")]
    public async Task Userstat(string nick = null)
    {
        string userNick = nick ?? Context.User.Username;
        User usr = new User(userNick, in Context.BotCtx.Connection);
        var data = usr.GetUserInfo();
        var tags = usr.GetTags();

        double percent = (double)data.Xp / data.Need;

        await Context.SendMessage(Context.Channel, 
            $"Статистика: {data.Nick} Уровень: {data.Level} XP: {MessageHelper.Bar(15, (int)(percent * 100.0))} {data.Xp} / {data.Need}");

        if (tags.Any())
        {
            await Context.SendMessage(Context.Channel, 
                $"Теги: {string.Join(' ', tags.Select(x => FmtTag(x)))}");
        }
        else
        {
            await Context.SendMessage(Context.Channel, "Теги: нет");
        }
    }
}
