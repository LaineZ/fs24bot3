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
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    public CommandService Service { get; set; }
    private readonly CommandService SearchCommandService = new CommandService();


    private async Task PrintResults(SearchCommandProcessor.CustomCommandContext ctx)
    {
        if (ctx.SearchResults == null || !ctx.SearchResults.Any())
        {
            Context.SendSadMessage(Context.Channel);
            return;
        }

        if (!ctx.Random)
        {
            foreach (var item in ctx.SearchResults.Take(ctx.Limit))
            {
                await Context.SendMessage(Context.Channel, $"{MessageHelper.BoldToIrc(item.Title)} // [blue]{item.Url}");
                if (ctx.Limit <= 1) { await Context.SendMessage(Context.Channel, MessageHelper.BoldToIrc(item.Description)); }
            }
        }
        else
        {
            var rand = ctx.SearchResults.Random();
            await Context.SendMessage(Context.Channel, $"{MessageHelper.BoldToIrc(rand.Title)} // [blue]{rand.Url}");
            if (ctx.Limit <= 1) { await Context.SendMessage(Context.Channel, MessageHelper.BoldToIrc(rand.Description)); }
        }
    }

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

    [Command("ms", "search", "sx")]
    [Description("Поиск Bing - мощный инструмент нетсталкинга")]
    [Remarks("Запрос разбивается на сам запрос и параметры которые выглядят как `PARAMETR:VALUE`. Все параметры с типом String, кроме `regex` - регистронезависимы\n" +
        "page:Number - Страница поиска; max:Number - Максимальная глубина поиска; site:String - Поиск по адресу сайта; multi:Boolean - Мульти вывод (сразу 5 результатов);\n" +
        "random:Boolean - Рандомная выдача (не работает с multi); include:String - Включить результаты с данной подстрокой; exclude:String - Исключить результаты с данной подстрокой;\n" +
        "regex:String - Регулярное выражение в формате PCRE")]
    public async Task MailSearch([Remainder] string query)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://bing-web-search1.p.rapidapi.com/search?q=" + query + 
                                 "&textFormat=Raw&safeSearch=Moderate"),
            Headers =
            {
                { "X-BingApis-SDK", "true" },
                { "X-RapidAPI-Key", ConfigurationProvider.Config.Services.RapidApiKey },
                { "X-RapidAPI-Host", "bing-web-search1.p.rapidapi.com" },
            },
        };
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
        {
            Context.SendSadMessage(Context.Channel);
            return;
        }

        var searchResults = JsonConvert.DeserializeObject<BingSearchResults.Root>(body);

        if (searchResults == null)
        {   
            Context.SendSadMessage(Context.Channel);
            return;
        }

        var res = searchResults.WebPages.Value.FirstOrDefault();

        if (res == null)
        {
            Context.SendSadMessage(Context.Channel);
            return;
        }

        await Context.SendMessage(Context.Channel, $"{res.Name} // [blue]{res.Url}");
        await Context.SendMessage(Context.Channel, res.Snippet);

    }
}
