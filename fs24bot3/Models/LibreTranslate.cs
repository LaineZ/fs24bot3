namespace fs24bot3.Models
{
    public class LibreTranslate
    {
        public class TranslateQuery
        {
            public string q { get; set; }
            public string source { get; set; }
            public string target { get; set; }
        }

        public class TranslateOut
        {
            public string translatedText { get; set; }
        }
    }
}
