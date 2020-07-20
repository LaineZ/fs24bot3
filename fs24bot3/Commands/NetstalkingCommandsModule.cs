using fs24bot3.Models;
using HtmlAgilityPack;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VkNet.Enums.Filters;

namespace fs24bot3
{
    public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        [Command("ms", "search")]
        [Description("Поиск@Mail.ru - Мощный инстурмент нетсталкинга")]
        [Remarks("Запрос разбивается на сам запрос, параметры которые выглядят как `PARAMETR:VALUE` и операторы поиска (+, -)\n" +
            "page:Number - Страница поиска; max:Number - Максимальная глубина поиска;\n" +
            "site:URL - Поиск по адресу сайта; fullmatch:on - Включить полное совпадение запроса; multi:on - Мульти вывод (сразу 5 результатов)" +
            "Операторы поиска: `+` - Включить слово в запрос `-` - Исключить слово из запроса")]
        public async void MailSearch([Remainder] string query)
        {

            // search options
            int page = 0;
            int limit = 1;
            int maxpage = 10;
            bool fullmatch = false;
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
                    if (queryOptions[i].Contains("max:"))
                    {
                        string[] options = queryOptions[i].Split(":");
                        maxpage = int.Parse(options[1]);
                    }
                    if (queryOptions[i].Contains("commongarbage:off"))
                    {
                        exclude.AddRange(new List<string>() { "mp3", "музыку", "двач", "2ch" });
                    }
                    // exclude
                    else if (queryOptions[i].Contains("-"))
                    {
                        string[] options = queryOptions[i].Split("-");
                        exclude.Add(options[1].ToLower());
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
                        fullmatch = true;
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
                Log.Verbose("Foring {0}", i);
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
                                var excludeMatch = exclude.FirstOrDefault(x => item.title.ToLower().Contains(x) || item.passage.ToLower().Contains(x) || item.url.ToLower().Contains(x));

                                if (fullmatch)
                                {
                                    if (item.title.Contains(string.Join(" ", queryText)) || item.passage.Contains(string.Join(" ", queryText)))
                                    {
                                        searchResults.Add(item);
                                    }
                                    else
                                    {
                                        continue;
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
                            else
                            {
                                continue;
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
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Ничего не найдено попробуйте изменить опции поиска");
                    break;
                case MailErrors.SearchError.UnknownError:
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка блин..........");
                    break;
                default:
                    if (searchResults.Count > 0)
                    {
                        foreach (var item in searchResults.Take(limit))
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

                            Context.SendMessage(Context.Channel, $"{searchResult} {IrcColors.Green} // {item.url}");
                            if (limit <= 1) { Context.SendMessage(Context.Channel, desc); }
                        }
                    }
                    break;
            }
        }

        [Command("vksearch", "vks", "groups")]
        [Description("Поиск душевных групп в ВК")]
        public async void VkGroups(int count = 99, int rangemin = 100, int rangemax = 32900000, int minmembers = 2)
        {
            if (count > 0 && count < 151 && rangemin < rangemax)
            {
                Random random = new Random();
                List<string> vkg = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    vkg.Add(random.Next(rangemin, rangemax).ToString());
                }

                try
                {
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
                                // reset avatar
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
                catch (Exception)
                {
                    Log.Warning("VK Session is not invalid... retrying login");
                    Context.VKApi = http.LogInVKAPI();
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка сессии VK: Попробуйте использовать команду ЕЩЕ РАЗ");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "капец ты математик");
            }
        }


        [Command("vkfind", "vs")]
        [Description("Поиск в ВК")]
        public async void VkSearch([Remainder] string query)
        {
            try
            {
                var parametrs = new VkNet.Model.RequestParams.NewsFeedSearchParams();
                parametrs.Query = query;
                parametrs.Count = 1;
                var found = await Context.VKApi.NewsFeed.SearchAsync(parametrs);

                if (found.TotalCount > 0)
                {
                    Context.SendMessage(Context.Channel, found.Items[0].Text[..Math.Min(200, found.Items[0].Text.Length)].Replace("\n", " ") + IrcColors.Lime + " // https://vk.com/wall" + found.Items[0].FromId + "_" + found.Items[0].Id); ;
                }
                else
                {
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Ничего не найдено...");
                }
            }
            catch (Exception)
            {
                Context.VKApi = http.LogInVKAPI();
                Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка сессии VK: Попробуйте использовать команду ЕЩЕ РАЗ");
            }
        }
    }
}
