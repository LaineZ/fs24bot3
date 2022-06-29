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

namespace fs24bot3.Commands
{
    public sealed class NetstalkingCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        private readonly HttpTools http = new HttpTools();
        private readonly HttpClient client = new HttpClient();
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
                    await Context.SendMessage(Context.Channel, $"{MessageHelper.BoldToIrc(item.Title)} // {IrcClrs.Blue}{item.Url}");
                    if (ctx.Limit <= 1) { await Context.SendMessage(Context.Channel, MessageHelper.BoldToIrc(item.Description)); }
                }
            }
            else
            {
                var rand = ctx.SearchResults.Random();
                await Context.SendMessage(Context.Channel, $"{MessageHelper.BoldToIrc(rand.Title)} // {IrcClrs.Blue}{rand.Url}");
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

        [Command("ms", "mailsearch")]
        [Description("Поиск@Mail.ru - Мощный инстурмент нетсталкинга")]
        [Remarks("Запрос разбивается на сам запрос и параметры которые выглядят как `PARAMETR:VALUE`. Все параметры с типом String, кроме `regex` - регистронезависимы\n" +
            "page:Number - Страница поиска; max:Number - Максимальная глубина поиска; site:String - Поиск по адресу сайта; multi:Boolean - Мульти вывод (сразу 5 результатов);\n" +
            "random:Boolean - Рандомная выдача (не работает с multi); include:String - Включить результаты с данной подстрокой; exclude:String - Исключить результаты с данной подстрокой;\n" +
            "regex:String - Регулярное выражение в формате PCRE")]
        public async Task MailSearch([Remainder] string query)
        {
            List<(Command, string)> searchOptions = new List<(Command, string)>();
            var paser = new Core.OneLinerOptionParser(query);

            SearchCommandService.AddModule<SearchQueryCommands>();
            var ctx = new SearchCommandProcessor.CustomCommandContext
            {
                PreProcess = true
            };

            foreach ((string opt, string value) in paser.Options)
            {
                var cmd = SearchCommandService.GetAllCommands().Where(x => x.Name == opt).FirstOrDefault();

                if (cmd == null)
                {
                    await Context.SendMessage(Context.Channel, $"Неизвестная опция: `{opt}`");
                    return;
                }
                searchOptions.Add((cmd, value));
            }

            // execute pre process commands
            await ExecuteCommands(searchOptions, ctx);
            // weird visibility bug
            string inp = paser.RetainedInput;
            for (int i = ctx.Page; i < ctx.Page + ctx.Max; i++)
            {
                Log.Verbose("Foring {0}/{1}/{2} Query string: {3}", i, ctx.Page, ctx.Max, query);

                if (ctx.SearchResults.Count >= ctx.Limit) { break; }
                string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + inp + "&sf=" + (i * 10));

                if (response == null)
                {
                    Context.SendSadMessage(Context.Channel, "Не удается установить соединение с сервером. Возможно...");
                    return;
                }

                var items = InternetServicesHelper.PerformDecode(response);

                if (items == null) { continue; }

                if (items.antirobot.blocked)
                {
                    Log.Warning("Antirobot-blocked: {0} reason {1}", items.antirobot.blocked, items.antirobot.message);
                    await Context.SendMessage(Context.Channel, $"Вы были забанены reason: {RandomMsgs.BanMessages.Random()} Пожалуйста, используйте команду {IrcClrs.Bold}{Context.User.GetUserPrefix()}sx {query}");
                    return;
                }
                else
                {
                    if (items.serp.results.Any())
                    {
                        foreach (var item in items.serp.results)
                        {
                            if (!item.is_porno && item.title != null && item.title.Length > 0)
                            {
                                ctx.SearchResults.Add(new ResultGeneric(item.title, item.url, item.passage));
                            }
                        }
                    }
                }
            }

            // execute post process commands
            ctx.PreProcess = false;
            await ExecuteCommands(searchOptions, ctx);
            await PrintResults(ctx);
        }

        [Command("sx", "searx")]
        [Description("SearX - Еще один инструмент нетсталкинга")]
        [Remarks("Запрос разбивается на сам запрос и параметры которые выглядят как `PARAMETR:VALUE`. Все параметры с типом String, кроме `regex` - регистронезависимы\n" +
            "page:Number - Страница поиска; max:Number - Максимальная глубина поиска; site:String - Поиск по адресу сайта; multi:Boolean - Мульти вывод (сразу 5 результатов);\n" +
            "random:Boolean - Рандомная выдача (не работает с multi); include:String - Включить результаты с данной подстрокой; exclude:String - Исключить результаты с данной подстрокой;\n" +
            "regex:String - Регулярное выражение в формате PCRE")]
        public async Task SearxSearch([Remainder] string query)
        {
            List<(Command, string)> searchOptions = new List<(Command, string)>();

            SearchCommandService.AddModule<SearchQueryCommands>();
            var ctx = new SearchCommandProcessor.CustomCommandContext();
            var paser = new Core.OneLinerOptionParser(query);
            ctx.PreProcess = true;

            foreach ((string opt, string value) in paser.Options)
            {
                var cmd = SearchCommandService.GetAllCommands().Where(x => x.Name == opt).FirstOrDefault();

                if (cmd == null)
                {
                    await Context.SendMessage(Context.Channel, $"Неизвестная опция: `{opt}`");
                    return;
                }

                searchOptions.Add((cmd, value));
            }

            // execute pre process commands
            await ExecuteCommands(searchOptions, ctx);

            // weird visibility bug
            string inp = paser.RetainedInput;

            for (int i = ctx.Page + 1; i < ctx.Page + ctx.Max; i++)
            {
                Log.Verbose("Foring {0}/{1}/{2} Query string: {3}", i, ctx.Page, ctx.Max, query);
                if (ctx.SearchResults.Count >= ctx.Limit) { break; }

                MultipartFormDataContent form = new MultipartFormDataContent
                {
                    { new StringContent(inp), "q" },
                    { new StringContent(i.ToString()), "pageno" },
                    { new StringContent("json"), "format" }
                };

                HttpResponseMessage response = await client.PostAsync("https://anon.sx/search", form);
                var search = JsonConvert.DeserializeObject<Searx.Root>(await response.Content.ReadAsStringAsync());

                if (search.results != null)
                {
                    foreach (var item in search.results)
                    {
                        if (item.url.Contains(ctx.Site))
                        {
                            ctx.SearchResults.Add(new ResultGeneric(item.title, item.url, item.content ?? "Нет описания"));
                        }
                    }
                }
            }

            ctx.PreProcess = false;
            await ExecuteCommands(searchOptions, ctx);
            await PrintResults(ctx);
        }
    }
}
