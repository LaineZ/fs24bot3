using fs24bot3.Core;
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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace fs24bot3.Commands;

public sealed class InternetCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }

    [Command("execute", "exec")]
    [Description("REPL. поддерживает множество языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
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

    [Command("executeurl", "execurl")]
    [Description("Тоже самое что и exec только работает через URL")]
    public async Task ExecuteApiUrl(string code, string rawurl)
    {
        var response = await Context.HttpTools.GetTextPlainResponse(rawurl);
        await ExecuteApi(code, response);
    }


    [Command("shopcurrency", "shopcur", "curshop", "curshp")]
    [Description("Курсы валют различных интернет магазинов. Поддерживается только USD.")]
    [Remarks("Параметр shop допускает следующие значения: `ЦБ-РФ`, `Aliexpress`, `GearBest`, `GeekBuying`, `Banggood`")]
    public async Task ShopCur(float usd = 1.0f, string shop = "aliexpress")
    {
        var curs = await InternetServicesHelper.GetShopCurrencies();

        if (!curs.ContainsKey(shop))
        {
            await Context.SendSadMessage();
            return;
        }
        await Context.SendMessage($"{shop}: {usd} USD -> {curs[shop.ToLower()] * usd} RUB");
    }

    [Command("isblocked", "blocked", "block", "blk", "isup", "isdown", "ping")]
    [Description("Заблокирован ли сайт в России?")]
    public async Task IsBlocked([Remainder] string url)
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
            var urik = new UriBuilder(url);
            bool response = await Context.HttpTools.PingHost(urik.Host);
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

    [Command("talk", "chatbot")]
    [Description("Чатбот")]
    public async Task Chatbot([Remainder] string message)
    {
        ChatBotResponse jsonOutput;
        for (int i = 0; i < 5; i++)
        {
            var fmt = $"{Context.User.Username}: {message}\nuser2: ";
            var msg = new ChatBotRequest(fmt);
            try
            {
                var output = await Context.HttpTools.PostJson("https://pelevin.gpt.dobro.ai/generate/", msg);
                jsonOutput = JsonConvert.DeserializeObject<ChatBotResponse>(output);
                if (jsonOutput != null)
                {
                    var reply = jsonOutput.Replies.FirstOrDefault();
                    if (reply != null)
                    {

                        var containsBadword = RandomMsgs.BadWordsSubstrings.Any(x => reply.ToLower().Contains(x)); ;

                        if (!containsBadword)
                        {
                            await Context.SendMessage(Context.Channel, $"{Context.User.Username}: {jsonOutput.Replies.FirstOrDefault()}");
                            return;
                        }
                    }
                }
                else
                {
                    await Context.SendMessage(Context.Channel, $"{Context.User.Username}: я не понимаю о чем ты");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                break;
            }
        }

        await Context.SendMessage(Context.Channel, $"{Context.User.Username}: {RandomMsgs.NotFoundMessages.Random()}");
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
}
