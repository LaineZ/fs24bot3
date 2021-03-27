using fs24bot3.Models;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace fs24bot3.Commands
{
    public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();
        HttpClient client = new HttpClient();

        [Command("ms", "search")]
        [Description("Поиск@Mail.ru - Мощный инстурмент нетсталкинга")]
        [Remarks("Запрос разбивается на сам запрос, параметры которые выглядят как `PARAMETR:VALUE` и операторы поиска (+, -)\n" +
            "page:Number - Страница поиска; max:Number - Максимальная глубина поиска;\n" +
            "site:URL - Поиск по адресу сайта; fullmatch:on - Включить полное совпадение запроса; multi:on - Мульти вывод (сразу 5 результатов); random:off - Выключить рандомную выдачу" +
            "Операторы поиска: `+` - Включить слово в запрос `-` - Исключить слово из запроса")]
        public async void MailSearch([Remainder] string query)
        {
            // search options
            int page = 0;
            int limit = 1;
            int maxpage = 10;
            bool fullmatch = false;
            bool random = true;
            string site = "";

            string[] queryOptions = query.Split(" ");
            List<string> queryText = new List<string>();
            List<string> exclude = new List<string>();
            // error message
            MailErrors.SearchError errors = MailErrors.SearchError.None;

            for (int i = 0; i < queryOptions.Length; i++)
            {
                try
                {
                    if (queryOptions[i].Contains("page:"))
                    {
                        string[] options = queryOptions[i].Split(":");
                        page = int.Parse(options[1]);
                    }
                    else if (queryOptions[i].Contains("max:"))
                    {
                        string[] options = queryOptions[i].Split(":");
                        maxpage = int.Parse(options[1]);
                    }
                    // exclude
                    else if (queryOptions[i].Contains("-"))
                    {
                        string[] options = queryOptions[i].Split("-");
                        // do not add to exclude if user just wrote a '-'
                        if (options[1].Length > 0)
                        {
                            exclude.Add(options[1].ToLower());
                        }
                    }
                    else if (queryOptions[i].Contains("multi:on"))
                    {
                        limit = 5;
                    }
                    else if (queryOptions[i].Contains("site:"))
                    {
                        string[] options = queryOptions[i].Split(":");
                        site = options[1].ToLower();
                    }
                    else if (queryOptions[i].Contains("fullmatch:on"))
                    {
                        fullmatch = queryOptions[i].Contains("fullmatch:on");
                    }
                    else if (queryOptions[i].Contains("random:off"))
                    {
                        random = false;
                    }
                    else
                    {
                        queryText.Add(queryOptions[i]);
                    }
                }
                catch (FormatException)
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Red}{IrcColors.Bold}ОШИБКА:{IrcColors.Reset} Неверно задан тип");
                    Context.SendMessage(Context.Channel, $"{IrcColors.Red}{query}");
                    // 5 is page: word offest (in positive side)
                    Context.SendMessage(Context.Channel, $"{IrcColors.Bold}{new String(' ', query.IndexOf(queryOptions[i]) + 5)}^ ожидалось число");
                    return;
                }
            }
            var searchResults = new List<MailSearch.Result>();

            for (int i = page; i < maxpage; i++)
            {
                Log.Verbose("Foring {0} Query string: {1}", i, string.Join(" ", queryText));
                if (searchResults.Count >= limit) { break; }
                string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + string.Join(" ", queryText) + "&sf=" + ((i + 1) * 10) + "&site=" + site);
                var items = Core.MailSearchDecoder.PerformDecode(response);
                if (items == null) { continue; }

                Log.Information("@MS: Antirobot-blocked?: {0} reason {1}", items.antirobot.blocked, items.antirobot.message);

                if (items.antirobot.blocked)
                {
                    errors = MailErrors.SearchError.Banned;
                    break;
                }
                else
                {
                    if (items.serp.results.Count > 0)
                    {
                        foreach (var item in items.serp.results)
                        {
                            if (!item.is_porno && item.title != null && item.title.Length > 0)
                            {
                                string desc = item.passage?.ToLower() ?? string.Empty;
                                var excludeMatch = exclude.FirstOrDefault(x => item.title.ToLower().Contains(x) || desc.Contains(x) || item.url.ToLower().Contains(x));

                                if (fullmatch)
                                {
                                    if (item.title.Contains(string.Join(" ", queryText)) || item.passage.Contains(string.Join(" ", queryText)))
                                    {
                                        searchResults.Add(item);
                                    }
                                }
                                else
                                {
                                    if (excludeMatch == null)
                                    {
                                        searchResults.Add(item);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        errors = MailErrors.SearchError.NotFound;
                        break;
                    }
                }
                if (errors != MailErrors.SearchError.None) { break; }
            }

            if (searchResults.Count <= 0 && errors != MailErrors.SearchError.Banned)
            {
                errors = MailErrors.SearchError.NotFound;
            }

            switch (errors)
            {
                case MailErrors.SearchError.Banned:
                    Context.SendMessage(Context.Channel, "Вы были забанены reason: " + RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
                    break;
                case MailErrors.SearchError.NotFound:
                    Context.SendMessage(Context.Channel, IrcColors.Gray + RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
                    break;
                case MailErrors.SearchError.UnknownError:
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка блин..........");
                    break;
                default:
                    if (searchResults.Count > 0)
                    {
                        if (!random)
                        {
                            foreach (var item in searchResults.Take(limit))
                            {
                                Context.SendMessage(Context.Channel, $"{Core.MailSearchDecoder.BoldToIrc(item.title)} // {IrcColors.Blue}{item.url}");
                                if (limit <= 1) { Context.SendMessage(Context.Channel, Core.MailSearchDecoder.BoldToIrc(item.passage)); }
                            }
                        }
                        else
                        {
                            var rand = new Random().Next(0, searchResults.Count - 1);
                            Context.SendMessage(Context.Channel, $"{Core.MailSearchDecoder.BoldToIrc(searchResults[rand].title)} // {IrcColors.Blue}{searchResults[rand].url}");
                            if (limit <= 1) { Context.SendMessage(Context.Channel, Core.MailSearchDecoder.BoldToIrc(searchResults[rand].passage)); }
                        }
                    }
                    break;
            }
        }

        [Command("bc", "bandcamp", "bcs")]
        [Description("Поиск по сайту bandcamp.com")]
        public async void BcSearch([Remainder] string query)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            string response = await http.MakeRequestAsync("https://bandcamp.com/api/fuzzysearch/1/autocomplete?q=" + query);
            try
            {
                BandcampSearch.Root searchResult = JsonConvert.DeserializeObject<BandcampSearch.Root>(response, settings);
                if (searchResult.auto.results.Any())
                {
                    foreach (var rezik in searchResult.auto.results)
                    {
                        if (rezik.is_label)
                        {
                            continue;
                        }

                        switch (rezik.type)
                        {
                            case "a":
                                Context.SendMessage(Context.Channel, $"Альбом: {rezik.name} от {rezik.band_name} // {IrcColors.Blue}{rezik.url}");
                                return;
                            case "b":
                                Context.SendMessage(Context.Channel, $"Артист/группа: {rezik.name} // {IrcColors.Blue}{rezik.url}");
                                return;
                            case "t":
                                Context.SendMessage(Context.Channel, $"{rezik.band_name} - {rezik.name} // {IrcColors.Blue}{rezik.url}");
                                return;
                        }
                    }
                }
            }
            catch (JsonSerializationException)
            {
                Context.SendSadMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
            }
        }

        [Command("bcr", "bcd", "bcdisc", "bandcampdiscover", "bcdiscover")]
        [Description("Поиск по тегам на сайте bandcamp.com")]
        public async void BcDiscover(uint mult = 1, [Remainder] string tagsStr = "metal")
        {
            var tags = tagsStr.Split(" ");
            List<string> tagsFixed = new List<string>();

            foreach (string tag in tags)
            {
                if (tag.Length > 0)
                {
                    tagsFixed.Add("\"" + tag + "\"");
                }
            }

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            Random rand = new Random();

            int timeout = 0;

            while (timeout < 5)
            {
                string content = "{\"filters\":{ \"format\":\"all\",\"location\":0,\"sort\":\"pop\",\"tags\":[" + string.Join(",", tagsFixed) + "] },\"page\":" + rand.Next(0, 200) + "}";
                //Console.WriteLine(content);
                HttpContent c = new StringContent(content, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://bandcamp.com/api/hub/2/dig_deeper", c);
                string responseString = await response.Content.ReadAsStringAsync();

                try
                {
                    BandcampDiscover.RootObject discover = JsonConvert.DeserializeObject<BandcampDiscover.RootObject>(responseString, settings);

                    if (!discover.ok || !discover.more_available)
                    {
                        timeout++;
                    }

                    if (discover.items.Any())
                    {
                        for (int i = 0; i < Math.Clamp(mult, 1, 5); i++)
                        {
                            int randIdx = new Random().Next(0, discover.items.Count - 1);
                            var rezik = discover.items[randIdx];
                            Context.SendMessage(Context.Channel, $"{rezik.artist} - {rezik.title} // {IrcColors.Blue}{rezik.tralbum_url}");
                        }
                        return;
                    }
                }
                catch (JsonSerializationException)
                {
                    Log.Warning("cannot find tracks for request {0}", tagsStr);
                    timeout++;
                }
            }

            Context.SendSadMessage(Context.Channel, "Не удалось найти треки...");
        }
    }
}
