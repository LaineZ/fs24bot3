using fs24bot3.Models;
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
                    await Context.SendMessage(Context.Channel, "CPU: " + jsonOutput.cpuTime + " Mem: " + jsonOutput.memory);
                    Context.SendMultiLineMessage(jsonOutput.output);
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
        public async void ExecuteAPIUrl(string code, string rawurl)
        {
            var response = await http.GetResponseAsync(rawurl);
            if (response != null)
            {
                if (response.ContentType.Contains("text/plain"))
                {
                    Stream responseStream = response.GetResponseStream();
                    ExecuteAPI(code, new StreamReader(responseStream).ReadToEnd());
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
        public async void Addlyrics(string rawurl, [Remainder] string song)
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
                        // TODO: fix that
                        Artist = artist,
                        Track = track
                    };

                    var user = new User(Context.Sender, Context.Connection, Context);

                    try
                    {
                        if (user.RemItemFromInv("money", 2000))
                        {
                            Context.Connection.Insert(lyric);
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

        [Command("aigen", "gensent", "ppc")]
        public void GenAI(uint max = 200)
        {
            var user = new User(Context.Sender, Context.Connection, Context);
            if (user.RemItemFromInv("beer", 1))
            {
                AITranslate("ar", " ذضصثقفغعهخجدشسيبلاتنمكطئءؤرلاىةوزظ", max);
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
                await Context.SendMessage(Context.Channel, $"{translatedOutput.text} ({from}-{translatedOutput.to}, bing.com/translator)");
            }
            catch (Exception)
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось перевести текст..... =( Возможно вы неправильно ввели код языка. Используйте @helpcmd tr чтобы узнать как правильно пользоваться.");
            }
        }

        [Command("trppc")]
        [Description("Переводчик (ппц)")]
        public async void TranslatePpc([Remainder] string text)
        {
            var usr = new User(Context.Sender, Context.Connection, Context);
            if (usr.RemItemFromInv("beer", 1))
            {
                await Context.SendMessage(Context.Channel, Core.Transalator.TranslatePpc(text) + " (bing.com/translator, ппц)");
            }
        }

        [Command("trppcgen")]
        [Description("Переводчик (ппц)")]
        public async void TranslatePpcGen(int gens, [Remainder] string text)
        {
            var usr = new User(Context.Sender, Context.Connection, Context);
            if (usr.RemItemFromInv("beer", 1))
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
        [Description("Переводчик (ппц lite). Параметр lang вводится так же как и в @tr")]
        public async void TranslatePpc2(string lang, [Remainder] string text)
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
        [Description("Текст песни (ппц)")]
        public async void LyricsPpc([Remainder] string song)
        {
            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.Connection);
                    string translated = await lyrics.GetLyrics();
                    var usr = new User(Context.Sender, Context.Connection, Context);

                    if (usr.RemItemFromInv("beer", 1))
                    {
                        string[] translations = { "ru", "ar", "pl", "fr", "ja", "es", "ro", "de", "ru" };

                        foreach (var tr in translations)
                        {
                            var translatorResponse = await Core.Transalator.Translate(translated, "auto-detect", tr);
                            translated = translatorResponse.text;
                        }

                        Context.SendMultiLineMessage(translated);
                    }
                }
                catch (Exception e)
                {
                    Context.SendMultiLineMessage("Ошибка при получении слов: " + e.Message);
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Instumental");
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
                await Context.SendMessage(Context.Channel, "Instumental");
            }
        }

        [Command("whrand", "whowrand", "howrand")]
        public async void WikiHowRand()
        {
            var resp = await new HttpTools().GetResponseAsync("https://ru.wikihow.com/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:Randomizer");
            await Context.SendMessage(Context.Channel, resp.ResponseUri.ToString());
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
                    await Context.SendMessage(Context.Channel, $"{title.InnerText} // {hrefValue}");
                    break;
                }
            }
        }


        [Command("pearls", "inpearls", "inp", "ip")]
        [Description("Самые душевные цитаты в мире!")]
        public async void InPearls(string category = "", int page = 0)
        {
            var output = await InPearlsGetter(category, page);
            if (output != null)
            {
                await Context.SendMessage(Context.Channel, output);
            }
        }

        [Command("pearlsppc", "inpearlsppc", "inppc", "pppc")]
        public async void InPearlsPpc(string category = "", int page = 0)
        {
            var output = await InPearlsGetter(category, page);
            if (output != null)
            {
                TranslatePpc(output);
            }
        }

        [Command("trlyrics", "trlyr")]
        [Description("Текст песни (Перевод)")]
        public async void LyricsTr([Remainder] string song)
        {
            var user = new User(Context.Sender, Context.Connection, Context);

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
                await Context.SendMessage(Context.Channel, "Instumental");
            }
        }

        [Command("u", "unicode")]
        public async void FindUnicode(string query)
        {
            string definedCodePoints = await http.MakeRequestAsync("http://unicode.org/Public/UNIDATA/UnicodeData.txt");
            StringReader reader = new StringReader(definedCodePoints);
            UTF8Encoding encoder = new UTF8Encoding();
            List<string> output = new List<string>();
            while (true)
            {
                string line = reader.ReadLine();

                if (line == null) break;

                var props = line.Split(";");

                int codePoint = Convert.ToInt32(props[0], 16);
                if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
                {
                    //surrogate boundary; not valid codePoint, but listed in the document
                }
                else
                {
                    string utf16 = char.ConvertFromUtf32(codePoint);
                    byte[] utf8 = encoder.GetBytes(utf16);

                    string name = props[1];
                    string hexcode = "0x" + props[0];

                    if (name == "<control>")
                    {
                        name = props[10];
                    }

                    if (query.ToLower().Contains(name.ToLower()) || query == encoder.GetString(utf8))
                    {
                        output.Add($"{encoder.GetString(utf8)} {IrcColors.Green}({name}, {hexcode})");
                    }
                }
            }

            if (output.Any())
            {
                await Context.SendMessage(Context.Channel, string.Join(", ", output));
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Red}{RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages)}");
            }
        }
    }
}
