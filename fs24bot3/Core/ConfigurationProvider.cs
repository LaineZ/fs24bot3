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
        public static Configuration Config = new Configuration();

        public static LoggingLevelSwitch LoggerSw = new LoggingLevelSwitch();

        public static void LoadConfiguration()
        {
            if (File.Exists("settings.toml"))
            {
                Config = Toml.ToModel<Configuration>(File.ReadAllText("settings.toml"));
                Log.Information("Configuration loaded!");
            }
            else
            {
                Log.Warning("Unable to find configuration file, creating new");
                File.WriteAllText("settings.toml", Toml.FromModel(Config));
            }
        }
    }
}
