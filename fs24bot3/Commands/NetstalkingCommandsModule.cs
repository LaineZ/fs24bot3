using fs24bot3.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using VkNet.Model.RequestParams;
using System.Text.RegularExpressions;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;

namespace fs24bot3
{
    public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        [Command("ms", "search")]
        [Description("Поиск@Mail.ru - Мощный инстурмент нетсталкинга")]
        [Remarks("Запрос разбивается на сам запрос и параметры которые выглядят как `PARAMETR:VALUE`\n" +
            "Параметры: page:Number - Искать на странице (иногда глючит, если не находит - попробуйте большее число, раз так в 10)\n" +
            "exclude:word - Исключить запросы с словом word; site:URL - Поиск по адресу сайта; multi:on - Мульти вывод (сразу 5 результатов)")]
        public async void MailSearch([Remainder] string query)
        {

            int page = 0;
            int limit = 1;

            string[] queryOptions = query.Split(" ");
            List<string> queryText = new List<string>();
            List<string> exclude = new List<string>();
            string site = "";

            for (int i = 0; i < queryOptions.Length; i++)
            {

                if (queryOptions[i].Contains("page:"))
                {
                    string[] options = queryOptions[i].Split(":");
                    page = int.Parse(options[1]);
                }
                else if (queryOptions[i].Contains("exclude:"))
                {
                    string[] options = queryOptions[i].Split(":");
                    exclude.Add(options[1].ToLower());
                }
                else if (queryOptions[i].Contains("site:"))
                {
                    string[] options = queryOptions[i].Split(":");
                    site = options[1].ToLower();
                }
                else if (queryOptions[i].Contains("multi:on"))
                {
                    limit = 5;
                }
                else
                {
                    queryText.Add(queryOptions[i]);
                }
            }

            string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + string.Join(" ", queryText) + "&sf=" + page + "&site=" + site);

            string startString = "go.dataJson = {";
            string stopString = "};";

            string searchDataTemp = response.Substring(response.IndexOf(startString) + startString.Length - 1);
            string searchData = searchDataTemp.Substring(0, searchDataTemp.IndexOf(stopString) + 1);

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                MailSearch.RootObject items = JsonConvert.DeserializeObject<MailSearch.RootObject>(searchData, settings);

                Log.Information("@MS: Antirobot-blocked?: {0}", items.antirobot.blocked);

                if (!items.antirobot.blocked)
                {
                    if (items.serp.results.Count > 0)
                    {
                        int results = 0;

                        foreach (var item in items.serp.results)
                        {
                            if (!item.is_porno && item.title != null && item.title.Length > 0)
                            {
                                StringBuilder searchResult = new StringBuilder(item.title);
                                searchResult.Replace("<b>", IrcColors.Bold);
                                searchResult.Replace("</b>", IrcColors.Reset);

                                StringBuilder descResult = new StringBuilder(item.passage);
                                descResult.Replace("<b>", IrcColors.Bold);
                                descResult.Replace("</b>", IrcColors.Reset);


                                HtmlDocument doc = new HtmlDocument();

                                doc.LoadHtml(descResult.ToString());

                                string desc = doc.DocumentNode.InnerText;

                                string url = item.url;
                                var match = exclude.FirstOrDefault(x => item.title.ToLower().Contains(x));

                                if (match == null)
                                {
                                    Context.SendMessage(Context.Channel, searchResult.ToString() + IrcColors.Green + " // " + url);
                                    if (limit <= 1) { Context.SendMessage(Context.Channel, desc); }
                                    results++;
                                    if (results == limit) { break; }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (results == 0)
                        {
                            Context.SendMessage(Context.Channel, IrcColors.Gray + "Ничего не найдено по вашим опциям поиска...");
                        }
                    }
                    else
                    {
                        Context.SendMessage(Context.Channel, IrcColors.Gray + "Ничего не найдено");
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, "Вы были забанены reason: " + RandomMsgs.GetRandomMessage(RandomMsgs.BanMessages));
                }
            }
            catch (JsonReaderException)
            {
                Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка блин..........");
            }
        }

        [Command("vksearch", "vks", "groups")]
        [Description("Поиск душевных групп в ВК")]
        public async void VkGroups(int count = 99, int rangemin = 100, int rangemax = 32900000, int minmembers = 2)
        {
            if (count > 0 && count < 151)
            {
                Random random = new Random();
                List<string> vkg = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    vkg.Add(random.Next(rangemin, rangemax).ToString());
                }

                var groups = await Context.VKApi.Groups.GetByIdAsync(vkg, null, GroupsFields.All);

                vkg.Clear();


                List<(string Name, string Url, string Img)> vkgs = new List<(string, string, string)>();

                foreach (var group in groups)
                {
                    if (group.MembersCount > minmembers && group.IsClosed == VkNet.Enums.GroupPublicity.Public)
                    {
                        try
                        {
                            vkgs.Add((group.Name, Url: "https://vk.com/club" + group.Id, Img: group.Photo200.AbsoluteUri));
                        }
                        catch (InvalidOperationException)
                        {
                            vkgs.Add((group.Name, Url: "https://vk.com/club" + group.Id, Img: "https://gitlab.com/uploads/-/system/user/avatar/2374023/avatar.png"));
                        }
                    }
                }

                if (vkgs.Count > 8)
                {
                    Context.SendMessage(Context.Channel, string.Join(" ", vkgs.Take(5).Select(x => x.Name + " // " + x.Url)));
                    Context.SendMessage(Context.Channel, await http.UploadToTrashbin(
                        string.Join("<br>", vkgs.Select(x => $"<img width=100 height=100 src=\"{x.Img}\"><a href=\"{x.Url}\">{x.Name}</a>"))));
                }
                else
                {
                    Context.SendMessage(Context.Channel, string.Join(" ", vkg));
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "капец ты математик");
            }
        }
    }
}
