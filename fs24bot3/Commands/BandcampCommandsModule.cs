using fs24bot3.Core;
using fs24bot3.Helpers;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class BandcampCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    public CommandService Service { get; set; }
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
        string response = await Context.HttpTools.MakeRequestAsync("https://bandcamp.com/api/fuzzysearch/1/autocomplete?q=" + query);
        try
        {
            BandcampSearch.Root searchResult = JsonConvert.DeserializeObject<BandcampSearch.Root>(response, JsonSerializerHelper.OPTIMIMAL_SETTINGS);
            if (searchResult.auto.results.Any())
            {
                foreach (var rezik in searchResult.auto.results)
                {
                    if (rezik.is_label) { continue; }

                    switch (rezik.type)
                    {
                        case "a":
                            await Context.SendMessage(Context.Channel, $"Альбом: {rezik.name} от {rezik.band_name} // [blue]{rezik.url}");
                            return;
                        case "b":
                            await Context.SendMessage(Context.Channel, $"Артист/группа: {rezik.name} // [blue]{rezik.url}");
                            return;
                        case "t":
                            await Context.SendMessage(Context.Channel, $"{rezik.band_name} - {rezik.name} // [blue]{rezik.url}");
                            return;
                        default:
                            continue;
                    }
                }
            }

            Context.SendSadMessage(Context.Channel, RandomMsgs.NotFoundMessages.Random());
        }
        catch (JsonSerializationException)
        {
            Context.SendSadMessage(Context.Channel, RandomMsgs.NotFoundMessages.Random());
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
            var cmd = SearchCommandService.GetAllCommands().FirstOrDefault(x => x.Name == opt);

            if (cmd == null)
            {
                await Context.SendMessage(Context.Channel, $"Неизвестная опция: `{opt}`");
                return;
            }
            searchOptions.Add((cmd, value));
        }

        await ExecuteCommands(searchOptions, ctx);
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
            var response = await Context.HttpTools.Client.PostAsync("https://bandcamp.com/api/hub/2/dig_deeper", c);
            string responseString = await response.Content.ReadAsStringAsync();
            query.page = i;

            try
            {
                BandcampDiscover.RootObject discover = JsonConvert.DeserializeObject<BandcampDiscover.RootObject>(responseString, JsonSerializerHelper.OPTIMIMAL_SETTINGS);

                if (discover is { ok: false, more_available: false })
                {
                    Log.Warning("cannot find tracks for request {0}, retrying", tagsStr);
                }

                if (discover == null || !discover.items.Any()) continue;
                foreach (var rezik in discover.items.Take(ctx.Limit))
                {
                    await Context.SendMessage(Context.Channel, $"{rezik.artist} - {rezik.title} // [blue]{rezik.tralbum_url}");
                }
                return;
            }
            catch (JsonSerializationException)
            {
                Log.Warning("cannot find tracks for request {0}", tagsStr);
            }
        }

        Context.SendSadMessage(Context.Channel, "Не удалось найти треки...");
    }
}
