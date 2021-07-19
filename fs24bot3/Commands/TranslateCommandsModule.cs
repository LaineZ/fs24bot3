using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class TranslateCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        private (string, string) ParseLang(string input)
        {
            string[] langs = input.Split("-");

            if (langs.Length > 1)
            {
                return (langs[0], langs[1]);
            }
            else
            {
                return ("", input);
            }
        }

        private async void AITranslate(string lang, string chars, uint max)
        {
            try
            {
                string rndWord = "";
                Random rnd = new Random();
                for (uint i = 0; i < rnd.Next(10, (int)Math.Clamp(max, 10, 400)); i++)
                {
                    rndWord += chars[rnd.Next(0, chars.Length - 1)];
                }

                var translatedOutput = await Core.Transalator.Translate(rndWord, lang, "ru");
                await Context.SendMessage(Context.Channel, translatedOutput);
            }
            catch (Exception e)
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст..... =( {e.Message}");
            }
        }

        [Command("aigen", "gensent")]
        [Checks.UnPpcable]
        public async Task GenAI(uint max = 200)
        {
            var user = new User(Context.Sender, Context.BotCtx.Connection, Context);
            if (await user.RemItemFromInv(Context.BotCtx.Shop, "beer", 1))
            {
                AITranslate("ar", " ذضصثقفغعهخجدشسيبلاتنمكطئءؤرلاىةوزظ", max);
            }
        }

        [Command("tr", "translate")]
        [Checks.UnPpcable]
        [Description("Переводчик")]
        [Remarks("Параметр lang нужно вводить в формате 'sourcelang-translatelang' или 'traslatelang' в данном случае переводчик попытается догадаться с какого языка пытаются перевести (работает криво, претензии не к разработчику бота)\nВсе языки вводятся по стандарту ISO-639-1 посмотреть можно здесь: https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%8F%D0%B7%D1%8B%D0%BA%D0%BE%D0%B2")]
        public async Task Translate(string lang, [Remainder] string text)
        {
            try
            {
                var (from, to) = ParseLang(lang);
                var translatedOutput = await Transalator.TranslateBing(text, from, to);
                await Context.SendMessage(Context.Channel, $"{translatedOutput.translations[0].text} ({translatedOutput.translations[0].to})");
            }
            catch (ArgumentException)
            {
                Context.SendSadMessage(Context.Channel, "Вы указали неверный язык при переводе. Используйте @helpcmd tr чтобы узнать как пользоваться командой!");
            }
        }

        [Command("trppc")]
        [Checks.UnPpcable]
        [Description("Переводчик (ппц)")]
        public async Task TranslatePpc([Remainder] string text)
        {
            var usr = new User(Context.Sender, Context.BotCtx.Connection, Context);
            if (await usr.RemItemFromInv(Context.BotCtx.Shop, "beer", 1))
            {
                try
                {
                    await Context.SendMessage(Context.Channel, Transalator.TranslatePpc(text).Result);
                }
                catch (FormatException)
                {
                    await Context.SendMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
                }
            }
        }

        [Command("trppcgen")]
        [Checks.UnPpcable]
        public async Task TranslatePpcGen(int gensArg, [Remainder] string text)
        {
            int gens = Math.Clamp(gensArg, 2, 8);
            string lastText = text;
            List<string> translationsChain = new List<string>();
            var usr = new User(Context.Sender, Context.BotCtx.Connection, Context);

            if (await usr.RemItemFromInv(Context.BotCtx.Shop, "beer", 1))
            {
                for (int i = 0; i < gens; i++)
                {
                    try
                    {
                        lastText = await Transalator.TranslatePpc(lastText);
                        translationsChain.Add(lastText);
                    }
                    catch (FormatException)
                    {
                        await Context.SendMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
                    }
                }

                // calculating output
                string totalOut = string.Join("->", translationsChain);

                if (totalOut.Length < 250)
                {
                    await Context.SendMessage(Context.Channel, totalOut);
                }
                else
                {
                    await Context.SendMessage(Context.Channel, lastText);
                }
            }
        }

        [Command("trppclite", "trl")]
        [Checks.UnPpcable]
        [Description("Переводчик (ппц lite). Параметр lang вводится так же как и в @tr")]
        public async Task TranslatePpc2(string lang, [Remainder] string text)
        {

            (string from, string to) = ParseLang(lang);

            try
            {
                var splitted = text.Split(" ");

                if (splitted.Length > 35)
                {
                    await Context.SendMessage(Context.Channel, $"{IrcColors.Royal}У вас слишком жесткий текст ({splitted.Length} слов) его обработка может занять некоторое время...");
                }

                // Forech statement cannot be modified WHY???????
                for (int i = 0; i < splitted.Length; i++)
                {
                    var tr = await Transalator.TranslateBing(splitted[i], from, to);
                    splitted[i] = tr.translations[0].text;
                }

                await Context.SendMessage(Context.Channel, string.Join(' ', splitted).ToLower());
            }
            catch (FormatException)
            {
                await Context.SendMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
            }
        }

        [Command("dmlyrics", "dmlyr")]
        [Checks.UnPpcable]
        [Description("Текст песни (ппц)")]
        public async Task LyricsPpc([Remainder] string song)
        {
            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.BotCtx.Connection);
                    string translated = await lyrics.GetLyrics();
                    var usr = new User(Context.Sender, Context.BotCtx.Connection, Context);

                    if (await usr.RemItemFromInv(Context.BotCtx.Shop, "beer", 2))
                    {
                        string[] translations = { "ru", "ar", "pl", "fr", "ja", "es", "ro", "de", "ru" };

                        foreach (var tr in translations)
                        {
                            var translatorResponse = await Core.Transalator.Translate(translated, "auto-detect", tr);
                            translated = translatorResponse;
                        }

                        await Context.SendMessage(Context.Channel, translated);
                    }
                }
                catch (Exception e)
                {
                    Context.SendErrorMessage(Context.Channel, "Ошибка при получении слов: " + e.Message);
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Instumental");
            }
        }

        [Command("trlyrics", "trlyr")]
        [Checks.UnPpcable]
        [Description("Текст песни (Перевод)")]
        public async Task LyricsTr(string lang, [Remainder] string song)
        {
            var user = new User(Context.Sender, Context.BotCtx.Connection, Context);

            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.BotCtx.Connection);

                    string lyricsOut = await lyrics.GetLyrics();

                    if (await user.RemItemFromInv(Context.BotCtx.Shop, "money", 1000 + lyricsOut.Length))
                    {
                        var (from, to) = ParseLang(lang);
                        var resultTranslated = await Transalator.TranslateBing(lyricsOut, from, to);

                        await Context.SendMessage(Context.Channel, resultTranslated.translations[0].text);
                    }
                }
                catch (Exception e)
                {
                    await Context.SendMessage(Context.Channel, e.Message);
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Instumental");
            }
        }
    }
}
