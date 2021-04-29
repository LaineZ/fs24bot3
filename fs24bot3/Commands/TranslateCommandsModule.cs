using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Genbox.WolframAlpha;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace fs24bot3.Commands
{
    public sealed class TranslateCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        private (string, string) ParseLang(string input)
        {
            string[] langs = input.Split("-");

            string from = "auto-detect";
            string to = langs[0]; // auto detection

            if (input.Contains("-"))
            {
                from = langs[0];
                to = langs[1];
            }

            return (from, to);
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
                await Context.SendMessage(Context.Channel, translatedOutput.text.ToString());
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
            var user = new User(Context.Sender, Context.Connection, Context);
            if (await user.RemItemFromInv("beer", 1))
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
                (string from, string to) = ParseLang(lang);

                var translatedOutput = await Transalator.Translate(text, from, to);
                await Context.SendMessage(Context.Channel, $"{translatedOutput.text} ({from}-{translatedOutput.to}, bing.com/translator)");
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
            var usr = new User(Context.Sender, Context.Connection, Context);
            if (await usr.RemItemFromInv("beer", 1))
            {
                await Context.SendMessage(Context.Channel, Transalator.TranslatePpc(text).Result + " (bing.com/translator, ппц)");
            }
        }

        [Command("trppcgen")]
        [Checks.UnPpcable]
        [Description("Переводчик (ппц)")]
        public async Task TranslatePpcGen(int gens, [Remainder] string text)
        {
            var usr = new User(Context.Sender, Context.Connection, Context);
            if (await usr.RemItemFromInv("beer", 1))
            {
                string[] translations = { "ru", "ar", "pl", "fr", "ja", "es", "ro", "de", "ru" };
                string translated = text;

                for (int i = 0; i < Math.Clamp(gens, 1, 5); i++)
                {
                    foreach (var tr in translations)
                    {
                        var translatorResponse = await Core.Transalator.Translate(translated, "auto-detect", tr);
                        translated = translatorResponse.text;
                    }
                }

                await Context.SendMessage(Context.Channel, translated + " (bing.com/translator, ппц)");
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
                    var tr = await Core.Transalator.Translate(splitted[i], from, to);
                    splitted[i] = tr.text;
                }

                await Context.SendMessage(Context.Channel, string.Join(' ', splitted).ToLower() + " (bing.com/translator, ппц lite edition) ");
            }
            catch (Exception)
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст....");
            }
        }

        [Command("dmlyrics", "dmlyr")]
        [Checks.UnPpcable]
        [Description("Текст песни (ппц)")]
        public async Task LyricsPpc([Remainder] string song)
        {
            var data = song.Split(" - ", 1);
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.Connection);
                    string translated = await lyrics.GetLyrics();
                    var usr = new User(Context.Sender, Context.Connection, Context);

                    if (await usr.RemItemFromInv("beer", 1))
                    {
                        string[] translations = { "ru", "ar", "pl", "fr", "ja", "es", "ro", "de", "ru" };

                        foreach (var tr in translations)
                        {
                            var translatorResponse = await Core.Transalator.Translate(translated, "auto-detect", tr);
                            translated = translatorResponse.text;
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
            var user = new User(Context.Sender, Context.Connection, Context);

            var data = song.Split(" - ", 1);
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.Connection);

                    string lyricsOut = await lyrics.GetLyrics();

                    if (await user.RemItemFromInv("money", 1000 + lyricsOut.Length))
                    {
                        var lng = ParseLang(lang);
                        var resultTranslated = await Core.Transalator.Translate(lyricsOut, lng.Item1, lng.Item2);

                        Context.SendMessage(Context.Channel, resultTranslated.text.ToString());
                    }
                }
                catch (Exception e)
                {
                    await Context.SendMessage(Context.Channel, "Ошибка при получении слов: " + e.Message);
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Instumental");
            }
        }
    }
}
