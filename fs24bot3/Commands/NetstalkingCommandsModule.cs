using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Newtonsoft.Json;
using Qmmands;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    private static readonly string[] SearxUrls =
    {
        "https://search.trom.tf/",
        "https://searx.dresden.network/",
        "https://searx.mastodontech.de/",
        "https://searx.mxchange.org/",
        "https://searx.namejeff.xyz/",
        "https://searx.nixnet.services/",
        "https://searx.roflcopter.fr/",
        "https://searx.ru/",
        "https://searx.tuxcloud.net/",
        "https://searx.tyil.nl/",
        "https://searx.win/",
        "https://searx.xyz/",
        "https://searx.zapashcanon.fr/",
        "https://searxng.nicfab.eu/",
        "https://suche.tromdienste.de/",
        "https://search.mdosch.de/",
        "https://search.neet.works/",
        "https://search.sapti.me/"
    };
    
    public CommandService Service { get; set; }
    
    [Command("sx", "searx", "ms")]
    [Description("SearX - Еще один инструмент нетсталкинга")]
    public async Task SearxSearch([Remainder] string query)
    {
        for (var i = 0; i < SearxUrls.Length; i++)
        {
            HttpResponseMessage response = await Context.HttpTools.GetResponseAsync(
                SearxUrls[i] + "search?q=" + 
                query
                + "&category_general=1&pageno=1&language=all&time_range=None&safesearch=2&format=json");


            if (response == null) { continue; }

            try
            {
                var search = JsonConvert.DeserializeObject<Searx.Root>(await response.Content.ReadAsStringAsync(),
                JsonSerializerHelper.OPTIMIMAL_SETTINGS);

                if (search == null || !search.Results.Any()) { continue; }
                var result = search?.Results.Random();

                await Context.SendMessage(Context.Channel, $"{result.Title} [blue]// {result.Url}\n" +
                                                           $"{result.Content ?? "Нет описания"}");
                return;
            }
            catch (JsonException)
            {
                continue;
            }
        }
        
        await Context.SendSadMessage();
    }
}
