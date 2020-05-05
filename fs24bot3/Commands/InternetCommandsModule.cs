using fs24bot3.Models;
using Newtonsoft.Json;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace fs24bot3
{
    public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

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

        [Command("executeurl", "execurl")]
        [Description("Тоже самое что и @exec только работает через URL")]
        public async void ExecuteAPIUrl(string code, string rawurl)
        {
            string response = await http.MakeRequestAsync(rawurl);
            if (response != null)
            {
                ExecuteAPI(code, response);
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ =(");   
            }
        }

        [Command("tr", "translate")]
        [Description("Переводчик")]
        [Remarks("Параметр lang нужно вводить в формате 'sourcelang-translatelang' или 'traslatelang' в данном случае переводчик попытается догадаться с какого языка пытаются перевести\nВсе языки вводятся по стандарту ISO-639-1 посмотреть можно здесь: https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%8F%D0%B7%D1%8B%D0%BA%D0%BE%D0%B2")]
        public async void Translate(string lang, [Remainder] string text)
        {
            try
            {
                var translatedOutput = await Core.Transalator.Translate(lang, text);
                Context.SendMessage(Context.Channel, translatedOutput.text[0] + " (translate.yandex.ru) " + translatedOutput.lang);
            }
            catch (Exception)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст....");
            }
        }

        [Command("trppc", "translateppc")]
        [Description("Переводчик (ппц), вводи и поражайся")]
        public async void TranslatePpc([Remainder] string text)
        {
            try
            {
                var user = new UserOperations(Context.Message.From, Context.Connection, Context);

                if (user.RemItemFromInv("beer", (int)Math.Floor((decimal)text.Length / 3) + 1))
                {
                    var translatedOutput = await Core.Transalator.Translate("ru", text);

                    foreach (string lang in new string[] { "ru", "ro-ru", "de-ru", "mn-ru", "ky-ru" })
                    {
                        var tr = await Core.Transalator.Translate(lang, translatedOutput.text[0]);
                        translatedOutput.text[0] = tr.text[0];
                    }

                    Context.SendMessage(Context.Channel, translatedOutput.text[0] + " (translate.yandex.ru, ппц) ");
                }
            }
            catch (Exception)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст....");
            }
        }

        [Command("trppclite")]
        [Description("Переводчик (ппц lite)")]
        public async void TranslatePpc2([Remainder] string text)
        {
            try
            {
                var splitted = text.Split(" ");

                if (splitted.Length > 15)
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Royal}У вас слишком жесткий текст ({splitted.Length} слов) его обработка может занять некоторое время...");
                }

                // Forech statement cannot be modified WHY???????
                for (int i = 0; i < splitted.Length - 1; i++)
                {
                    var tr = await Core.Transalator.Translate("ru", splitted[i]);
                    splitted[i] = tr.text[0];
                }

                Context.SendMessage(Context.Channel, string.Join(' ', splitted) + " (translate.yandex.ru, ппц lite edition) ");
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

                    if (user.RemItemFromInv("money", 2000 + lyricsOut.Length))
                    {
                        var resultTranslated = await Core.Transalator.Translate("ru", lyricsOut);

                        Context.SendMultiLineMessage(resultTranslated.text[0]);
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

        [Command("dmlyrics", "dmlyr")]
        [Description("Текст песни (Ппц)")]
        public async void LyricsDm([Remainder] string song)
        {
            var user = new UserOperations(Context.Message.From, Context.Connection, Context);

            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.Connection);

                    string lyricsOut = await lyrics.GetLyrics();


                    if (user.RemItemFromInv("beer", (int)Math.Floor((decimal)lyricsOut.Length / 20)))
                    {
                        foreach (string lang in new string[] { "ru", "ro-ru", "de-ru", "mn-ru", "ky-ru" })
                        {
                            var lyricsTr = await Core.Transalator.Translate(lang, lyricsOut);
                            lyricsOut = lyricsTr.text[0];
                        }

                        Context.SendMultiLineMessage(lyricsOut);
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
