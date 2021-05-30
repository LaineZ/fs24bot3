using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class YandexTranslate
    {
        public class Root
        {
            public List<string> align { get; set; }
            public int code { get; set; }
            public string lang { get; set; }
            public List<string> text { get; set; }
        }
    }
}
