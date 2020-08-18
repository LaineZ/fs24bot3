using fs24bot3.Models;
using Newtonsoft.Json;
using Qmmands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using SQLite;

namespace fs24bot3
{
    public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        //private string SongameString = String.Empty;
        //private int SongameTries = 5;

        // TODO: Turn back @trppc/@dmlyrics command

        private (string, string) ParseLang(string input)
        {
            string[] langs = input.Split("-");

            string from = langs[0];
            string to = "auto-detect"; // auto detection

            if (input.Contains("-"))
            {
                to = langs[1];
            }
            else
            {
                to = input;
            }

            return (from, to);
        }

        [Command("execute", "exec")]
        [Description("REPL. поддерживает множество языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
        public async void ExecuteAPI(string lang, [Remainder] string code)
        {
            HttpClient client = new HttpClient();

            APIExec.Input codeData = new APIExec.Input
            {
                clientId = Configuration.jdoodleClientID,
                clientSecret = Configuration.jdoodleClientSecret,
                language = lang,
                script = code
            };

            try
            {
                HttpContent c = new StringContent(JsonConvert.SerializeObject(codeData), Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.jdoodle.com/v1/execute", c);
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonOutput = JsonConvert.DeserializeObject<APIExec.Output>(responseString);


                if (jsonOutput.output != null)
                {
                    Context.SendMessage(Context.Channel, "CPU: " + jsonOutput.cpuTime + " Mem: " + jsonOutput.memory);
                    Context.SendMultiLineMessage(jsonOutput.output);
                }
                else
                {
                    Context.SendMessage(Context.Channel, "Сервер вернул: " + responseString);
                }
            }
            catch (HttpRequestException)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не работает короче, блин........");
            }
        }

        [Command("executeurl", "execurl")]
        [Description("Тоже самое что и @exec только работает через URL")]
        public async void ExecuteAPIUrl(string code, string rawurl)
        {
            var response = await http.GetResponseAsync(rawurl);
            if (response != null && response.ContentType == "text/plain")
            {
                Stream responseStream = response.GetResponseStream();
                ExecuteAPI(code, new StreamReader(responseStream).ReadToEnd());
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ =( {response.ContentType}");
            }
        }

        //[Command("songame", "songg", "sg")]
        //[Description("Игра-перевод песен: введите по русски так чтобы получилось ...")]
        //public async void Songame(string translated = "")
        //{
        //    Random rand = new Random();
        //    while (SongameString.Length == 0)
        //    {
        //        var ObjectIDList = await database.QueryAsync<Object>("SELECT * FORM");
        //        if (query.Count > 0)
        //        {
        //            string[] lyrics = query[rand.Next(0, query.Count - 1)].Lyrics.Split("\n");

        //            foreach (string line in lyrics)
        //            {
        //                if (line.Length > 10 && Regex.IsMatch(line, @"^[a-zA-Z]+$"))
        //                {
        //                    SongameString = line.ToLower().Replace(",", "");
        //                    break;
        //                }
        //            }

        //        }
        //        SongameTries = 5;
        //    }


        //    if (translated.Length == 0)
        //    {
        //        Context.SendMessage(Context.Channel, $"Введи на русском так чтобы получилось: {SongameString} попыток: {SongameTries}");
        //    }
        //    else
        //    {
        //        if (!Regex.IsMatch(translated, @"^[a-zA-Z]+$"))
        //        {
        //            var translatedOutput = await Core.Transalator.Translate(translated, "ru", "en");

        //            if (translatedOutput.text.ToLower() == SongameString)
        //            {
        //                int reward = 100 * SongameTries;
        //                Context.SendMessage(Context.Channel, $"ВЫ УГАДАЛИ И ВЫИГРАЛИ {reward} ДЕНЕГ!");
        //                // reset the game
        //                SongameString = "";
        //            }
        //            else
        //            {
        //                Context.SendMessage(Context.Channel, $"Неправильно, ожидалось | получилось: {SongameString} | {translatedOutput.text[0].ToLower()}");
        //                SongameTries--;
        //            }
        //        }
        //        else
        //        {
        //            Context.SendMessage(Context.Channel, "Обнаружен английский язык!!!");
        //        }
        //    }
        //}

        [Command("tr", "translate")]
        [Description("Переводчик")]
        [Remarks("Параметр lang нужно вводить в формате 'sourcelang-translatelang' или 'traslatelang' в данном случае переводчик попытается догадаться с какого языка пытаются перевести (работает криво, претензии не к разработчику бота)\nВсе языки вводятся по стандарту ISO-639-1 посмотреть можно здесь: https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%8F%D0%B7%D1%8B%D0%BA%D0%BE%D0%B2")]
        public async void Translate(string lang, [Remainder] string text)
        {
            (string from, string to) = ParseLang(lang);

            try
            {
                var translatedOutput = await Core.Transalator.Translate(text, from, to);
                Context.SendMessage(Context.Channel, $"{translatedOutput.text} ({from}-{translatedOutput.to}, bing.com/translator)");
            }
            catch (Exception e)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст..... =( {e.Message}");
            }
        }

        [Command("trppclite", "trl")]
        [Description("Переводчик (ппц lite)")]
        public async void TranslatePpc2(string lang, [Remainder] string text)
        {

            (string from, string to) = ParseLang(lang);

            try
            {
                var splitted = text.Split(" ");

                if (splitted.Length > 35)
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Royal}У вас слишком жесткий текст ({splitted.Length} слов) его обработка может занять некоторое время...");
                }

                // Forech statement cannot be modified WHY???????
                for (int i = 0; i < splitted.Length; i++)
                {
                    var tr = await Core.Transalator.Translate(splitted[i], from, to);
                    splitted[i] = tr.text;
                }

                Context.SendMessage(Context.Channel, string.Join(' ', splitted) + " (bing.com/translator, ппц lite edition) ");
            }
            catch (Exception)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст....");
            }
        }

        [Command("lyrics", "lyr")]
        [Description("Текст песни")]
        public async void Lyrics([Remainder] string song)
        {
            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.Connection);

                    Context.SendMultiLineMessage(await lyrics.GetLyrics());
                }
                catch (Exception e)
                {
                    Context.SendMultiLineMessage("Ошибка при получении слов: " + e.Message);
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "Instumental");
            }
        }

        [Command("trlyrics", "trlyr")]
        [Description("Текст песни (Перевод)")]
        public async void LyricsTr([Remainder] string song)
        {

            var user = new UserOperations(Context.Message.From, Context.Connection, Context);

            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.Connection);

                    string lyricsOut = await lyrics.GetLyrics();

                    if (user.RemItemFromInv("money", 1000 + lyricsOut.Length))
                    {
                        var resultTranslated = await Core.Transalator.Translate(lyricsOut, "auto-detect", "ru");

                        Context.SendMultiLineMessage(resultTranslated.text.ToString());
                    }
                }
                catch (Exception e)
                {
                    Context.SendMultiLineMessage("Ошибка при получении слов: " + e.Message);
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "Instumental");
            }
        }
    }
}
