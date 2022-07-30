using fs24bot3.Models;
using Serilog;
using Serilog.Core;
using System.IO;
using Tomlyn;

namespace fs24bot3.Core;
public static class ConfigurationProvider
{
    public static Configuration Config;
    public static LoggingLevelSwitch LoggerSw = new LoggingLevelSwitch();

    public static void LoadConfiguration()
    {
        if (File.Exists("settings.toml"))
        {
            var loadedconfig = Toml.ToModel<Configuration>(File.ReadAllText("settings.toml"));
            Config = loadedconfig;
            Log.Information("Configuration loaded!");
        }
        else
        {
            Config = new Configuration();
            Log.Warning("Unable to find configuration file, creating new");
            File.WriteAllText("settings.toml", Toml.FromModel(Config));
        }
    }
}
