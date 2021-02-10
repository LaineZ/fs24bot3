﻿using fs24bot3.Models;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fs24bot3.Commands
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
            bool random = false;
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
                        // do not add to exclude if user just wrote a '-'
                        if (options[1].Length > 0)
                        {
                            exclude.Add(options[1].ToLower());
                        }
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
                        fullmatch = queryOptions[i].Contains("fullmatch:on");
                    }
                    else if (queryOptions[i].Contains("random:on"))
                    {
                        random = true;
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
                    Context.SendMessage(Context.Channel, IrcColors.Gray + RandomMsgs.GetRandomMessage(RandomMsgs.NotFoundMessages));
                    break;
                case MailErrors.SearchError.UnknownError:
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Ошибка блин..........");
                    break;
                default:
                    if (searchResults.Count > 0)
                    {
                        if (!random)
                        {
                            foreach (var item in searchResults.Take(limit))
                            {
                                Context.SendMessage(Context.Channel, $"{Core.MailSearchDecoder.BoldToIrc(item.title)}{IrcColors.Green} // {item.url}");
                                if (limit <= 1) { Context.SendMessage(Context.Channel, Core.MailSearchDecoder.BoldToIrc(item.passage)); }
                            }
                        }
                        else
                        {
                            var rand = new Random().Next(0, searchResults.Count - 1);
                            Context.SendMessage(Context.Channel, $"{Core.MailSearchDecoder.BoldToIrc(searchResults[rand].title)}{IrcColors.Green} // {searchResults[rand].url}");
                            if (limit <= 1) { Context.SendMessage(Context.Channel, Core.MailSearchDecoder.BoldToIrc(searchResults[rand].passage)); }
                        }
                    }
                    break;
            }
        }
    }
}
