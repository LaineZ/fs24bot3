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
    }
}
