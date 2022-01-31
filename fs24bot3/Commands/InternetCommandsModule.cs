using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Genbox.WolframAlpha;
using HtmlAgilityPack;
using MCQuery;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }
        private readonly HttpTools http = new HttpTools();
        private readonly Helpers.InternetServicesHelper InternetServicesHelper = new Helpers.InternetServicesHelper();


        [Command("execute", "exec")]
        [Description("REPL. поддерживает множество языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
        public async Task ExecuteAPI(string lang, [Remainder] string code)
        {
            APIExec.Input codeData = new APIExec.Input
            {
                clientId = Configuration.JdoodleClientID,
                clientSecret = Configuration.JdoodleClientSecret,
                language = lang,
                script = code
            };

            try
            {
                var output = await http.PostJson("https://api.jdoodle.com/v1/execute", codeData);
                var jsonOutput = JsonConvert.DeserializeObject<APIExec.Output>(output);

                await Context.SendMessage(Context.Channel, $"CPU: {jsonOutput.cpuTime * 1000} ms Mem: {jsonOutput.memory} KiB");


                if (jsonOutput.output != null)
                {
                    await Context.SendMessage(Context.Channel, jsonOutput.output);
                }
                else
                {
                    await Context.SendMessage(Context.Channel, "Сервер вернул: " + output);
                }
            }
            catch (HttpRequestException)
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}Не работает короче, блин........");
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
                    await Context.SendMessage(Context.Channel, $"{IrcClrs.Red}НЕ ПОЛУЧИЛОСЬ =( Потому что Content-Type запроса: {response.ContentType} а надо text/plain!");
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}Не удалось выполнить запрос...");
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
                        if (await user.RemItemFromInv(Context.BotCtx.Shop, "money", 2000))
                        {
                            Context.BotCtx.Connection.Insert(lyric);
                            Context.SendErrorMessage(Context.Channel, "Добавлено!");
                        }
                    }
                    catch (SQLiteException)
                    {
                        Context.SendErrorMessage(Context.Channel, "[ДЕНЬГИ ВОЗВРАЩЕНЫ] Такая песня уже существует в базе!");
                        user.AddItemToInv(Context.BotCtx.Shop, "money", 2000);
                    }
                }
                else
                {
                    await Context.SendMessage(Context.Channel, $"{IrcClrs.Red}НЕ ПОЛУЧИЛОСЬ =( Потому что Content-Type запроса: {response.ContentType} а надо text/plain!");
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}Не удалось выполнить запрос...");
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

        [Command("isblocked", "blocked", "block", "blk")]
        [Description("Заблокирован ли сайт в росии?")]
        public async Task IsBlocked([Remainder] string url)
        {
            var output = await http.PostJson("http://isitblockedinrussia.com/", new IsBlockedInRussia.RequestRoot() { host = url });
            var jsonOutput = JsonConvert.DeserializeObject<IsBlockedInRussia.Root>(output);

            int totalblocks = 0;
            int totalips = jsonOutput.ips.Count;

            foreach (var item in jsonOutput.ips)
            {
                if (item.blocked.Any())
                {
                    totalblocks += 1;
                }
            }

            if (totalblocks > 0)
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Bold}{url}{IrcClrs.Reset}: заблокировано {IrcClrs.Red}{totalblocks}{IrcClrs.Reset} айпишников из {IrcClrs.Green}{totalips}{IrcClrs.Reset}!!! " +
                    $"Подробнее: https://isitblockedinrussia.com/?host={url}");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Green}{url}: Не заблокирован!");
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
            WolframAlphaClient client = new WolframAlphaClient(Configuration.WolframID);
            var results = await client.QueryAsync(query);

            if (results.IsError)
            {
                Context.SendErrorMessage(Context.Channel, $"Ошибка при работе сервиса: {results.ErrorDetails}");
                return;
            }

            if (!results.IsSuccess || !results.Pods.Any())
            {
                Context.SendSadMessage(Context.Channel, RandomMsgs.NotFoundMessages.Random());
                return;
            }

            var result = results.Pods[0].SubPods[0].Plaintext;

            foreach (var pod in results.Pods.Take(3))
            {
                if (pod.IsPrimary)
                {
                    foreach (var subPod in pod.SubPods)
                    {
                        if (!string.IsNullOrEmpty(subPod.Plaintext))
                        {
                            var output = subPod.Plaintext.Split("\n");
                            await Context.SendMessage(Context.Channel, $"{IrcClrs.Bold}{result}{IrcClrs.Reset} = {string.Join(" ", output)}");
                            return;
                        }
                    }
                }
            }

            // falling back to old view
            foreach (var pod in results.Pods.Take(2))
            {
                foreach (var subPod in pod.SubPods.Take(2))
                {
                    if (!string.IsNullOrEmpty(subPod.Plaintext))
                        await Context.SendMessage(Context.Channel, $"{IrcClrs.Bold}{pod.Title}: {IrcClrs.Reset}{subPod.Plaintext}");
                }
            }
        }


        [Command("pearls", "inpearls", "inp", "ip")]
        [Description("Самые душевные цитаты в мире!")]
        public async Task InPearls(string category = "", int page = 0)
        {
            var pagenum = Context.Random.Next(page, 36);
            try
            {
                var output = await InternetServicesHelper.InPearls(category, pagenum);

                if (output.Any())
                {
                    await Context.SendMessage(Context.Channel, output.Random());
                }
                else
                {
                    Context.SendSadMessage(Context.Channel);
                }
            }
            catch (InvalidOperationException e)
            {
                Context.SendSadMessage(Context.Channel, $"Не удалось получить цитаты с inpearls: {e.Message}");
            }
        }

        [Command("mc", "minecraft", "mineserver", "mineserv")]
        [Description("Информация о сервере Minecraft")]
        public async Task MinecraftQuery(string ipaddr)
        {
            var hostname = http.ParseHostname(ipaddr);
            MCServer server = new MCServer(hostname.Address.ToString(), hostname.Port);
            ServerStatus status = server.Status();
            double ping = server.Ping();
            await Context.SendMessage(Context.Channel, $"{IrcClrs.Bold}{ipaddr}{IrcClrs.Reset}: ({status.Version.Name}): Игроки: {IrcClrs.Bold}{status.Players.Online}/{status.Players.Max}{IrcClrs.Reset} {IrcClrs.Green}Пинг: {ping} мс");
        }
    }
}
