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
    public sealed class BandcampCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        private readonly HttpTools http = new HttpTools();
        private readonly HttpClient client = new HttpClient();
        private readonly CommandService SearchCommandService = new CommandService();

        private async Task ExecuteCommands(List<(Command, string)> searchOptions, CommandContext ctx)
        {
            foreach ((Command cmd, string args) in searchOptions)
            {
                var result = await SearchCommandService.ExecuteAsync(cmd, args, ctx);
                FormatError(result);
                if (!result.IsSuccessful) { return; }
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
                                await Context.SendMessage(Context.Channel, $"Альбом: {rezik.name} от {rezik.band_name} // {IrcClrs.Blue}{rezik.url}");
                                return;
                            case "b":
                                await Context.SendMessage(Context.Channel, $"Артист/группа: {rezik.name} // {IrcClrs.Blue}{rezik.url}");
                                return;
                            case "t":
                                await Context.SendMessage(Context.Channel, $"{rezik.band_name} - {rezik.name} // {IrcClrs.Blue}{rezik.url}");
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
        [Remarks("Через пробел вводятся теги поиска, также доступны функции:\n" +
            "page:Number - Страница поиска; max:Number - Максимальная глубина поиска; format:string - Формат носителя: cd, cassete, vinyl, all; sort:string - Сортировка: pop, date; location:Number - ID локации")]
        public async Task BcDiscover([Remainder] string tagsStr = "metal limit:5")
        {
            List<(Command, string)> searchOptions = new List<(Command, string)>();

            SearchCommandService.AddModule<BandcampSearchQueryCommands>();
            var ctx = new BandcampSearchCommandProcessor.CustomCommandContext();
            var parser = new Core.OneLinerOptionParser(tagsStr);

            var tags = parser.RetainedInput.Split(" ");

            foreach ((string opt, string value) in parser.Options)
            {
                var cmd = SearchCommandService.GetAllCommands().Where(x => x.Name == opt).FirstOrDefault();

                if (cmd == null)
                {
                    await Context.SendMessage(Context.Channel, $"Неизвестная опция: `{opt}`");
                    return;
                }
                searchOptions.Add((cmd, value));
            }

            await ExecuteCommands(searchOptions, ctx);

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var query = new BandcampDiscoverQuery.Root()
            {
                page = ctx.Page,
                filters = new BandcampDiscoverQuery.Filters()
                {
                    format = ctx.Format,
                    location = ctx.Location,
                    sort = ctx.Sort,
                    tags = tags.ToList()
                }
            };

            for (int i = ctx.Page; i < ctx.Page + ctx.Max; i++)
            {
                HttpContent c = new StringContent(JsonConvert.SerializeObject(query), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://bandcamp.com/api/hub/2/dig_deeper", c);
                string responseString = await response.Content.ReadAsStringAsync();
                query.page = i;

                try
                {
                    BandcampDiscover.RootObject discover = JsonConvert.DeserializeObject<BandcampDiscover.RootObject>(responseString, settings);

                    if (!discover.ok || !discover.more_available)
                    {
                        Log.Warning("cannot find tracks for request {0}, retrying", tagsStr);
                    }

                    if (discover.items.Any())
                    {
                        foreach (var rezik in discover.items.Take(ctx.Limit))
                        {
                            await Context.SendMessage(Context.Channel, $"{rezik.artist} - {rezik.title} // {IrcClrs.Blue}{rezik.tralbum_url}");
                        }
                        return;
                    }
                }
                catch (JsonSerializationException)
                {
                    Log.Warning("cannot find tracks for request {0}", tagsStr);
                }
            }

            Context.SendSadMessage(Context.Channel, "Не удалось найти треки...");
        }
    }
}
