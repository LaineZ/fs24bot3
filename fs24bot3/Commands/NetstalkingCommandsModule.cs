using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        private readonly HttpTools http = new HttpTools();
        private readonly HttpClient client = new HttpClient();
        private readonly CommandService SearchCommandService = new CommandService();


        private async Task PrintResults(SearchCommandProcessor.CustomCommandContext ctx)
        {
            if (!ctx.SearchResults.Any())
            {
                await Context.SendMessage(Context.Channel, IrcColors.Gray + RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
                return;
            }

            if (!ctx.Random)
            {
                foreach (var item in ctx.SearchResults.Take(ctx.Limit))
                {
                    await Context.SendMessage(Context.Channel, $"{Core.MailSearchDecoder.BoldToIrc(item.Title)} // {IrcColors.Blue}{item.Url}");
                    if (ctx.Limit <= 1) { await Context.SendMessage(Context.Channel, Core.MailSearchDecoder.BoldToIrc(item.Description)); }
                }
            }
            else
            {
                var rand = new Random().Next(0, ctx.SearchResults.Count - 1);
                await Context.SendMessage(Context.Channel, $"{Core.MailSearchDecoder.BoldToIrc(ctx.SearchResults[rand].Title)} // {IrcColors.Blue}{ctx.SearchResults[rand].Url}");
                if (ctx.Limit <= 1) { await Context.SendMessage(Context.Channel, Core.MailSearchDecoder.BoldToIrc(ctx.SearchResults[rand].Description)); }
            }
        }

        private async void FormatError(IResult result)
        {
            switch (result)
            {
                case TypeParseFailedResult err:
                    await Context.SendMessage(Context.Channel, $"Ошибка типа в `{err.Parameter}` необходимый тип `{err.Parameter.Type.Name}` вы же ввели `{err.Value.GetType().Name}`");
                    break;
                case ArgumentParseFailedResult err:
                    await Context.SendMessage(Context.Channel, $"Ошибка парсера: `{err.FailureReason}`");
                    break;
                case CommandExecutionFailedResult err:
                    await Context.SendMessage(Context.Channel, $"Ошибка: `{err.Exception.Message}`");
                    break;
            }
        }

        [Command("ms", "mailsearch")]
        [Description("Поиск@Mail.ru - Мощный инстурмент нетсталкинга")]
        [Remarks("Запрос разбивается на сам запрос и параметры которые выглядят как `PARAMETR:VALUE`. Все параметры с типом String, кроме `regex` - регистронезависимы\n" +
            "page:Number - Страница поиска; max:Number - Максимальная глубина поиска; site:String - Поиск по адресу сайта; multi:Boolean - Мульти вывод (сразу 5 результатов);\n" +
            "random:Boolean - Рандомная выдача (не работает с multi); include:String - Включить результаты с данной подстрокой; exclude:String - Исключить результаты с данной подстрокой;\n" +
            "regex:String - Регулярное выражение в формате PCRE")]
        public async Task MailSearch([Remainder] string query)
        {
            List<(Command, string)> searchOptions = new List<(Command, string)>();
            var paser = new Core.OneLinerOptionParser(query);

            SearchCommandService.AddModule<SearchQueryCommands>();
            var ctx = new SearchCommandProcessor.CustomCommandContext();
            ctx.PreProcess = true;

            foreach ((string opt, string value) in paser.Options)
            {
                var cmd = SearchCommandService.GetAllCommands().Where(x => x.Name == opt).FirstOrDefault();

                if (cmd == null)
                {
                    await Context.SendMessage(Context.Channel, $"Неизвестная опция: `{opt}`");
                    return;
                }

                Log.Verbose("{0}", value);
                searchOptions.Add((cmd, value));
            }

            // execute pre process commands
            foreach ((Command cmd, string args) in searchOptions)
            {
                var result = await SearchCommandService.ExecuteAsync(cmd, args, ctx);
                FormatError(result);
                if (!result.IsSuccessful) { return; }
            }

            for (int i = ctx.Page; i < ctx.Max; i++)
            {
                Log.Verbose("Foring {0}/{1}/{2} Query string: {3}", i, ctx.Page, ctx.Max, query);

                if (ctx.SearchResults.Count >= ctx.Limit) { break; }
                string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + query + "&sf=" + (i * 10) + "&site=" + ctx.Site);

                if (response == null)
                {
                    Context.SendSadMessage(Context.Channel, "Не удается установить соединение с сервером. Возможно...");
                    return;
                }

                var items = Core.MailSearchDecoder.PerformDecode(response);

                if (items == null) { continue; }

                if (items.antirobot.blocked)
                {
                    Log.Warning("Antirobot-blocked: {0} reason {1}", items.antirobot.blocked, items.antirobot.message);
                    await Context.SendMessage(Context.Channel, "Вы были забанены reason: " + RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
                    return;
                }
                else
                {
                    if (items.serp.results.Any())
                    {
                        foreach (var item in items.serp.results)
                        {
                            if (!item.is_porno && item.title != null && item.title.Length > 0)
                            {
                                ctx.SearchResults.Add(new ResultGeneric(item.title, item.url, item.passage));
                            }
                        }
                    }
                }
            }

            ctx.PreProcess = false;
            foreach ((Command cmd, string args) in searchOptions)
            {
                var result = await SearchCommandService.ExecuteAsync(cmd, args, ctx);
                FormatError(result);
                if (!result.IsSuccessful) { return; }
            }

            await PrintResults(ctx);
        }

        [Command("bc", "bandcamp", "bcs")]
        [Description("Поиск по сайту bandcamp.com")]
        public async Task BcSearch([Remainder] string query)
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
                        if (rezik.is_label) { continue; }

                        switch (rezik.type)
                        {
                            case "a":
                                await Context.SendMessage(Context.Channel, $"Альбом: {rezik.name} от {rezik.band_name} // {IrcColors.Blue}{rezik.url}");
                                return;
                            case "b":
                                await Context.SendMessage(Context.Channel, $"Артист/группа: {rezik.name} // {IrcColors.Blue}{rezik.url}");
                                return;
                            case "t":
                                await Context.SendMessage(Context.Channel, $"{rezik.band_name} - {rezik.name} // {IrcColors.Blue}{rezik.url}");
                                return;
                            default:
                                continue;
                        }
                    }
                }
                Context.SendSadMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
            }
            catch (JsonSerializationException)
            {
                Context.SendSadMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
            }
        }

        [Command("bcr", "bcd", "bcdisc", "bandcampdiscover", "bcdiscover")]
        [Description("Поиск по тегам на сайте bandcamp.com")]
        public async Task BcDiscover(uint mult = 1, [Remainder] string tagsStr = "metal")
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
                            await Context.SendMessage(Context.Channel, $"{rezik.artist} - {rezik.title} // {IrcColors.Blue}{rezik.tralbum_url}");
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

        [Command("sx")]
        [Description("Еще один инструмент нетсталкинга")]
        public async Task SearxSearch([Remainder] string query)
        {
            List<(Command, string)> searchOptions = new List<(Command, string)>();

            SearchCommandService.AddModule<SearchQueryCommands>();
            var ctx = new SearchCommandProcessor.CustomCommandContext();
            var paser = new Core.OneLinerOptionParser(query);
            ctx.PreProcess = true;

            foreach ((string opt, string value) in paser.Options)
            {
                var cmd = SearchCommandService.GetAllCommands().Where(x => x.Name == opt).FirstOrDefault();

                if (cmd == null)
                {
                    await Context.SendMessage(Context.Channel, $"Неизвестная опция: `{opt}`");
                    return;
                }

                Log.Verbose("{0}", value);
                searchOptions.Add((cmd, value));
            }

            // execute pre process commands
            foreach ((Command cmd, string args) in searchOptions)
            {
                var result = await SearchCommandService.ExecuteAsync(cmd, args, ctx);
                FormatError(result);
                if (!result.IsSuccessful) { return; }
            }

            // weird visibility bug
            string inp = paser.RetainedInput;

            for (int i = ctx.Page + 1; i < ctx.Max; i++)
            {
                MultipartFormDataContent form = new MultipartFormDataContent();

                form.Add(new StringContent(inp), "q");
                form.Add(new StringContent(i.ToString()), "pageno");
                form.Add(new StringContent("json"), "format");

                HttpResponseMessage response = await client.PostAsync("https://searx.xyz/search", form);
                var search = JsonConvert.DeserializeObject<Searx.Root>(await response.Content.ReadAsStringAsync());

                if (search.results != null)
                {
                    foreach (var item in search.results)
                    {
                        ctx.SearchResults.Add(new ResultGeneric(item.title, item.url, item.content ?? "Нет описания"));
                    }
                }
            }

            ctx.PreProcess = false;
            foreach ((Command cmd, string args) in searchOptions)
            {
                var result = await SearchCommandService.ExecuteAsync(cmd, args, ctx);
                FormatError(result);
                if (!result.IsSuccessful) { return; }
            }

            await PrintResults(ctx);
        }
    }
}
