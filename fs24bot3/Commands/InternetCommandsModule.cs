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
    public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        private string RecursiveHtmlDecode(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;
            var tmp = HttpUtility.HtmlDecode(str);
            while (tmp != str)
            {
                str = tmp;
                tmp = HttpUtility.HtmlDecode(str);
            }
            return str; //completely decoded string
        }


        private async Task<string> InPearlsGetter(string category = "", int page = 0)
        {
            if (page == 0)
            {
                page = new Random().Next(1, 35);
            }
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync("https://www.inpearls.ru/" + category + "?page=" + page);
            HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[@class=\"text\"]");
            var nodes = doc.DocumentNode.SelectNodes("//br");

            List<string> pearls = new List<string>();
            Log.Verbose("Page: {0}", page);
            if (divContainer != null && nodes != null)
            {
                foreach (HtmlNode node in nodes)
                    node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

                foreach (var node in divContainer)
                {
                    if (node.InnerText.Split("\n").Length <= 2)
                    {
                        pearls.Add(RecursiveHtmlDecode(node.InnerText));
                    }
                }

                if (pearls.Any())
                {
                    return RandomMsgs.GetRandomMessage(pearls);
                }
                else
                {
                    Context.SendSadMessage(Context.Channel, $"Подходящие сообщения в категории `{category}` не найдены!");
                }
            }
            else
            {
                Context.SendSadMessage(Context.Channel, $"Категории: `{category}` не существует!");
            }

            return null;
        }

        [Command("execute", "exec")]
        [Description("REPL. поддерживает множество языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
        public async Task ExecuteAPI(string lang, [Remainder] string code)
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
                    await Context.SendMessage(Context.Channel, "CPU: " + jsonOutput.cpuTime + " Mem: " + jsonOutput.memory);
                    await Context.SendMessage(Context.Channel, jsonOutput.output);
                }
                else
                {
                    await Context.SendMessage(Context.Channel, "Сервер вернул: " + responseString);
                }
            }
            catch (HttpRequestException)
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не работает короче, блин........");
            }
        }

        [Command("executeurl", "execurl")]
        [Description("Тоже самое что и @exec только работает через URL")]
        public async Task ExecuteAPIUrl(string code, string rawurl)
        {
            var response = await http.GetResponseAsync(rawurl);
            if (response != null)
            {
                if (response.ContentType.Contains("text/plain"))
                {
                    Stream responseStream = response.GetResponseStream();
                    await ExecuteAPI(code, new StreamReader(responseStream).ReadToEnd());
                }
                else
                {
                    await Context.SendMessage(Context.Channel, $"{IrcColors.Red}НЕ ПОЛУЧИЛОСЬ =( Потому что Content-Type запроса: {response.ContentType} а надо text/plain!");
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось выполнить запрос...");
            }
        }


        [Command("addlyrics", "addlyr")]
        [Description("Добавить свои слова в базу бота: параметр song должен быть в формате `artist - trackname`")]
        public async Task Addlyrics(string rawurl, [Remainder] string song)
        {
            var data = song.Split(" - ");
            string artist;
            string track;

            if (data.Length <= 1)
            {
                Context.SendErrorMessage(Context.Channel, "Недопустмый синтаксис команды: параметр song должен быть в формате `artist - trackname`!");
                return;
            }
            else
            {
                artist = data[0];
                track = data[1];
            }

            var response = await http.GetResponseAsync(rawurl);
            if (response != null)
            {
                if (response.ContentType.Contains("text/plain"))
                {
                    Stream responseStream = response.GetResponseStream();
                    string lyricData = new StreamReader(responseStream).ReadToEnd();

                    var lyric = new SQL.LyricsCache()
                    {
                        AddedBy = Context.Sender,
                        Lyrics = lyricData,
                        Artist = artist,
                        Track = track
                    };

                    var user = new User(Context.Sender, Context.BotCtx.Connection, Context);

                    try
                    {
                        if (await user.RemItemFromInv("money", 2000))
                        {
                            Context.BotCtx.Connection.Insert(lyric);
                            Context.SendErrorMessage(Context.Channel, "Добавлено!");
                        }
                    }
                    catch (SQLiteException)
                    {
                        Context.SendErrorMessage(Context.Channel, "[ДЕНЬГИ ВОЗВРАЩЕНЫ] Такая песня уже существует в базе!");
                        user.AddItemToInv("monney", 2000);
                    }
                }
                else
                {
                    await Context.SendMessage(Context.Channel, $"{IrcColors.Red}НЕ ПОЛУЧИЛОСЬ =( Потому что Content-Type запроса: {response.ContentType} а надо text/plain!");
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось выполнить запрос...");
            }
        }

        [Command("lyrics", "lyr")]
        [Description("Текст песни")]
        public async Task Lyrics([Remainder] string song)
        {
            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.BotCtx.Connection);

                    await Context.SendMessage(Context.Channel, await lyrics.GetLyrics());
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

        [Command("whrand", "whowrand", "howrand")]
        public async Task WikiHowRand()
        {
            var resp = await new HttpTools().GetResponseAsync("https://ru.wikihow.com/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:Randomizer");
            await Context.SendMessage(Context.Channel, resp.ResponseUri.ToString());
        }

        [Command("wh", "wikihow")]
        public async Task WikiHow([Remainder] string query)
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
                    await Context.SendMessage(Context.Channel, $"{title.InnerText} // {hrefValue}");
                    break;
                }
            }
        }

        [Command("wa", "wolfram", "wolframalpha")]
        [Description("Wolfram|Alpha — база знаний и набор вычислительных алгоритмов, вопросно-ответная система. Не является поисковой системой.")]
        public async Task Wolfram([Remainder] string query)
        {
            WolframAlphaClient client = new WolframAlphaClient(Configuration.wolframID);
            var results = await client.QueryAsync(query);

            if (results.IsError)
            {
                Context.SendErrorMessage(Context.Channel, $"Ошибка при работе сервиса: {results.ErrorDetails}");
                return;
            }

            if (!results.IsSuccess || !results.Pods.Any())
            {
                Context.SendSadMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
                return;
            }

            foreach (var pod in results.Pods.Take(3))
            {
                foreach (var subPod in pod.SubPods)
                {
                    if (!string.IsNullOrEmpty(subPod.Plaintext))
                        await Context.SendMessage(Context.Channel, $"{IrcColors.Bold}{pod.Title}: {IrcColors.Reset}{subPod.Plaintext}");
                }
            }
        }


        [Command("pearls", "inpearls", "inp", "ip")]
        [Description("Самые душевные цитаты в мире!")]
        public async Task InPearls(string category = "", int page = 0)
        {
            var output = await InPearlsGetter(category, page);
            if (output != null)
            {
                await Context.SendMessage(Context.Channel, output);
            }
        }
    }
}
