using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fs24bot3.Models;

public class YandexWeather
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Fact
    {
        [JsonProperty("obs_time")]
        public int ObsTime { get; set; }

        [JsonProperty("temp")]
        public int Temp { get; set; }

        [JsonProperty("feels_like")]
        public int FeelsLike { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonProperty("wind_dir")]
        public string WindDir { get; set; }

        [JsonProperty("pressure_mm")]
        public int PressureMm { get; set; }

        [JsonProperty("pressure_pa")]
        public int PressurePa { get; set; }

        [JsonProperty("humidity")]
        public int Humidity { get; set; }

        [JsonProperty("daytime")]
        public string Daytime { get; set; }

        [JsonProperty("polar")]
        public bool Polar { get; set; }

        [JsonProperty("season")]
        public string Season { get; set; }

        [JsonProperty("wind_gust")]
        public double WindGust { get; set; }
    }

    public class Forecast
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("date_ts")]
        public int DateTs { get; set; }

        [JsonProperty("week")]
        public int Week { get; set; }

        [JsonProperty("sunrise")]
        public string Sunrise { get; set; }

        [JsonProperty("sunset")]
        public string Sunset { get; set; }

        [JsonProperty("moon_code")]
        public int MoonCode { get; set; }

        [JsonProperty("moon_text")]
        public string MoonText { get; set; }

        [JsonProperty("parts")]
        public List<Part> Parts { get; set; }
    }

    public class Info
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }
    }

    public class Part
    {
        [JsonProperty("part_name")]
        public string PartName { get; set; }

        [JsonProperty("temp_min")]
        public int TempMin { get; set; }

        [JsonProperty("temp_avg")]
        public int TempAvg { get; set; }

        [JsonProperty("temp_max")]
        public int TempMax { get; set; }

        [JsonProperty("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonProperty("wind_gust")]
        public double WindGust { get; set; }

        [JsonProperty("wind_dir")]
        public string WindDir { get; set; }

        [JsonProperty("pressure_mm")]
        public int PressureMm { get; set; }

        [JsonProperty("pressure_pa")]
        public int PressurePa { get; set; }

        [JsonProperty("humidity")]
        public int Humidity { get; set; }

        [JsonProperty("prec_mm")]
        public double PrecMm { get; set; }

        [JsonProperty("prec_prob")]
        public double PrecProb { get; set; }

        [JsonProperty("prec_period")]
        public double PrecPeriod { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("feels_like")]
        public int FeelsLike { get; set; }

        [JsonProperty("daytime")]
        public string Daytime { get; set; }

        [JsonProperty("polar")]
        public bool Polar { get; set; }
    }

    public class Root
    {
        [JsonProperty("now")]
        public int Now { get; set; }

        [JsonProperty("now_dt")]
        public DateTime NowDt { get; set; }

        [JsonProperty("info")]
        public Info Info { get; set; }

        [JsonProperty("fact")]
        public Fact FactObj { get; set; }

        [JsonProperty("forecast")]
        public Forecast Forecast { get; set; }
    }
}