using IrcClientCore;
using Newtonsoft.Json;
using Qmmands;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Data.SQLite;

namespace fs24bot3
{

    public sealed class CustomCommandContext : CommandContext
    {
        public Message Message { get; }
        public Irc Socket { get; }

        public string Channel => Message.Channel;
        public SQLiteConnection Connection;

        // Pass your service provider to the base command context.
        public CustomCommandContext(Message message, Irc socket, SQLiteConnection connection, IServiceProvider provider = null) : base(provider)
        {
            Message = message;
            Socket = socket;
            Connection = connection;
        }
    }

    public sealed class CommandModule : ModuleBase<CustomCommandContext>
    {
        // Dependency Injection via the constructor or public settable properties.
        // CommandService and IServiceProvider self-inject into modules,
        // properties and other types are requested from the provided IServiceProvider
        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        // Invoked with:   !help
        // Responds with:  `help` - Lists available commands.
        //                 `sum` - Sums two given numbers.
        //                 `echo` - Echoes given text.
        [Command("help", "commands")]
        [Qmmands.Description("Список команд")]
        public async void Help()
        {
            Context.Socket.SendMessage(Context.Channel, "Генерация спика команд, подождите...");
            var cmds = Service.GetAllCommands();
            var commandsOutput = "";
            commandsOutput = string.Join('\n', Service.GetAllCommands().Select(x => $"`{x.Name}` - {x.Description}"));
            try
            {
                string link = await http.UploadToPastebin(commandsOutput);
                Context.Socket.SendMessage(Context.Channel, "Выложены команды по этой ссылке: " + link);
            }
            catch (NullReferenceException)
            {
                Context.Socket.SendMessage(Context.Channel, "Да блин чё такое link снова null!");
            }
        }

        [Command("ms", "search")]
        [Qmmands.Description("Поиск@Mail.ru")]
        public async void MailSearch([Remainder] string query)
        {
            string response = await http.MakeRequestAsync("https://go.mail.ru/search?q=" + query);

            string startString = "go.dataJson = {";
            string stopString = "};";

            string searchDataTemp = response.Substring(response.IndexOf(startString) + startString.Length - 1);
            string searchData = searchDataTemp.Substring(0, searchDataTemp.IndexOf(stopString) + 1);

            Models.MailSearch.RootObject items = JsonConvert.DeserializeObject<Models.MailSearch.RootObject>(searchData);

            StringBuilder searchResult = new StringBuilder(items.serp.results[0].title);
            searchResult.Replace("<b>", Models.IrcColors.Bold);
            searchResult.Replace("</b>", Models.IrcColors.Reset);
            string url = items.serp.results[0].url;

            Context.Socket.SendMessage(Context.Channel, searchResult.ToString() + Models.IrcColors.Green + " // " + url);
        }

        [Command("execute", "exec")]
        [Qmmands.Description("REPL")]
        public async void ExecuteAPI(string lang, [Remainder] string code)
        {
            HttpClient client = new HttpClient();

            Models.APIExec.Input codeData = new Models.APIExec.Input();

            codeData.clientId = Configuration.jdoodleClientID;
            codeData.clientSecret = Configuration.jdoodleClientSecret;
            codeData.language = lang;
            codeData.script = code;

            HttpContent c = new StringContent(JsonConvert.SerializeObject(codeData), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.jdoodle.com/v1/execute", c);
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonOutput = JsonConvert.DeserializeObject<Models.APIExec.Output>(responseString);

            int count = 0;

            if (jsonOutput.output != null)
            {
                Context.Socket.SendMessage(Context.Channel, "CPU: " + jsonOutput.cpuTime + " Mem: " + jsonOutput.memory);
                foreach (string outputstr in jsonOutput.output.Split("\n"))
                {
                    Context.Socket.SendMessage(Context.Channel, outputstr);
                    count++;
                    if (count > 4)
                    {
                        string link = await http.UploadToPastebin(jsonOutput.output);
                        Context.Socket.SendMessage(Context.Channel, "Полный вывод здесь: " + link);
                        break;
                    }
                }
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Сервер вернул: " + responseString);
            }
        }

        [Command("version")]
        [Qmmands.Description("Версия проги")]
        public void Version()
        {
            var os = Environment.OSVersion;
            Context.Socket.SendMessage(Context.Channel, String.Format("NET: {0} Система: {1} Версия: {2} Версия системы: {3}",
                Environment.Version.ToString(), os.Platform, os.VersionString, os.Version));
        }

        [Command("gc")]
        [Qmmands.Description("Вывоз мусора")]
        public void CollectGarbage()
        {
            GC.Collect();
            Context.Socket.SendMessage(Context.Channel, "Мусор вывезли!");
        }

        [Command("stat")]
        [Qmmands.Description("Статы пользователя или себя")]
        public void Userstat(string nick = null)
        {
            string userNick;
            if (nick != null)
            {
                userNick = nick;
            }
            else
            {
                userNick = Context.Message.User;
            }

            var cmd = new SQLiteCommand(Context.Connection);
            cmd.CommandText = "SELECT xp, level, need FROM nicks WHERE username = @username";
            cmd.Parameters.AddWithValue("@username", userNick);
            cmd.Prepare();
            cmd.ExecuteNonQuery();

            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                Context.Socket.SendMessage(Context.Channel, "Статистика: " + userNick + " Уровень: " + rdr["level"] + " XP: " + rdr["xp"] + "/" + rdr["need"]);
            }
        }
    }
}
