using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static fs24bot3.Models.IsBlockedInRussia;

namespace fs24bot3.Commands;
public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }

    [Command("ms", "search")]
    [Description("SearX - Еще один инструмент нетсталкинга")]
    [Disabled]
    public async Task MailSearch([Remainder] string query)
    {

        var response = await Context.HttpTools.GetResponseAsync($"https://mail.ru/search?search_source=mailru_desktop_safe&msid=1&encoded_text=AABArlkfOyVDiFe9OeDQwBn12mGgOHWbU5OVHcMnmH_hlnBoESlbsfaVOLDG9Py2-eGyi6ixRmjfcrzWj9_2lDVl40KEVg%2C%2C&serp_path=%2Fsearch%2F&type=web&text={{query}}\"");
        var content = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(content);
        Log.Information("{0}", content);
        var organicTitleElements = doc.DocumentNode.Descendants()
            .Where(n => n.GetAttributeValue("class", string.Empty).Contains("OrganicTitleContentSpan"));
        Log.Information("{0}", organicTitleElements);
    }
}
