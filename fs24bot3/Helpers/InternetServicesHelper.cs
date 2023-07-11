using fs24bot3.Models;
using HtmlAgilityPack;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using fs24bot3.Core;
using System.Globalization;
using System.Linq;

namespace fs24bot3.Helpers;
public class InternetServicesHelper
{
    private static readonly Regex LogsRegex = new Regex(@"^\[(\d{2}:\d{2}:\d{2})\] <([^>]+)> (.+)", RegexOptions.Compiled);
    private HttpTools Http { get; }

    public InternetServicesHelper(in HttpTools http)
    {
        Http = http;
    }

    public async Task<List<string>> InPearls(string category = "", int page = 0)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync("https://www.inpearls.ru/" + category + "?page=" + page);
        HtmlNodeCollection divContainer = doc.DocumentNode.SelectNodes("//div[@class=\"text\"]");
        var nodes = doc.DocumentNode.SelectNodes("//br");

        List<string> pearls = new List<string>();
        Log.Verbose("Page: {0}", page);

        if (divContainer != null && nodes != null)
        {
            foreach (HtmlNode node in nodes)
                node.ParentNode.ReplaceChild(doc.CreateTextNode("\n"), node);

            foreach (var node in divContainer)
            {
                if (node.InnerText.Split("\n").Length <= 2)
                {
                    pearls.Add(Http.RecursiveHtmlDecode(node.InnerText));
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"Категории `{category}`");
        }

        return pearls;
    }

    public static async Task<Dictionary<string, float>> GetShopCurrencies()
    {
        var currencies = new Dictionary<string, float>();

        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync("https://helpix.ru/currency/");
        HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//td[@class=\"b-tabcurr__td\"]");
        //Log.Verbose("{0}", doc.ParsedText);

        int idx = 1;

        foreach (var item in new[] { "цб-рф", "aliexpress", "gearbest", "geekbuying", "banggood" })
        {
            try
            {
                currencies.Add(item, float.Parse(nodes[idx].InnerText, CultureInfo.InvariantCulture));
            }
            catch (FormatException)
            {
                Log.Error("Failed to get price for {0}! excepted floating point got: {1} id: {2}", item, nodes[idx].InnerText, idx);
            }
            idx++;
        }

        return currencies;
    }

    public async Task<List<FomalhautMessage>> GetMessages(DateTime dateTime)
    {
        var output =
            await Http.MakeRequestAsyncNoCookie("https://logs.fomalhaut.me/download/" +
                                                dateTime.ToString("yyyy-MM-dd") + ".log");

        var list = new List<FomalhautMessage>();

        if (output == null)
        {
            return list;
        }

        foreach (var item in output.Split("\n"))
        {
            var captures = LogsRegex.Match(item);
            var time = captures.Groups[1].Value;
            var nick = captures.Groups[2].Value;
            var message = captures.Groups[3].Value;
            if (!string.IsNullOrWhiteSpace(nick) && !string.IsNullOrWhiteSpace(message))
            {
                list.Add(new FomalhautMessage
                    { Date = dateTime.Add(TimeSpan.Parse(time)), Message = message, Nick = nick, Kind = Kind.Message });
            }
            else
            {
                // cannot parse message, looks like a server message or action
                Log.Warning("Message {0} cannot be parsed!", item);
            }
        }

        return list;
    }

    public static async Task<string> UploadToTrashbin(string data, string route = "add")
    {
        try
        {
            HttpClient client = new HttpClient();
            HttpContent c = new StringContent(data, Encoding.UTF8);

            var response = await client.PostAsync(ConfigurationProvider.Config.Services.TrashbinUrl + "/" + route, c);

            var responseString = await response.Content.ReadAsStringAsync();

            if (int.TryParse(responseString, out _))
            {
                return ConfigurationProvider.Config.Services.TrashbinUrl + "/" + responseString;
            }
            else
            {
                return "Полный вывод недоступен: " + responseString + " Статус код: " + response.StatusCode;
            }
        }
        catch (Exception)
        {
            return "Полный вывод недоступен: " + ConfigurationProvider.Config.Services.TrashbinUrl;
        }
    }

    private async Task<OpenWeatherMapResponse.Coord> GetCityLatLon(string city)
    {
        var json = await Http.GetJson<OpenWeatherMapResponse.Root>(
            "https://api.openweathermap.org/data/2.5/weather?q=" + city + 
               "&APPID=" + ConfigurationProvider.Config.Services.OpenWeatherMapKey + "&units=metric");
        return json?.Coord;
    }


    public async Task<WeatherGeneric> OpenWeatherMap(string city)
    {
        var json = await Http.GetJson<OpenWeatherMapResponse.Root>(
            "https://api.openweathermap.org/data/2.5/weather?q=" + city +
                "&APPID=" + ConfigurationProvider.Config.Services.OpenWeatherMapKey + "&units=metric");

        var condition = json.Weather.First().Id switch
        {
            (>= 200) and (<= 202) => WeatherConditions.ThunderstormWithRain,
            (>= 210) and (<= 221) => WeatherConditions.Thunderstorm,
            (>= 230) and (<= 232) => WeatherConditions.ThunderstormWithRain,
            (>= 300) and (<= 321) => WeatherConditions.Drizzle,
            500 => WeatherConditions.LightRain,
            501 => WeatherConditions.ModerateRain,
            (>= 502) and (<= 504) => WeatherConditions.HeavyRain,
            (>= 511) and (<= 531) => WeatherConditions.Showers,
            (>= 600) and (<= 616) => WeatherConditions.Snow,
            (>= 617) and (<= 622) => WeatherConditions.SnowShowers,
            (>= 701) and (<= 781) => WeatherConditions.Overcast,
            800 => WeatherConditions.Clear,
            801 => WeatherConditions.PartlyCloudy,
            802 => WeatherConditions.PartlyCloudy,
            803 => WeatherConditions.Cloudy,
            804 => WeatherConditions.Cloudy,
            _ => WeatherConditions.Clear,
        };

        var dir = json.Wind.Deg switch
        {
            (>= 0) and (<= 11) => WindDirections.N,
            (> 348) and (<= 360) => WindDirections.N,
            (> 33) and (<= 56) => WindDirections.Ne,
            (> 78) and (<= 101) => WindDirections.E,
            (> 123) and (<= 146) => WindDirections.Se,
            (> 168) and (<= 191) => WindDirections.S,
            (> 213) and (<= 236) => WindDirections.Sw,
            (> 258) and (<= 281) => WindDirections.W,
            (> 303) and (<= 326) => WindDirections.Nw,
            _ => WindDirections.N
        };

        return new WeatherGeneric()
        {
            CityName = $"{json.Name} ({json.Sys.Country})",
            Condition = condition,
            Temperature = json.Main.Temp,
            FeelsLike = json.Main.FeelsLike,
            Humidity = json.Main.Humidity,
            WindDirection = dir,
            WindSpeed = json.Wind.Speed
        };
    }

    public async Task<WeatherGeneric> YandexWeather(string city)
    {
        var latlon = await GetCityLatLon(city);

        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://api.weather.yandex.ru/v2/informers?lat=" + latlon.Lat + "&lon=" + latlon.Lon + "&lang=ru_RU"),
            Headers = {
                    { "X-Yandex-API-Key", ConfigurationProvider.Config.Services.YandexWeatherKey },
                    { "Accept", "application/json" }
                },
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };


        var response = await Http.Client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();

        if (responseString.Any() && response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var wr = JsonConvert.DeserializeObject<YandexWeather.Root>(responseString,
                     JsonSerializerHelper.OPTIMIMAL_SETTINGS);
            var cond = wr?.FactObj;

            var condition = cond?.Condition switch
            {
                "clear" => WeatherConditions.Clear,
                "partly-cloudy" => WeatherConditions.PartlyCloudy,
                "cloudy" => WeatherConditions.Cloudy,
                "overcast" => WeatherConditions.Overcast,
                "drizzle" => WeatherConditions.Drizzle,
                "light-rain" => WeatherConditions.LightRain,
                "rain" => WeatherConditions.Rain,
                "moderate-rain" => WeatherConditions.ModerateRain,
                "heavy-rain" => WeatherConditions.HeavyRain,
                "continuous-heavy-rain" => WeatherConditions.ContinuousHeavyRain,
                "showers" => WeatherConditions.Showers,
                "wet-snow" => WeatherConditions.WetSnow,
                "light-snow" => WeatherConditions.WightSnow,
                "snow" => WeatherConditions.Snow,
                "snow-showers" => WeatherConditions.SnowShowers,
                "hail" => WeatherConditions.Hail,
                "thunderstorm" => WeatherConditions.Thunderstorm,
                "thunderstorm-with-rain" => WeatherConditions.ThunderstormWithRain,
                "thunderstorm-with-hail" => WeatherConditions.ThunderstormWithHail,
                _ => WeatherConditions.Clear,
            };

            var dir = cond?.WindDir switch
            {
                "n" => WindDirections.N,
                "ne" => WindDirections.Ne,
                "e" => WindDirections.E,
                "se" => WindDirections.Se,
                "s" => WindDirections.S,
                "sw" => WindDirections.Sw,
                "w" => WindDirections.W,
                "nw" => WindDirections.Nw,
                _ => WindDirections.N
            };


            return new WeatherGeneric()
            {
                CityName = city,
                Condition = condition,
                Temperature = cond.Temp,
                FeelsLike = cond.FeelsLike,
                Humidity = cond.Humidity,
                WindDirection = dir,
                WindSpeed = cond.WindSpeed
            };
        }
        else
        {
            throw new Exception("Не удалось получить информацию о погоде!");
        }
    }

    public async Task<Translate.Response> Translate(string text, string from = "", string to = "")
    {

        string content = JsonConvert.SerializeObject(new Translate.Request(from, to, text));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://ai-translate.p.rapidapi.com/translate"),
            Headers = {
                    { "x-rapidapi-key", ConfigurationProvider.Config.Services.RapidApiKey },
                    { "x-rapidapi-host", "ai-translate.p.rapidapi.com" },
                },
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };

        var response = await Http.Client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();

        Log.Verbose(responseString);

        if (responseString.Any() && response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return JsonConvert.DeserializeObject<Translate.Response>(responseString);
        }

        throw new Exception(responseString);
    }

    public async Task<string> TranslatePpc(string text)
    {
        string[] langs = { "en", "ru", "ja", "pt", "nl", "ru" };
        string resp = text;

        foreach (var item in langs)
        {
            try
            {
                var response = await Http.PostJson(ConfigurationProvider.Config.Services.LibretranslateURL + "/translate", new LibreTranslate.Request()
                {
                    ApiKey = "",
                    RequestText = resp,
                    Source = "auto",
                    Format = "text",
                    Target = item
                });

                Log.Verbose(response);

                resp = JsonConvert.DeserializeObject<LibreTranslate.Response>(response).TranslatedText;

                Log.Verbose(resp);
            }
            catch (Exception)
            {
                continue;
            }

        }
        return resp;
    }
}
