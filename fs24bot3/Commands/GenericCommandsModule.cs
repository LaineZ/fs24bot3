﻿using IrcClientCore;
using Newtonsoft.Json;
using Qmmands;
using Qmmands.Delegates;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace fs24bot3
{

    public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {
        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        [Command("help", "commands")]
        [Qmmands.Description("Список команд")]
        public async void Help()
        {
            Context.Socket.SendMessage(Context.Channel, "Генерация спика команд, подождите...");
            var cmds = Service.GetAllCommands();
            string commandsOutput;
            var shop = Shop.ShopItems.Where(x => x.Sellable == true);
            commandsOutput = string.Join('\n', Service.GetAllCommands().Select(x => $"`{x.Name}` - {x.Description}")) + "\nМагазин:\n" + string.Join("\n", shop.Select(x => $"[{x.Slug}] {x.Name}: Цена: {x.Price}"));
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

        [Command("helpcmd")]
        [Qmmands.Description("Помощь по команде")]
        public void HelpСmd(string command)
        {
            foreach (var cmd in Service.GetAllCommands())
            {
                if (cmd.Name == command)
                {
                    Context.Socket.SendMessage(Context.Channel,
                        cmd.Module.Name + ".cs : @" + cmd.Name + " " + string.Join(" ", cmd.Parameters) + " - " + cmd.Description);
                    break;
                }
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

            Log.Information("@MS: Antirobot-blocked?: {0}", items.antirobot.blocked);

            foreach (var item in items.serp.results)
            {
                if (!item.is_porno)
                {
                    StringBuilder searchResult = new StringBuilder(item.title);
                    searchResult.Replace("<b>", Models.IrcColors.Bold);
                    searchResult.Replace("</b>", Models.IrcColors.Reset);

                    StringBuilder descResult = new StringBuilder(item.passage);
                    descResult.Replace("<b>", Models.IrcColors.Bold);
                    descResult.Replace("</b>", Models.IrcColors.Reset);


                    string url = item.url;

                    Context.Socket.SendMessage(Context.Channel, searchResult.ToString() + Models.IrcColors.Green + " // " + url);
                    Context.Socket.SendMessage(Context.Channel, descResult.ToString());
                    break;
                }
                else
                {
                    continue;
                }
            }
        }

        [Command("execute", "exec")]
        [Qmmands.Description("REPL поддерживает полно языков, lua, php, nodejs, python3, python2, cpp, c, lisp ... и многие другие")]
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

        [Command("stat", "stats")]
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

            UserOperations usr = new UserOperations(userNick, Context.Connection);

            var data = usr.GetUserInfo();
            if (data != null)
            {
                Context.Socket.SendMessage(Context.Channel, "Статистика: " + data.Nick + " Уровень: " + data.Level + " XP: " + data.Xp + "/" + data.Need);
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Пользователя не существует");
            }
        }

        [Command("lyrics", "lyr")]
        [Qmmands.Description("Текст песни")]
        public void Lyrics(string providername, string artist, [Remainder] string title)
        {
            
        }


        [Command("tr", "translate")]
        [Qmmands.Description("Переводчик")]
        public async void Translate(string lang, [Remainder] string text)
        {
            HttpClient client = new HttpClient();


            var formVariables = new List<KeyValuePair<string, string>>();
            formVariables.Add(new KeyValuePair<string, string>("text", text));
            var formContent = new FormUrlEncodedContent(formVariables);

            var response = await client.PostAsync("https://translate.yandex.net/api/v1.5/tr.json/translate?lang=" + lang + "&key=" + Configuration.yandexTrKey, formContent);
            var responseString = await response.Content.ReadAsStringAsync();

            Log.Verbose(responseString);

            var translatedOutput = JsonConvert.DeserializeObject<Models.YandexTranslate.RootObject>(responseString);

            if (translatedOutput.text != null)
            {
                Context.Socket.SendMessage(Context.Channel, translatedOutput.text[0] + "(translate.yandex.ru) " + translatedOutput.lang);
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Сервер вернул: " + responseString);
            }
        }
    }
}   
