using fs24bot3.Models;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace fs24bot3.Core
{
    public static class ConfigurationProvider
    {
        public static Configuration Config;

        public static LoggingLevelSwitch LoggerSw = new LoggingLevelSwitch();

        public static void LoadConfiguration()
        {
            if (File.Exists("settings.toml"))
            {
                var loadedconfig = Toml.ToModel<Configuration>(File.ReadAllText("settings.toml"));
                if (loadedconfig != null)
                {
                    Config = loadedconfig;
                    Log.Information("Configuration loaded!");
                }
                else
                {
                    Log.Error("Configuration loading error!");
                }
            }
            else
            {
                Config = new Configuration();
                Log.Warning("Unable to find configuration file, creating new");
                File.WriteAllText("settings.toml", Toml.FromModel(Config));
            }
        }
    }
}
