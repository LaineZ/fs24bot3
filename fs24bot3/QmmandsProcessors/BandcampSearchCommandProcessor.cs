using Qmmands;
using System;

namespace fs24bot3.QmmandsProcessors
{
    public class BandcampSearchCommandProcessor
    {
        public sealed class CustomCommandContext : CommandContext
        {
            public string Format = "all";
            public int Location = 0;
            public int Limit = 5;
            public int Page = 1;
            public int Max = 5;
            public string Sort = "pop";


            // Pass your service provider to the base command context.
            public CustomCommandContext(IServiceProvider provider = null) : base(provider)
            {
            }
        }
    }
}
