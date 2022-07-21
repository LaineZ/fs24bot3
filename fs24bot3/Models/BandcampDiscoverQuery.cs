using System.Collections.Generic;

namespace fs24bot3.Models;

public class BandcampDiscoverQuery
{
    public class Filters
    {
        public string format { get; set; }
        public int location { get; set; }
        public string sort { get; set; }
        public List<string> tags { get; set; }
    }

    public class Root
    {
        public Filters filters { get; set; }
        public int page { get; set; }
    }
}
