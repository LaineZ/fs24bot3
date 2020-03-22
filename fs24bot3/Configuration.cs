using Serilog;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

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
        public static bool ignoreSSL;
        public static string nickservPass;

        public static string jdoodleClientID;
        public static string jdoodleClientSecret;
        public static string pastebinKey;
        public static string yandexTrKey;

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
                        {"ignore_ssl", ignoreSSL },
                        {"nickserv_pass", nickservPass }
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
                    }
                }
            }
            };
            File.WriteAllText("settings.toml", doc.ToString());
        }

        public static void LoadConfiguration()
        {
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
                        {"channel", "#cc.ru" },
                        {"reconnect", true },
                        {"port", 6697 },
                        {"ssl", true },
                        {"ignore_ssl", true },
                        {"nickserv_pass", "zxcvbnM1" }
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
                    }
                }
            }
                };
                File.WriteAllText("settings.toml", doc.ToString());
                LoadConfiguration(); // try again
            }
            else
            {
                string configFile = File.ReadAllText("settings.toml");
                var config = Toml.Parse(configFile);
                var table = config.ToModel();

                name = (string)((TomlTable)table["irc"])["name"];
                network = (string)((TomlTable)table["irc"])["network"];
                channel = (string)((TomlTable)table["irc"])["channel"];
                reconnect = (bool)((TomlTable)table["irc"])["reconnect"];
                port = (long)((TomlTable)table["irc"])["port"];
                ssl = (bool)((TomlTable)table["irc"])["ssl"];
                ignoreSSL = (bool)((TomlTable)table["irc"])["ignore_ssl"];
                nickservPass = (string)((TomlTable)table["irc"])["nickserv_pass"];

                jdoodleClientID = (string)((TomlTable)table["services"])["jdoodle_client_id"];
                jdoodleClientSecret = (string)((TomlTable)table["services"])["jdoodle_client_secret"];
                pastebinKey = (string)((TomlTable)table["services"])["pastebin_key"];
                yandexTrKey = (string)((TomlTable)table["services"])["yandex_translate_key"];
                Log.Information("configuration loaded");
            }
        }
     }
}
