using System.Collections.Generic;

namespace fs24bot3.Models
{
    public class PastebinUpload
    {
        public class Section
        {
            public string name { get; set; }
            public string syntax { get; set; }
            public string contents { get; set; }
        }

        public class RootObject
        {
            public string description { get; set; }
            public List<Section> sections { get; set; }
        }

        public class Output
        {
            public string id { get; set; }
            public string link { get; set; }
        }
    }
}
