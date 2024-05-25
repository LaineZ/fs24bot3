using Newtonsoft.Json;
using System.Collections.Generic;

namespace fs24bot3.Models;

public class MetarWeather
{
    public class Cloud
    {
        [JsonProperty("cover")]
        public string Cover { get; set; }

        [JsonProperty("base")]
        public int Base { get; set; }
    }

    public class Root
    {
        [JsonProperty("metar_id")]
        public int MetarId { get; set; }

        [JsonProperty("icaoId")]
        public string IcaoId { get; set; }

        [JsonProperty("receiptTime")]
        public string ReceiptTime { get; set; }

        [JsonProperty("obsTime")]
        public int ObsTime { get; set; }

        [JsonProperty("reportTime")]
        public string ReportTime { get; set; }

        [JsonProperty("temp")]
        public double Temp { get; set; }

        [JsonProperty("dewp")]
        public double Dewp { get; set; }

        [JsonProperty("wdir")]
        public int Wdir { get; set; }

        [JsonProperty("wspd")]
        public int Wspd { get; set; }

        [JsonProperty("wgst")]
        public object Wgst { get; set; }

        [JsonProperty("visib")]
        public string Visib { get; set; }

        [JsonProperty("altim")]
        public double Altim { get; set; }

        [JsonProperty("slp")]
        public double Slp { get; set; }

        [JsonProperty("qcField")]
        public int QcField { get; set; }

        [JsonProperty("wxString")]
        public object WxString { get; set; }

        [JsonProperty("presTend")]
        public object PresTend { get; set; }

        [JsonProperty("maxT")]
        public object MaxT { get; set; }

        [JsonProperty("minT")]
        public object MinT { get; set; }

        [JsonProperty("maxT24")]
        public object MaxT24 { get; set; }

        [JsonProperty("minT24")]
        public object MinT24 { get; set; }

        [JsonProperty("precip")]
        public object Precip { get; set; }

        [JsonProperty("pcp3hr")]
        public object Pcp3hr { get; set; }

        [JsonProperty("pcp6hr")]
        public object Pcp6hr { get; set; }

        [JsonProperty("pcp24hr")]
        public object Pcp24hr { get; set; }

        [JsonProperty("snow")]
        public object Snow { get; set; }

        [JsonProperty("vertVis")]
        public object VertVis { get; set; }

        [JsonProperty("metarType")]
        public string MetarType { get; set; }

        [JsonProperty("rawOb")]
        public string RawOb { get; set; }

        [JsonProperty("mostRecent")]
        public int MostRecent { get; set; }

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }

        [JsonProperty("elev")]
        public int Elev { get; set; }

        [JsonProperty("prior")]
        public int Prior { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("clouds")]
        public List<Cloud> Clouds { get; set; }
    }
}