using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace fs24bot3.Models;


public enum Backend
{
    Basic,
    IRC,
}

public class Services
{
    [DataMember(Name = "jdoodle_client_id")]
    public string JdoodleClientID { get; set; }
    [DataMember(Name = "jdoodle_client_secret")]
    public string JdoodleClientSecret { get; set; }
    [DataMember(Name = "trashbin_url")]
    public string TrashbinUrl { get; set; }
    [DataMember(Name = "wolfram_id")]
    public string WolframID { get; set; }
    [DataMember(Name = "rapidapi_key")]
    public string RapidApiKey { get; set; }

    [DataMember(Name = "finnhub_key")]
    public string FinnhubKey { get; set; }
    [DataMember(Name = "bridge_nickname")]
    public string BridgeNickname { get; set; }
    [DataMember(Name = "youtube_dl_path")]
    public string YoutubeDlPath { get; set; }

    [DataMember(Name = "openweathermap_key")]
    public string OpenWeatherMapKey { get; set; }
    [DataMember(Name = "yandex_weather_key")]
    public string YandexWeatherKey { get; set; }
}

public class Configuration
{
    [DataMember(Name = "prefix")]
    public string Prefix { get; set; }
    [DataMember(Name = "loglevel")]
    public string LogLevel { get; set; }
    [DataMember(Name = "backend")]
    public Backend Backend { get; set; }
    public Services Services { get; }

    public Configuration()
    {
        Prefix = "#";
        LogLevel = "Verbose";
        Backend = Backend.Basic;
        Services = new Services()
        {
            JdoodleClientID = "0",
            JdoodleClientSecret = "0",
            TrashbinUrl = "http://trashbin.140.ted.ge",
            WolframID = "0",
            RapidApiKey = "0",
            BridgeNickname = "cheburator",
            FinnhubKey = "0",
            YoutubeDlPath = "youtube-dl",
            OpenWeatherMapKey = "0",
            YandexWeatherKey = "0",
        };
    }
}
