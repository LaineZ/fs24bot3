using Serilog;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;
using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace fs24bot3
{
    public static class Configuration
    {
        public static string name;
        public static string network;
        public static string channel;
        public static bool reconnect;
        public static long port;
        public static bool ssl;
        public static string nickservPass;

        public static string jdoodleClientID;
        public static string jdoodleClientSecret;
        public static string pastebinKey;
        public static string yandexTrKey;
        public static string vkApiId;
        public static string vkLogin;
        public static string vkPassword;
        public static string trashbinUrl;

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
                        {"name", name},
                        {"network", network},
                        {"channel", channel },
                        {"reconnect", reconnect },
                        {"port", port },
                        {"ssl", ssl },
                        {"nickserv_pass", nickservPass },
                    }
                },

                new TableSyntax("services")
                {
                    Items =
                    {
                        {"jdoodle_client_id", jdoodleClientID},
                        {"jdoodle_client_secret", jdoodleClientSecret},
                        {"pastebin_key", pastebinKey },
                        {"yandex_translate_key", yandexTrKey },
                        {"vk_api_key", vkApiId},
                        {"vk_login", vkLogin },
                        {"vk_password", vkPassword },
                        {"trashbin_url", trashbinUrl },
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
                        {"reconnect", true },
                        {"port", 6667 },
                        {"ssl", true },
                        {"nickserv_pass", "zxcvbnM1" },
                        {"ignore", new string[] { "hubblest", "brote", "ayumi`" } }
                    }
                },

                new TableSyntax("services")
                {
                    Items =
                    {
                        {"jdoodle_client_id", "0"},
                        {"jdoodle_client_secret", "0"},
                        {"pastebin_key", "#cc.ru" },
                        {"yandex_translate_key", "0" },
                        {"vk_api_key", "0"},
                        {"vk_login", "0" },
                        {"vk_password", "0" },
                        {"trashbin_url", "http://127.0.0.1:8000" },
                    }
                }
            }
                };
                File.WriteAllText("settings.toml", doc.ToString());
                LoadConfiguration(); // try again
            }
            else
            {
                Log.Information("Loading configuration from {0}", System.Reflection.Assembly.GetExecutingAssembly().Location);
                string configFile = File.ReadAllText("settings.toml");
                var config = Toml.Parse(configFile);
                var table = config.ToModel();
                name = (string)((TomlTable)table["irc"])["name"];
                network = (string)((TomlTable)table["irc"])["network"];
                channel = (string)((TomlTable)table["irc"])["channel"];
                reconnect = (bool)((TomlTable)table["irc"])["reconnect"];
                port = (long)((TomlTable)table["irc"])["port"];
                ssl = (bool)((TomlTable)table["irc"])["ssl"];
                nickservPass = (string)((TomlTable)table["irc"])["nickserv_pass"];

                jdoodleClientID = (string)((TomlTable)table["services"])["jdoodle_client_id"];
                jdoodleClientSecret = (string)((TomlTable)table["services"])["jdoodle_client_secret"];
                pastebinKey = (string)((TomlTable)table["services"])["pastebin_key"];
                yandexTrKey = (string)((TomlTable)table["services"])["yandex_translate_key"];
                vkApiId = (string)((TomlTable)table["services"])["vk_api_key"];
                vkLogin = (string)((TomlTable)table["services"])["vk_login"];
                vkPassword = (string)((TomlTable)table["services"])["vk_password"];
                trashbinUrl = (string)((TomlTable)table["services"])["trashbin_url"];
                Log.Information("Configuration loaded!");
            }
        }
     }
}
