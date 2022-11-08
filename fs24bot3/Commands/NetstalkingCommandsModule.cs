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
    
    [Command("ms", "search", "sx")]
    [Description("Поиск Bing - мощный инструмент нетсталкинга")]
    public async Task BingSearch([Remainder] string query)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://bing-web-search1.p.rapidapi.com/search?q=" + query + 
                                 "&textFormat=Raw&safeSearch=Moderate&mkt=ru-RU"),
            Headers =
            {
                { "X-BingApis-SDK", "true" },
                { "X-RapidAPI-Key", ConfigurationProvider.Config.Services.RapidApiKey },
                { "X-RapidAPI-Host", "bing-web-search1.p.rapidapi.com" },
            },
        };
        using var response = await Context.HttpTools.Client.SendAsync(request);
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

        var res = searchResults.WebPages.Value.Random();

        if (res == null)
        {
            Context.SendSadMessage(Context.Channel);
            return;
        }

        await Context.SendMessage(Context.Channel, $"{res.Name} // [blue]{res.Url}");
        await Context.SendMessage(Context.Channel, res.Snippet ?? "Нет описания");

    }
}
