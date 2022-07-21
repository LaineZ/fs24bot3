using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models;

public class BingTranlate
{
    public class DetectedLanguage
    {
        public string language { get; set; }
        public double score { get; set; }
    }

    public class Translation
    {
        public string text { get; set; }
        public string to { get; set; }
    }

    public class Request
    {
        public string Text { get; set; }
    }

    public class Root
    {
        public DetectedLanguage detectedLanguage { get; set; }
        public List<Translation> translations { get; set; }
    }
}
