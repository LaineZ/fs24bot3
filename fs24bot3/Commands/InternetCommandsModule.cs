using fs24bot3.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace fs24bot3
{
    public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        [Command("ms", "search")]
        [Description("Поиск@Mail.ru")]
        public async void MailSearch([Remainder] string query)
        {

            int page = 0;
            int limit = 1;

            string[] queryOptions = query.Split(" ");
            List<string> queryText = new List<string>();
            List<string> exclude = new List<string>();
            string site = "";

            for (int i = 0; i < queryOptions.Length; i++)
            {

                if (queryOptions[i].Contains("page:"))
                {
                    string[] options = queryOptions[i].Split(":");
                    page = int.Parse(options[1]);
                }
                else if (queryOptions[i].Contains("exclude:"))
                {
                    string[] options = queryOptions[i].Split(":");
                    exclude.Add(options[1].ToLower());
                }
                else if (queryOptions[i].Contains("site:"))
                {
                    string[] options = queryOptions[i].Split(":");
                    site = options[1].ToLower();
                }
                else if (queryOptions[i].Contains("multi:on"))
                {
                    limit = 5;
                }
                else
                {
                    queryText.Add(queryOptions[i]);
                }
            }

            string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + string.Join(" ", queryText) + "&sf=" + page + "&site=" + site);

            string startString = "go.dataJson = {";
            string stopString = "};";

            string searchDataTemp = response.Substring(response.IndexOf(startString) + startString.Length - 1);
            string searchData = searchDataTemp.Substring(0, searchDataTemp.IndexOf(stopString) + 1);
            
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                MailSearch.RootObject items = JsonConvert.DeserializeObject<MailSearch.RootObject>(searchData, settings);

                Log.Information("@MS: Antirobot-blocked?: {0}", items.antirobot.blocked);

                if (!items.antirobot.blocked)
                {
                    if (items.serp.results.Count > 0)
                    {
                        int results = 0;

                        foreach (var item in items.serp.results)
                        {
                            if (!item.is_porno && item.title != null && item.title.Length > 0)
                            {
                                StringBuilder searchResult = new StringBuilder(item.title);
                                searchResult.Replace("<b>", IrcColors.Bold);
                                searchResult.Replace("</b>", IrcColors.Reset);

                                StringBuilder descResult = new StringBuilder(item.passage);
                                descResult.Replace("<b>", IrcColors.Bold);
                                descResult.Replace("</b>", IrcColors.Reset);


                                HtmlDocument doc = new HtmlDocument();

                                doc.LoadHtml(descResult.ToString());

                                string desc = doc.DocumentNode.InnerText;

                                string url = item.url;
                                var match = exclude.FirstOrDefault(x => item.title.ToLower().Contains(x));

                                if (match == null)
                                {
                                    Context.SendMessage(Context.Channel, searchResult.ToString() + IrcColors.Green + " // " + url);
                                    if (limit <= 1) { Context.SendMessage(Context.Channel, desc); }
                                    results++;
                                    if (results == limit) { break; }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (results == 0)
                        {
                            Context.SendMessage(Context.Channel, IrcColors.Gray + "Ничего не найдено по вашим опциям поиска...");
                        }
                    }
                    else
                    {
                        Context.SendMessage(Context.Channel, IrcColors.Gray + "Ничего не найдено");
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, "Вы были забанены reason: " + RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
                }
            }
            catch (JsonReaderException)
            {
                Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка блин..........");
            }
        }

        [Command("execute", "exec")]
        [Description("REPL поддерживает полно языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
        public async void ExecuteAPI(string lang, [Remainder] string code)
        {
            HttpClient client = new HttpClient();

            APIExec.Input codeData = new APIExec.Input();

            codeData.clientId = Configuration.jdoodleClientID;
            codeData.clientSecret = Configuration.jdoodleClientSecret;
            codeData.language = lang;
            codeData.script = code;

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

        [Command("tr", "translate")]
        [Description("Переводчик")]
        [Remarks("Параметр lang нужно вводить в формате 'sourcelang-translatelang' или 'traslatelang' в данном случае переводчик попытается догадаться с какого языка пытаются перевести\nВсе языки вводятся по стандарту ISO-639-1 посмотреть можно здесь: https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%8F%D0%B7%D1%8B%D0%BA%D0%BE%D0%B2")]
        public async void Translate(string lang, [Remainder] string text)
        {
            var translatedOutput = await Core.Transalator.Translate(lang, text);
            Context.SendMessage(Context.Channel, translatedOutput.text[0] + "(translate.yandex.ru) " + translatedOutput.lang);
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
