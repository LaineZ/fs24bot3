using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace fs24bot3
{
    public class Configuration
    {
        public static string Name { get; private set; }
        public static string Network { get; private set; }
        public static string Channel { get; private set; }
        public static bool Reconnect { get; private set; } 
        public static long Port { get; private set; }
        public static string NickservPass { get; private set; }
        public static string ServerPassword { get; private set; }

        public static string JdoodleClientID { get; private set; }
        public static string JdoodleClientSecret { get; private set; }
        public static string TrashbinUrl { get; private set; }
        public static string WolframID { get; private set; }
        public static string TranslateKey { get; private set; }
        

        public static LoggingLevelSwitch LoggerSw = new LoggingLevelSwitch();

        public static void SaveConfiguration()
        {
            var doc = new DocumentSyntax()
            {
                Tables =
            {
                new TableSyntax("irc")
                {
                    Items =
                    {
                        {"name", Name},
                        {"network", Network},
                        {"channel", Channel },
                        {"port", Port },
                        {"nickserv_pass", NickservPass },
                        {"server_pass", ServerPassword},
                    }
                },

                new TableSyntax("services")
                {
                    Items =
                    {
                        {"jdoodle_client_id", JdoodleClientID},
                        {"jdoodle_client_secret", JdoodleClientSecret},
                        {"trashbin_url", TrashbinUrl },
                        {"wolfram_id", WolframID },
                        {"translate_key", TranslateKey },
                    }
                }
            }
            };
            File.WriteAllText("settings.toml", doc.ToString());
        }

        public static void LoadConfiguration()
        {
            LoggerSw.MinimumLevel = LogEventLevel.Verbose;
            if (!File.Exists("settings.toml"))
            {
                Log.Warning("unable to load configuraion file: creating new");
                var doc = new DocumentSyntax()
                {
                    Tables =
            {
                new TableSyntax("irc")
                {
                    Items =
                    {
                        {"name", "djmadest123"},
                        {"network", "irc.esper.net"},
                        {"channel", "#fl-studio" },
                        {"port", 6667 },
                        {"nickserv_pass", "zxcvbnM1" },
                        {"server_pass", "" },
                    }
                },

                new TableSyntax("services")
                {
                    Items =
                    {
                        {"jdoodle_client_id", "0"},
                        {"jdoodle_client_secret", "0"},
                        {"trashbin_url", "http://127.0.0.1:8000" },
                        {"wolfram_id", "0" },
                        {"translate_key", "0" },
                    }
                }
            }
                };
                File.WriteAllText("settings.toml", doc.ToString());
                LoadConfiguration(); // try again
            }
            else
            {
                Log.Information("Loading configuration from {0}", Directory.GetCurrentDirectory());
                string configFile = File.ReadAllText("settings.toml");
                var config = Toml.Parse(configFile);
                var table = config.ToModel();
                try
                {
                    Name = (string)((TomlTable)table["irc"])["name"];
                    Network = (string)((TomlTable)table["irc"])["network"];
                    Channel = (string)((TomlTable)table["irc"])["channel"];
                    Port = (long)((TomlTable)table["irc"])["port"];
                    NickservPass = (string)((TomlTable)table["irc"])["nickserv_pass"];
                    ServerPassword = (string)((TomlTable)table["irc"])["server_pass"];

                    JdoodleClientID = (string)((TomlTable)table["services"])["jdoodle_client_id"];
                    JdoodleClientSecret = (string)((TomlTable)table["services"])["jdoodle_client_secret"];
                    TrashbinUrl = (string)((TomlTable)table["services"])["trashbin_url"];
                    WolframID = (string)((TomlTable)table["services"])["wolfram_id"];
                    TranslateKey = (string)((TomlTable)table["services"])["translate_key"];
                }
                catch (KeyNotFoundException e)
                {
                    Log.Error("Cannot load key: {0}", e.Message);
                }
                Log.Information("Configuration loaded!");
            }
        }
     }
}
