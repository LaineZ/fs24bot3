using System;
using System.Collections.Generic;

using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace fs24bot3.Models
{
    public class BandcampSearch
    {
        public class Genre
        {
        }

        public class Result
        {
            public string stat_params { get; set; }
            public string part { get; set; }
            public int? img_id { get; set; }
            public long? art_id { get; set; }
            public string img { get; set; }
            public string name { get; set; }
            public string url { get; set; }
            public string type { get; set; }
            public bool is_label { get; set; }
            public long id { get; set; }
            public long? band_id { get; set; }
            public string band_name { get; set; }
        }

        public class Auto
        {
            public List<Result> results { get; set; }
            public int time_ms { get; set; }
            public string stat_params_for_tag { get; set; }
        }

        public class Match
        {
            public int score { get; set; }
            public int count { get; set; }
            public string display_name { get; set; }
            public string norm_name { get; set; }
            public long display_tag_id { get; set; }
        }

        public class Tag
        {
            public List<Match> matches { get; set; }
            public int time_ms { get; set; }
            public int count { get; set; }
        }

        public class Root
        {
            public Genre genre { get; set; }
            public Auto auto { get; set; }
            public Tag tag { get; set; }
        }

    }
}
