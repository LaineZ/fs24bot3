using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fs24bot3.QmmandsProcessors
{
    public class SearchCommandProcessor
    {
        public sealed class CustomCommandContext : CommandContext
        {
            public List<ResultGeneric> SearchResults = new List<ResultGeneric>();
            public string Site = string.Empty;
            public bool Random = true;
            public int Page = 0;
            public int Limit = 1;
            public int Max = 10;
            public bool PreProcess = false;

            // Pass your service provider to the base command context.
            public CustomCommandContext(IServiceProvider provider = null) : base(provider)
            {
            }
        }
    }
}
