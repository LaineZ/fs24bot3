using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Genbox.WolframAlpha;
using HtmlAgilityPack;
using MCQuery;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }
    private readonly HttpTools http = new HttpTools();


    [Command("execute", "exec")]
    [Description("REPL. поддерживает множество языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
    public async Task ExecuteAPI(string lang, [Remainder] string code)
    {
        APIExec.Input codeData = new APIExec.Input
        {
            clientId = ConfigurationProvider.Config.Services.JdoodleClientID,
            clientSecret = ConfigurationProvider.Config.Services.JdoodleClientSecret,
            language = lang,
            script = code
        };

        try
        {
            var output = await http.PostJson("https://api.jdoodle.com/v1/execute", codeData);
            var jsonOutput = JsonConvert.DeserializeObject<APIExec.Output>(output);


            if (jsonOutput.cpuTime != null && jsonOutput.memory != null)
            {
                await Context.SendMessage(Context.Channel, $"CPU: {jsonOutput.cpuTime * 1000} ms Mem: {jsonOutput.memory} KiB");
            }

            if (jsonOutput.output != null)
            {
                await Context.SendMessage(Context.Channel, jsonOutput.output);
            }
            else
            {
                var jsonErr = JsonConvert.DeserializeObject<APIExec.JsonError>(output);
                await Context.SendMessage(Context.Channel, 
                    $"Ошибка работы API сервиса: {jsonErr.error} ({jsonErr.statusCode})");
            }
        }
        catch (HttpRequestException)
        {
            await Context.SendMessage(Context.Channel, $"[gray]Не работает короче, блин........");
        }
    }

    [Command("executeurl", "execurl")]
    [Description("Тоже самое что и exec только работает через URL")]
    public async Task ExecuteAPIUrl(string code, string rawurl)
    {
        var response = await http.GetTextPlainResponse(rawurl);
        await ExecuteAPI(code, response);
    }


    [Command("shopcurrency", "shopcur", "curshop", "curshp")]
    [Description("Курсы валют различных интернет магазинов. Поддерживается только USD.")]
    [Remarks("Параметр shop допускает следующие значения: `ЦБ-РФ`, `Aliexpress`, `GearBest`, `GeekBuying`, `Banggood`")]
    public async Task ShopCur(float usd = 1.0f, string shop = "aliexpress")
    {
        var curs = await InternetServicesHelper.GetShopCurrencies();

        if (!curs.ContainsKey(shop))
        {
            Context.SendSadMessage(Context.Channel);
            return;
        }
        await Context.SendMessage($"{shop}: {usd} USD -> {curs[shop.ToLower()] * usd} RUB");
    }

    [Command("addlyrics", "addlyr")]
    [Description("Добавить свои слова в базу бота: параметр song должен быть в формате `artist - trackname`")]
    public async Task Addlyrics(string rawurl, [Remainder] string song)
    {
        var data = song.Split(" - ");
        string artist;
        string track;

        if (data.Length <= 1)
        {
            Context.SendErrorMessage(Context.Channel, "Недопустмый синтаксис команды: параметр song должен быть в формате `artist - trackname`!");
            return;
        }
        else
        {
            artist = data[0];
            track = data[1];
        }

        var response = await http.GetTextPlainResponse(rawurl);
        var lyric = new SQL.LyricsCache()
        {
            AddedBy = Context.User.Username,
            Lyrics = response,
            Artist = artist,
            Track = track
        };

        Context.User.SetContext(Context);

        try
        {
            if (await Context.User.RemItemFromInv(Context.BotCtx.Shop, "money", 2000))
            {
                Context.BotCtx.Connection.Insert(lyric);
                Context.SendErrorMessage(Context.Channel, "Добавлено!");
            }
        }
        catch (SQLiteException)
        {
            Context.SendErrorMessage(Context.Channel, "[ДЕНЬГИ ВОЗВРАЩЕНЫ] Такая песня уже существует в базе!");
            Context.User.AddItemToInv(Context.BotCtx.Shop, "money", 2000);
        }
    }

    [Command("lyrics", "lyr")]
    [Description("Текст песни")]
    public async Task Lyrics([Remainder] string song)
    {
        var data = song.Split(" - ");
        if (data.Length > 0)
        {
            try
            {
                Helpers.Lyrics lyrics = new Helpers.Lyrics(data[0], data[1], Context.BotCtx.Connection);
                await Context.SendMessage(Context.Channel, await lyrics.GetLyrics());
            }
            catch (Exception e)
            {
                Context.SendErrorMessage(Context.Channel, "Ошибка при получении слов: " + e.Message);
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, "Instumental");
        }
    }

    [Command("isblocked", "blocked", "block", "blk", "isup", "isdown", "ping")]
    [Description("Заблокирован ли сайт в России?")]
    public async Task IsBlocked([Remainder] string url)
    {
        var output = await http.PostJson("https://isitblockedinrussia.com/", new IsBlockedInRussia.RequestRoot() { host = url });
        var jsonOutput = JsonConvert.DeserializeObject<IsBlockedInRussia.Root>(output);

        int totalblocks = 0;
        int totalips = jsonOutput.ips.Count;

        foreach (var item in jsonOutput.ips)
        {
            if (item.blocked.Any())
            {
                totalblocks += 1;
            }
        }

        if (totalblocks > 0 || jsonOutput.domain.blocked.Any())
        {
            await Context.SendMessage(Context.Channel, $"[b]{url}[r]: заблокировано [red]{totalblocks}[r] айпишников из [green]{totalips}[r]!!!" +
                $" Также заблочено доменов: [b][red]{jsonOutput.domain.blocked.Count}[r] Подробнее: https://isitblockedinrussia.com/?host={url}");
        }
        else
        {
            var urik = new UriBuilder(url);
            bool response = await http.PingHost(urik.Host);
            if (response)
            {
                await Context.SendMessage(Context.Channel, $"[green]{urik.Host}: Не заблокирован!");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"[red]{urik.Host}: Не смог установить соединение с сайтом, возможно сайт заблокирован.");
            }
        }
    }

    [Command("whrand", "whowrand", "howrand")]
    public async Task WikiHowRand()
    {
        var resp = await new HttpTools().GetResponseAsync("https://ru.wikihow.com/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:Randomizer");
        await Context.SendMessage(Context.Channel, resp.RequestMessage.RequestUri.ToString());
    }


    [Command("cur", "currency", "coin")]
    [Description("Конвертер валют")]
    public async Task Currency(float amount = 1, string codeFirst = "USD", string codeSecond = "RUB", string bankProvider = "")
    {
        var resp = await http.MakeRequestAsync("https://api.exchangerate.host/latest?base=" + codeFirst + "&amount=" + amount + "&symbols=" + codeSecond + "&format=csv&source=" + bankProvider);

        // "code","rate","base","date"
        // "RUB","82,486331","USD","2022-04-07" -- this

        if (resp == null)
        {
            Context.SendSadMessage(Context.Channel, "Сервак не пашет");
            return;
        }

        try
        {
            string currency = resp.Split("\n")[1];
            var info = currency.Split("\",\"");
            string gotConvCode = info[0].Replace("\"", "");
            string valueString = info[1].Replace("\"", "");
            string gotCode = info[2].Replace("\"", "");

            if (valueString == "NaN") { valueString = "∞"; }

            await Context.SendMessage(bankProvider.ToUpper() + ": " + amount + " " + gotCode + " -> " + valueString + " " + gotConvCode);
        }
        catch (IndexOutOfRangeException)
        {
            Context.SendSadMessage(Context.Channel);
        }
    }

    [Command("stocks", "stock")]
    [Description("Акции. параметр lookUpOnlySymbol позволяет сразу искать акцию по символу, а не названию компании")]
    public async Task Stocks(string stock = "AAPL", bool lookUpOnlySymbol = false)
    {
        var resp = await http.MakeRequestAsync(
            "https://finnhub.io/api/v1/search?q=" + stock + "&token=" + 
            ConfigurationProvider.Config.Services.FinnhubKey);

        if (resp == null)
        {
            Context.SendSadMessage(Context.Channel, "Сервак не пашет");
            return;
        }

        SymbolLookup.Root symbolLookup = JsonConvert.DeserializeObject<SymbolLookup.Root>(resp);
        var lookup = symbolLookup.result.FirstOrDefault();

        if (lookup == null)
        {
            Context.SendSadMessage(Context.Channel);
            return;
        }

        resp = await http.MakeRequestAsync("https://finnhub.io/api/v1/quote?symbol=" + lookup.symbol + 
                                           "&token=" + ConfigurationProvider.Config.Services.FinnhubKey);

        if (resp == null || lookUpOnlySymbol)
        {
            // trying just find stock by symbol
            resp = await http.MakeRequestAsync("https://finnhub.io/api/v1/quote?symbol=" + stock + 
                                               "&token=" + ConfigurationProvider.Config.Services.FinnhubKey);
            if (resp == null)
            {
                // give up
                return;
            }
        }

        Stock.Root stockObj = JsonConvert.DeserializeObject<Stock.Root>(resp);

        if (stockObj == null)
        {
            Context.SendSadMessage(Context.Channel, "Не удалось найти акцию!");
        }

        await Context.SendMessage(
            $"({lookup.description}) {lookup.symbol} [b]{stockObj.c} USD[r] (низ: [red]{stockObj.l} [r]/ выс: [green]{stockObj.h})");
    }

    [Command("curcmp", "currencycomapre", "currencycomp", "curcompare", "ccmp")]
    public async Task CurrencyCompare(float amount = 1, string codeFirst = "USD", string codeSecond = "RUB")
    {
        foreach (string bank in new string[] { "boc", "nbu", "bnro", "nob" })
        {
            await Currency(amount, codeFirst, codeSecond, bank);
        }
    }

    [Command("wh", "wikihow")]
    public async Task WikiHow([Remainder] string query)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync("https://ru.wikihow.com/wikiHowTo?search=" + query);
        HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//a[@class=\"result_link\"]");
        if (divContainer != null)
        {
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//br"))
                node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

            foreach (var node in divContainer)
            {
                Log.Verbose(node.InnerText);
                string hrefValue = node.GetAttributeValue("href", string.Empty);
                var title = node.SelectSingleNode("//div[@class=\"result\"]").SelectSingleNode("//div[@class=\"result_title\"]");
                await Context.SendMessage(Context.Channel, $"{title.InnerText} // {hrefValue}");
                break;
            }
        }
    }

    [Command("wa", "wolfram", "wolframalpha")]
    [Description("Wolfram|Alpha — база знаний и набор вычислительных алгоритмов, вопросно-ответная система. Не является поисковой системой.")]
    public async Task Wolfram([Remainder] string query)
    {
        WolframAlphaClient client = new WolframAlphaClient(ConfigurationProvider.Config.Services.WolframID);
        var results = await client.QueryAsync(query);

        if (results.IsError)
        {
            Context.SendErrorMessage(Context.Channel, $"Ошибка при работе сервиса: {results.ErrorDetails}");
            return;
        }

        if (!results.IsSuccess || !results.Pods.Any())
        {
            Context.SendSadMessage(Context.Channel, RandomMsgs.NotFoundMessages.Random());
            return;
        }

        var result = results.Pods[0].SubPods[0].Plaintext;

        foreach (var pod in results.Pods.Take(3))
        {
            if (pod.IsPrimary)
            {
                foreach (var subPod in pod.SubPods)
                {
                    if (!string.IsNullOrEmpty(subPod.Plaintext))
                    {
                        var output = subPod.Plaintext.Split("\n");
                        await Context.SendMessage(Context.Channel, $"[b]{result}[r] = {string.Join(" ", output)}");
                        return;
                    }
                }
            }
        }

        // falling back to old view
        foreach (var pod in results.Pods.Take(2))
        {
            foreach (var subPod in pod.SubPods.Take(2))
            {
                if (!string.IsNullOrEmpty(subPod.Plaintext))
                    await Context.SendMessage(Context.Channel, $"[b]{pod.Title}: [r]{subPod.Plaintext}");
            }
        }
    }

    [Command("pearls", "inpearls", "inp", "ip")]
    [Description("Самые душевные цитаты в мире!")]
    public async Task InPearls(string category = "", int page = 0)
    {
        var pagenum = Context.Random.Next(page, 36);
        try
        {
            var output = await Context.ServicesHelper.InPearls(category, pagenum);

            if (output.Any())
            {
                await Context.SendMessage(Context.Channel, output.Random());
            }
            else
            {
                Context.SendSadMessage(Context.Channel);
            }
        }
        catch (InvalidOperationException e)
        {
            Context.SendSadMessage(Context.Channel, $"Не удалось получить цитаты с inpearls: {e.Message}");
        }
    }

    [Command("talk", "chatbow")]
    [Description("Чатбот")]
    public async Task Chatbot([Remainder] string message)
    {
        var msg = new ChatBotRequest(message);
        var output = await Context.HttpTools.PostJson("https://xu.su/api/send", msg);
        var jsonOutput = JsonConvert.DeserializeObject<ChatBotResponse>(output);
        if (jsonOutput != null && jsonOutput.Ok)
        {
           await Context.SendMessage(Context.Channel, $"{Context.User.Username}: {jsonOutput.Text}");
        }
        else
        {
            await Context.SendMessage(Context.Channel, $"{Context.User.Username}: я не понимаю о чем ты");
        }
    }

    [Command("mc", "minecraft", "mineserver", "mineserv")]
    [Description("Информация о сервере Minecraft")]
    public async Task MinecraftQuery(string ipaddr)
    {
        var hostname = http.ParseHostname(ipaddr);
        MCServer server = new MCServer(hostname.Address.ToString(), hostname.Port);
        ServerStatus status = server.Status();
        double ping = server.Ping();

        await Context.SendMessage(Context.Channel,
        $"[b]{ipaddr}[r]: " +
        $"({status.Version.Name}): Игроки: [b]{status.Players.Online}/{status.Players.Max}[r] [green]Пинг: {ping} мс");
    }

    [Command("oweather", "openweather", "openweathermap")]
    public async Task OpenWeatherMap([Remainder] string omskWhereYouLive = "")
    {
        string city = Context.User.GetCity(omskWhereYouLive);
        if (city == "" && omskWhereYouLive == "")
        {
            Context.SendSadMessage(Context.Channel, "Пожалуйста, установите город!");
            return;
        }

        try
        {
            var wr = await Context.ServicesHelper.OpenWeatherMap(city);
            await Context.SendMessage($"[b]{wr.CityName}[r]: {wr.Condition.GetDescription()} {wr.Temperature} °C" +
            $" (ощущения: {wr.FeelsLike} °C) Влажность: {wr.Humidity}% Ветер: {wr.WindDirection.GetDescription()} {wr.WindSpeed} m/s");
        }
        catch (ArgumentNullException)
        {
            Context.SendSadMessage(Context.Channel);
        }
    }

    [Command("weather", "yanderweather")]
    public async Task YandexWeather([Remainder] string omskWhereYouLive = "")
    {
        string city = Context.User.GetCity(omskWhereYouLive);
        if (city == "" && omskWhereYouLive == "")
        {
            Context.SendSadMessage(Context.Channel, "Пожалуйста, установите город!");
            return;
        }

        try
        {
            var wr = await Context.ServicesHelper.YandexWeather(city);
            await Context.SendMessage($"По данным Яндекс.Погоды в [b]{wr.CityName}[r]: {wr.Condition.GetDescription()} {wr.Temperature} °C" +
            $" (ощущения: {wr.FeelsLike} °C) Влажность: {wr.Humidity}% Ветер: {wr.WindDirection.GetDescription()} {wr.WindSpeed} m/s");
        }
        catch (ArgumentNullException)
        {
            Context.SendSadMessage(Context.Channel);
        }
        catch (Exception)
        {
            Context.SendSadMessage(Context.Channel, "Яндекс.Погода не работает, пробуем OpenWeatherMap...");
            await OpenWeatherMap(omskWhereYouLive);
        }
    }
}
