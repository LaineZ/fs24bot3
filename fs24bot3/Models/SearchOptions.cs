using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class SearchOptions
    {
        [Option('p', "page", Required = false)]
        public int Page { get; set; }
        [Option("nogarbage", Required = false)]
        public bool NoGarbage { get; set; }
        [Option("rn", Required = false)]
        public int ResultNum { get; set; }
    }
}
