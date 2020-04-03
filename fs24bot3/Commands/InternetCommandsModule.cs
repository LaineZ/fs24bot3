using fs24bot3.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
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

        [Command("ms", "search")]
        [Description("Поиск@Mail.ru")]
        public async void MailSearch([Remainder] string query)
        {

            string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + query);

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
                        foreach (var item in items.serp.results)
                        {
                            if (!item.is_porno)
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

                                Context.SendMessage(Context.Channel, searchResult.ToString() + Models.IrcColors.Green + " // " + url);
                                Context.SendMessage(Context.Channel, desc);
                                break;
                            }
                            else
                            {
                                continue;
                            }
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
            HttpClient client = new HttpClient();


            var formVariables = new List<KeyValuePair<string, string>>();
            formVariables.Add(new KeyValuePair<string, string>("text", text));
            var formContent = new FormUrlEncodedContent(formVariables);

            var response = await client.PostAsync("https://translate.yandex.net/api/v1.5/tr.json/translate?lang=" + lang + "&key=" + Configuration.yandexTrKey, formContent);
            var responseString = await response.Content.ReadAsStringAsync();

            Log.Verbose(responseString);

            var translatedOutput = JsonConvert.DeserializeObject<YandexTranslate.RootObject>(responseString);

            if (translatedOutput.text != null)
            {
                Context.SendMessage(Context.Channel, translatedOutput.text[0] + "(translate.yandex.ru) " + translatedOutput.lang);
            }
            else
            {
                Context.SendMessage(Context.Channel, "Сервер вернул: " + responseString);
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
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1]);

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
    }
}
