using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class BingTranlate
    {
        public class Translation
        {
            public string text { get; set; }
            public string to { get; set; }
        }

        public class Root
        {
            public List<Translation> translations { get; set; }
        }
    }
}
