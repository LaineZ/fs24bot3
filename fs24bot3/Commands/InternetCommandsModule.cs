using fs24bot3.Models;
using Newtonsoft.Json;
using Qmmands;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using Serilog;

namespace fs24bot3.Commands
{
    public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        // TODO: Turn back @trppc/@dmlyrics command

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
            if (response != null)
            {
                if (response.ContentType == "text/plain")
                {
                    Stream responseStream = response.GetResponseStream();
                    ExecuteAPI(code, new StreamReader(responseStream).ReadToEnd());
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ =( {response.ContentType}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось выполнить запрос...");
            }
        }

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

        [Command("whrand", "whowrand", "howrand")]
        public async void WikiHowRand()
        {
            var resp = await new HttpTools().GetResponseAsync("https://ru.wikihow.com/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:Randomizer");

            Context.SendMessage(Context.Channel, resp.ResponseUri.ToString());

        }

        [Command("wh", "wikihow")]
        public async void WikiHow([Remainder] string query)
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync("https://ru.wikihow.com/wikiHowTo?search=" + query);
            HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//a[@class=\"result_link\"]");
            if (divContainer != null)
            {
                foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//br"))
                    node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

                foreach (var node in divContainer)
                {
                    Log.Verbose(node.InnerText);
                    string hrefValue = node.GetAttributeValue("href", string.Empty);
                    var title = node.SelectSingleNode("//div[@class=\"result\"]").SelectSingleNode("//div[@class=\"result_title\"]");
                    Context.SendMessage(Context.Channel, $"{title.InnerText} // {hrefValue}");
                    break;
                }
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
