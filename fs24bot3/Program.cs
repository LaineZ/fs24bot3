using fs24bot3.Core;
using NetIRC;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using fs24bot3.Backend;
using fs24bot3.Helpers;
using fs24bot3.Models;

namespace fs24bot3;

internal static class Program
{
    private static IMessagingClient Client;
    private static void Main()
    {
        Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
#if DEBUG
        .MinimumLevel.Verbose()
#else
        .MinimumLevel.Information()
#endif
        .CreateLogger();

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Console.OutputEncoding = Encoding.Unicode;
        }
        
        Log.Information("fs24_bot 3 by 140bpmdubstep");
        ConfigurationProvider.LoadConfiguration();
        Log.Information("Running with: {0} backend", ConfigurationProvider.Config.Backend);
        Client = ConfigurationProvider.Config.Backend switch
        {
            Models.Backend.Basic => new Basic(),
            Models.Backend.IRC => new Irc(),
            Models.Backend.Discord => new Discord(),
            _ => throw new UnauthorizedAccessException()
        };
        Client.Process();
    }
}
