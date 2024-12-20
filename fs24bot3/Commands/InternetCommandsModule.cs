﻿using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Genbox.WolframAlpha;
using HtmlAgilityPack;
using MCQuery;
using NetIRC;
using Newtonsoft.Json;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fs24bot3.Commands;

public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }
    
    private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
    {
        for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            yield return day;
    }
    
    [Command("execute", "exec")]
    [Description("REPL. поддерживает множество языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
    [Cooldown(5, 2, CooldownMeasure.Minutes, Bot.CooldownBucketType.Global)]
    public async Task ExecuteApi(string lang, [Remainder] string code)
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
            var output = await Context.HttpTools.PostJson("https://api.jdoodle.com/v1/execute", codeData);
            var jsonOutput = JsonConvert.DeserializeObject<APIExec.Output>(output);

            if (jsonOutput == null)
            {
                await Context.SendSadMessage();
                return;
            }

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
        catch (Exception)
        {
            await Context.SendMessage(Context.Channel, $"[gray]Не работает короче, блин........");
        }
    }

    [Command("shopcurrency", "shopcur", "curshop", "curshp")]
    [Description("Курсы валют различных интернет магазинов. Поддерживается только USD.")]
    [Remarks("Параметр shop допускает следующие значения: `ЦБ-РФ`, `Aliexpress`, `GearBest`, `GeekBuying`, `Banggood`")]
    public async Task ShopCur(float usd = 1.0f, string shop = "aliexpress")
    {
        var curs = await InternetServicesHelper.GetShopCurrencies();

        if (!curs.ContainsKey(shop))
        {
            await Context.SendSadMessage(Context.Channel, "Такой интернет магазин не поддерживается");
            return;
        }
        await Context.SendMessage($"{shop}: {usd} USD -> {curs[shop.ToLower()] * usd} RUB");
    }

    [Command("isup", "isdown", "ping")]
    [Description("Работает ли сайт?")]
    [Cooldown(5, 1, CooldownMeasure.Minutes, Bot.CooldownBucketType.Channel)]
    public async Task IsUp(string url)
    {
        var urik = new UriBuilder(url);
        bool response = await Context.HttpTools.PingHost(urik.Host);
        if (response)
        {
            await Context.SendMessage(Context.Channel, $"[green]{urik.Host}: Работает!");
        }
        else
        {
            await Context.SendMessage(Context.Channel, $"[red]{urik.Host}: Не смог установить соединение...");
        }
    }

    [Command("isblocked", "blocked", "block", "blk")]
    [Description("Заблокирован ли сайт в России?")]
    public async Task IsBlocked(string url)
    {
        var output = await Context.HttpTools.PostJson("https://isitblockedinrussia.com/", new IsBlockedInRussia.RequestRoot() { host = url });
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
            await Context.SendMessage(Context.Channel, $"[green]{url}: Не заблокирован!");
        }
    }

    [Command("whrand", "whowrand", "howrand")]
    public async Task WikiHowRand()
    {
        var resp = await Context.HttpTools.GetResponseAsync("https://ru.wikihow.com/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:Randomizer");
        resp.EnsureSuccessStatusCode();

        if (resp.RequestMessage != null && resp.RequestMessage.RequestUri != null)
        {
            await Context.SendMessage(Context.Channel, resp.RequestMessage.RequestUri.ToString());
        }
        else
        {
            await Context.SendSadMessage();
        }
    }


    [Command("cur", "currency", "coin")]
    [Description("Конвертер валют")]
    public async Task Currency(float amount = 1, string codeFirst = "USD", string codeSecond = "RUB", string bankProvider = "")
    {
        var resp = await Context.HttpTools.GetResponseAsync
            ("https://api.exchangerate.host/latest?base=" + codeFirst + "&amount=" + amount + "&symbols=" + codeSecond + "&format=csv&source=" + bankProvider);


        resp.EnsureSuccessStatusCode();

        // "code","rate","base","date"
        // "RUB","82,486331","USD","2022-04-07" -- this
        try
        {
            string currency = resp.Content.ReadAsStringAsync().Result.Split("\n")[1];
            var info = currency.Split("\",\"");
            string gotConvCode = info[0].Replace("\"", "");
            string valueString = info[1].Replace("\"", "");
            string gotCode = info[2].Replace("\"", "");

            if (valueString == "NaN") { valueString = "∞"; }

            await Context.SendMessage(bankProvider.ToUpper() + ": " + amount + " " + gotCode + " -> " + valueString + " " + gotConvCode);
        }
        catch (IndexOutOfRangeException)
        {
            await Context.SendSadMessage();
        }
    }

    [Command("stocks", "stock")]
    [Description("Акции. параметр lookUpOnlySymbol позволяет сразу искать акцию по символу, а не названию компании")]
    public async Task Stocks(string stock = "AAPL", bool lookUpOnlySymbol = true)
    {
        var symbolLockup = await Context.HttpTools.GetJson<SymbolLookup.Root>(
            "https://finnhub.io/api/v1/search?q=" + stock + "&token=" + 
            ConfigurationProvider.Config.Services.FinnhubKey);
        
        var lookup = symbolLockup.result.FirstOrDefault();

        if (lookup == null)
        {
            await Context.SendSadMessage();
            return;
        }

        var resp = await Context.HttpTools.GetResponseAsync("https://finnhub.io/api/v1/quote?symbol=" + lookup.symbol + 
                                           "&token=" + ConfigurationProvider.Config.Services.FinnhubKey);

        if (resp == null || lookUpOnlySymbol)
        {
            // trying just find stock by symbol
            resp = await Context.HttpTools.GetResponseAsync("https://finnhub.io/api/v1/quote?symbol=" + stock + 
                                               "&token=" + ConfigurationProvider.Config.Services.FinnhubKey);
            if (resp == null)
            {
                // give up
                return;
            }
        }

        var stockObj = JsonConvert.DeserializeObject<Stock.Root>(await resp.Content.ReadAsStringAsync());

        if (stockObj == null)
        {
            await Context.SendSadMessage(Context.Channel, "Не удалось найти акцию!");
            return;
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

    [Command("prz", "prazdnik", "holiday", "kakojsegodnjaprazdnik")]
    [Description("Какой сегодня или завтра праздник?")]
    public async Task Holiday(uint month = 0, uint day = 0)
    {

        if (month == 0 || month > 12)
        {
            month = (uint)DateTime.Now.Month;
        }

        if (day == 0)
        {
            day = (uint)DateTime.Now.Day;
        }

        var humanMonth = new Dictionary<int, string>()
        {
            {1, "yanvar"  },
            {2, "fevral"  },
            {3, "mart"    },
            {4, "aprel"   },
            {5, "may"     },
            {6, "iyun"    },
            {7, "iyul"    },
            {8, "avgust"  },
            {9, "sentyabr"},
            {10, "oktyabr"},
            {11, "noyabr" },
            {12, "dekabr" },
        };


        string url = "https://kakoysegodnyaprazdnik.ru/baza/" + humanMonth[(int)month] + "/" + day;

        var response = await Context.HttpTools.GetResponseAsync(url);
        Log.Verbose(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(await response.Content.ReadAsStringAsync());

        var outputs = new List<string>();

        HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[@itemprop='suggestedAnswer']//span[@itemprop='text']");
        if (divContainer != null)
        {
            foreach (var node in divContainer)
            {
                outputs.Add(node.InnerText);
            }
        }

        await Context.SendMessage(Context.Channel, $"[b]{day}-{month}-{DateTime.Today.Year}:[r] у нас: [b]{outputs.Random()}");
    }

    [Command("wa", "wolfram", "wolframalpha")]
    [Description("Wolfram|Alpha — база знаний и набор вычислительных алгоритмов, вопросно-ответная система. Не является поисковой системой.")]
    [Cooldown(20, 10, CooldownMeasure.Minutes, Bot.CooldownBucketType.Global)]
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
            await Context.SendSadMessage();
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
                await Context.SendSadMessage();
            }
        }
        catch (InvalidOperationException e)
        {
            await Context.SendSadMessage(Context.Channel, $"Не удалось получить цитаты с inpearls: {e.Message}");
        }
    }
    
    [Command("chat", "talk", "chatgpt", "gpt", "ask")]
    [Cooldown(5, 1, CooldownMeasure.Seconds,  Bot.CooldownBucketType.Channel)]
    public async Task ChatGPT([Remainder] string message)
    {

        if (!Context.BotCtx.Gpt.Contexts.ContainsKey(Context.User))
        {
            Context.BotCtx.Gpt.Contexts[Context.User] = new DuckDuckGoGPTHelper();
            Log.Verbose("Creating new Session for USER");
        }

        var msg = await Context.BotCtx.Gpt.Contexts[Context.User].SendMessage(message);
        await Context.SendMessage($"{Context.User}: {msg}");
    }

    [Command("cleargpt", "clear")]
    public async Task ClearGPT()
    {
        
        if (Context.BotCtx.Gpt.Contexts.ContainsKey(Context.User))
        {
            Context.BotCtx.Gpt.Contexts.Remove(Context.User);
            await Context.SendMessage(Context.Channel, $"{Context.User}: Ваш контекст чата был удалён...");
        }
        else
        {
            await Context.SendSadMessage(Context.Channel, $"{Context.User}: Вас еще не чатились со мной");
        }
    }

    [Command("gchat", "globaltalk", "talkglobal")]
    [Cooldown(5, 1, CooldownMeasure.Seconds,  Bot.CooldownBucketType.Channel)]
    public async Task TalkGlobalGPT([Remainder] string message)
    {
        var msg = await Context.BotCtx.Gpt.GlobalContext.SendMessage(message);
        await Context.SendMessage(Context.Channel, $"Global: {msg}");
    }

    [Command("clearglobal")]
    public async Task ClearGlobalGPTContexnt()
    {
        Context.BotCtx.Gpt.GlobalContext = new DuckDuckGoGPTHelper();
        await Context.SendMessage(Context.Channel, "Глобальный контекст удалён!");
    }

    [Command("mc", "minecraft", "mineserver", "mineserv")]
    [Description("Информация о сервере Minecraft")]
    public async Task MinecraftQuery(string ipaddr)
    {
        var hostname = Context.HttpTools.ParseHostname(ipaddr);
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
            await Context.SendSadMessage(Context.Channel, "Пожалуйста, установите город!");
            return;
        }

        try
        {
            var wr = await Context.ServicesHelper.OpenWeatherMap(city);

            await Context.SendMessage($"[b]{wr.CityName}[r]: {wr.Condition.GetDescription()} {wr.Temperature} °C" +
            $" (ощущения: [b]{wr.FeelsLike} °C[r]) Влажность: [b]{wr.Humidity}%[r] Ветер: [b]{wr.WindDirection.GetDescription()} {wr.WindHeading}° {(wr.WindSpeed * 1.944):0.0} kts {wr.WindSpeed} m/s[r]");
        }
        catch (Exception)
        {
            await Context.SendSadMessage();
        }
    }

    [Command("metar", "mweather")]
    [Description("Запрос METAR информации о погоде")]
    public async Task Metar(string airportIcao = "URWW", bool rawOutput = false)
    {

        var response = await Context.HttpTools.GetJson<List<MetarWeather.Root>>($"https://aviationweather.gov/cgi-bin/data/metar.php?ids={airportIcao}&format=json");

        var metar = response.FirstOrDefault();

        if (!rawOutput)
        {
            if (metar == null)
            {
                await Context.SendSadMessage();
                return;
            }

            int wind = metar.Wspd;

            if (metar.RawOb.Contains("MPS"))
            {
                wind = (int)Math.Floor(wind * 1.944);
            }

            await Context.SendMessage($"[b]{metar.ReportTime}: {metar.IcaoId} ({metar.Name}):[r] [b]{metar.Temp}°C[r] QNH: [b]{metar.Altim}[r] hPA Ветер: [b]{metar.Wdir}° {wind} kts [r]Видимость: [b]{metar.Visib}[r]");
        }
        else
        {
            await Context.SendMessage(metar.RawOb);
        }
    }

    [Command("weather", "yanderweather")]
    public async Task YandexWeather([Remainder] string omskWhereYouLive = "")
    {
        string city = Context.User.GetCity(omskWhereYouLive);
        if (city == "" && omskWhereYouLive == "")
        {
            await Context.SendSadMessage(Context.Channel, "Пожалуйста, установите город!");
            return;
        }

        try
        {
            var wr = await Context.ServicesHelper.YandexWeather(city);
            await Context.SendMessage($"По данным Яндекс.Погоды в [b]{wr.CityName}[r]: {wr.Condition.GetDescription()} {wr.Temperature} °C" +
            $" (ощущения: {wr.FeelsLike} °C) Влажность: {wr.Humidity}% Ветер: {wr.WindDirection.GetDescription()} ~{wr.WindHeading}° {(wr.WindSpeed * 1.944):0.0} kts {wr.WindSpeed} m/s");
        }
        catch (ArgumentNullException)
        {
            await Context.SendSadMessage();
        }
        catch (Exception)
        {
            await Context.SendSadMessage(Context.Channel, "Яндекс.Погода не работает, пробуем OpenWeatherMap...");
            await OpenWeatherMap(omskWhereYouLive);
        }
    }
    
    [Command("getimages", "images", "img", "imagefind", "findimage")]
    [Description("Получает изображение из логов")]
    public async Task GetImagesFromLogs(string nickname, string dStart, string dEnd, bool htmlOutput = true)
    {
        Regex regex = new("https?://.*.(png|jpg|gif|webp|jpeg)");
        var dateStart = DateTime.Now;
        DateTime.TryParse(dStart, out dateStart);

        Stopwatch stopWatch = new Stopwatch();
        var result = DateTime.TryParse(dEnd, out DateTime dateEnd);
        string output = "";

        if (!result)
        {
            await Context.SendMessage(Context.Channel, "Ошибка ввода конечной даты!");
            return;
        }

        var totalDays = EachDay(dateStart, dateEnd).Count();
        int current = 0;

        foreach (var date in EachDay(dateStart, dateEnd))
        {
            stopWatch.Start();
            var messages = await Context.ServicesHelper.GetMessagesSprout(date);
            foreach (var message in messages)
            {
                var captures = regex.Match(message.Message);
                if (message.Nick == nickname && captures.Success)
                {
                    if (htmlOutput)
                    {
                        output +=
                            $"<p>{message.Date} from <strong>{message.Nick}</strong></p><img src='{captures.Value}' alt='{captures.Value}' style='width: auto; height: 100%;'>\n";
                    }
                    else
                    {
                        output += $"{captures.Value}\n";
                    }
                }
            }

            stopWatch.Stop();
            current++;

            if (Context.Random.Next(0, 1000) == 25)
            {
                var left = stopWatch.ElapsedTicks * (totalDays - current);
                await Context.SendMessage(Context.Channel,
                    $"Обработка логфайла: {current}/{totalDays} Осталось: {new TimeSpan(left).ToReadableString()}. " +
                    $"Обработка одного логфайла занимает: {stopWatch.ElapsedMilliseconds} ms");
            }

            stopWatch.Restart();
        }

        await Context.SendMessage(Context.Channel,
            await InternetServicesHelper.UploadToTrashbin(output, htmlOutput ? "add" : "addplain"));
    }
}
