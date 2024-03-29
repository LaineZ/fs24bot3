﻿using System.Collections.Generic;

namespace fs24bot3.Models;

public class SymbolLookup
{
    public class Result
    {
        public string description { get; set; }
        public string displaySymbol { get; set; }
        public string symbol { get; set; }
        public string type { get; set; }
    }

    public class Root
    {
        public int count { get; set; }
        public List<Result> result { get; set; }
    }
}
