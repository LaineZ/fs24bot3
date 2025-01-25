using fs24bot3.Models;
using fs24bot3.Parsers;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class TranslateCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    public CommandService Service { get; set; }

    [Command("tr", "translate")]
    [Description("Переводчик")]
    [Remarks("Параметр lang нужно вводить в формате 'sourcelang-translatelang' или 'traslatelang' в данном случае переводчик попытается догадаться с какого языка пытаются перевести (работает криво, претензии не к разработчику бота)\nВсе языки вводятся по стандарту ISO-639-1 посмотреть можно здесь: https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%8F%D0%B7%D1%8B%D0%BA%D0%BE%D0%B2")]
    [Cooldown(10, 2, CooldownMeasure.Minutes, Bot.CooldownBucketType.Global)]
    public async Task Translate(Language lang, [Remainder] string text) 
    {
        try
        {
            var translatedOutput = await Context.ServicesHelper.Translate(text, lang.From, lang.To);
            if (translatedOutput.Tl != null && translatedOutput.Texts.Any())
            {
                await Context.SendMessage(Context.Channel, $"{translatedOutput.Texts[0]} ({lang})");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{translatedOutput.Texts[0]} ({lang})");
            }
        }
        catch (ArgumentException)
        {
            await Context.SendSadMessage(Context.Channel, $"Вы указали неверный язык при переводе. Используйте .helpcmd tr чтобы узнать как пользоваться командой!");
        }
    }

    [Command("trppc")]
    [Description("Ппц жесть переводчик")]
    [Cooldown(1, 2, CooldownMeasure.Minutes, Bot.CooldownBucketType.Global)]
    public async Task TranslatePpc([Remainder] string text)
    {
        var output = await Context.ServicesHelper.TranslatePpc(text);
        await Context.SendMessage(Context.Channel, output);
    }

    [Command("trl")]
    [Description("Переводчик (ппц). Параметр lang вводится так же как и в tr")]
    [Cooldown(5, 2, CooldownMeasure.Minutes, Bot.CooldownBucketType.Global)]
    public async Task TranslatePpc2(Language lang, [Remainder] string text)
    {
        try
        {
            var splitted = text.Split(" ");

            if (splitted.Length > 35)
            {
                await Context.SendMessage(Context.Channel, $"[royal]У вас слишком жесткий текст ({splitted.Length} слов) его обработка может занять некоторое время...");
            }

            // Forech statement cannot be modified WHY???????
            for (int i = 0; i < splitted.Length; i++)
            {
                var tr = await Context.ServicesHelper.Translate(splitted[i], lang.From, lang.To);
                splitted[i] = tr.Texts[0];
            }

            await Context.SendMessage(Context.Channel, string.Join(' ', splitted).ToLower());
        }
        catch (FormatException)
        {
            await Context.SendMessage(Context.Channel, RandomMsgs.BanMessages.Random());
        }
    }
}
