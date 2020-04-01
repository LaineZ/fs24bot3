using HtmlAgilityPack;
using IrcClientCore;
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
            commandsOutput = string.Join('\n', Service.GetAllCommands().Select(x => $"{string.Join(' ', x.Checks)} @{x.Name} {string.Join(' ', x.Parameters)} - {x.Description}")) + "\nМагазин:\n" + string.Join("\n", shop.Select(x => $"[{x.Slug}] {x.Name}: Цена: {x.Price}"));
            try
            {
                string link = await http.UploadToPastebin(commandsOutput);
                Context.Socket.SendMessage(Context.Channel, "Выложены команды по этой ссылке: " + link + " также вы можете написать @helpcmd имякоманды для получение дополнительной помощи");
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
                    if (cmd.Remarks != null)
                    {
                        foreach (string help in cmd.Remarks.Split("\n"))
                        {
                            Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + help);
                        }
                    }
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


            if (jsonOutput.output != null)
            {
                Context.Socket.SendMessage(Context.Channel, "CPU: " + jsonOutput.cpuTime + " Mem: " + jsonOutput.memory);
                Context.SendMultiLineMessage(jsonOutput.output);
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
        public async void Lyrics([Remainder] string song)
        {
            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1]);

                    Context.SendMultiLineMessage(await lyrics.GetLyrics());
                }
                catch (Exception e)
                {
                    Context.SendMultiLineMessage("Ошибка при получении слов: " + e.Message);
                }
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Instumental");
            }
        }


        [Command("tr", "translate")]
        [Qmmands.Description("Переводчик")]
        [Qmmands.Remarks("Параметр lang нужно вводить в формате 'sourcelang-translatelang' или 'traslatelang' в данном случае переводчик попытается догадаться с какого языка пытаются перевести\nВсе языки вводятся по стандарту ISO-639-1 посмотреть можно здесь: https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%B4%D1%8B_%D1%8F%D0%B7%D1%8B%D0%BA%D0%BE%D0%B2")]
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

        [Command("regcmd")]
        [Qmmands.Description("Регистрация команды (Параметр command вводится без @)")]
        [Qmmands.Remarks("Пользовательские команды позволяют добавлять вам собстенные команды которые будут выводить случайный текст с некоторыми шаблонами. Вывод команды можно разнообразить с помощью '||' - данный набор символов разделяют вывод команды, и при вводе пользователем команды будет выводить случайные фразы разделенные '||'\nЗаполнители (placeholders, patterns) - Позволяют динамически изменять вывод команды:\n#USERINPUT - Ввод пользователя после команды\n#USERNAME - Имя пользователя который вызвал команду")]
        public void CustomCmdRegister(string command, [Remainder] string output)
        {
            var commandIntenral = Service.GetAllCommands().Where(x => x.Name.Equals(command));
            if (!commandIntenral.Any())
            {
                var commandInsert = new Models.SQL.CustomUserCommands()
                {
                    Command = "@" + command,
                    Output = output,
                    Nick = Context.Message.User,
                };
                try
                {
                    Context.Connection.Insert(commandInsert);
                }
                catch (SQLiteException)
                {
                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Данная команда уже создана! Если вы создали данную команду используйте @editcmd");
                }
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Данная команда уже суещствует в fs24_bot");
            }
        }

        [Command("cmdout")]
        [Qmmands.Description("Редактор строки вывода команды: параметр action: add, del")]
        [Qmmands.Remarks("Параметр action отвечает за действие команды:\nadd - добавить вывод команды при этом параметр value отвечает за строку вывода\ndel - удалить вывод команды, параметр value принимает как числовые значения вывода от 0-n, так и строку вывода которую небоходимо удалить (без ||)")]
        public void CustomCmdEdit(string command, string action, [Remainder] string value)
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.User, Context.Connection);
            if (query.Any() && query[0].Command == commandConcat || usr.GetUserInfo().Admin == 2)
            {
                if (query[0].Nick == Context.Message.User)
                {
                    switch (action)
                    {
                        case "add":
                            Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output + "||" + value, commandConcat);
                            Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Blue + "Команда успешно обновлена!");
                            break;
                        case "del":
                            var outputlist = query[0].Output.Split("||").ToList();
                            try
                            {
                                int val = int.Parse(value);
                                if (val < outputlist.Count && val >= 0)
                                {
                                    outputlist.RemoveAt(val);
                                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), commandConcat);
                                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Green + "Команда успешно обновлена!");
                                }
                                else
                                {
                                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Максимальное число удаления: " + outputlist.Count);
                                }
                            }
                            catch (FormatException)
                            {
                                if (outputlist.Remove(value))
                                {
                                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), commandConcat);
                                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Green + "Команда успешно обновлена!");
                                }
                                else
                                {
                                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Такой записи не существует...");
                                }
                            }
                            break;
                        default:
                            Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Неправильный ввод, введите @helpcmd editout");
                            break;
                    }
                }
                else
                {
                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.User}");
                }
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Команды не существует");
            }
        }

        [Command("cmdrep")]
        [Qmmands.Description("Заменитель строки вывода команды (используете кавычки если замена с пробелом)")]
        [Qmmands.Remarks("Если параметр newstr не заполнен - происходит просто удаление oldstr из команды")]
        public void CustomCmdRepl(string command, string oldstr, string newstr = "")
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.User, Context.Connection);
            if (query.Any() && query[0].Command == commandConcat || usr.GetUserInfo().Admin == 2)
            {
                if (query[0].Nick == Context.Message.User)
                {
                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output.Replace(oldstr, newstr), commandConcat);
                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Blue + "Команда успешно обновлена!");
                }
                else
                {
                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.User}");
                }
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Команды не существует");
            }
        }

        [Command("delcmd")]
        [Qmmands.Description("Удалить команду")]
        public void CustomCmdRem(string command)
        {
            var commandConcat = "@" + command;
            UserOperations usr = new UserOperations(Context.Message.User, Context.Connection);
            if (usr.GetUserInfo().Admin == 2)
            {
                var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).Delete();
                if (query > 0)
                {
                    Context.Socket.SendMessage(Context.Channel, "Команда удалена!");
                }
            }
            else
            {
                var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat) && v.Nick.Equals(Context.Message.User)).Delete();
                if (query > 0)
                {
                    Context.Socket.SendMessage(Context.Channel, "Команда удалена!");
                }
                else
                {
                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Этого не произошло....");
                }
            }
        }

        [Command("tag")]
        [Qmmands.Description("Управление тегами: параметр action: add/del")]
        [Qmmands.Remarks("Параметр action отвечает за действие команды:\nadd - добавить тег\ndel - удалить тег. Параметр ircolor представляет собой код IRC цвета, его можно узнать например с помощью команды .colors (brote@irc.esper.net)")]
        public void AddTag(string action, string tagname, int ircolor = 0)
        {
            var user = new UserOperations(Context.Message.User, Context.Connection, Context.Socket, Context.Message);

            switch (action)
            {
                case "add":
                    if (user.RemItemFromInv("money", 1000))
                    {
                        var tag = new Models.SQL.Tag()
                        {
                            TagName = tagname,
                            Color = "" + ircolor,
                            TagCount = 0,
                            Username = Context.Message.User
                        };

                        Context.Connection.Insert(tag);

                        Context.Socket.SendMessage(Context.Channel, $"Тег \x0300,{ircolor}⚫{tagname}{Models.IrcColors.Reset} успешно добавлен!");
                    }
                    else
                    {
                        Log.Information("Error occurred while adding!");
                    }
                    break;
                case "del":
                    var tagDel = new Core.TagsUtils(tagname, Context.Connection);
                    if (tagDel.GetTagByName().Username == Context.Message.User)
                    {
                        Context.Connection.Execute("DELETE Tag WHERE TagName = ?", tagname);
                        Context.Socket.SendMessage(Context.Channel, "Тег " + tagname + " успешно удален!");
                    }
                    else
                    {
                        Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + $"Тег создал {tagDel.GetTagByName().Username} а не {Context.Message.User}");
                    }
                    break;
                default:
                    Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Gray + "Неправильный ввод, введите @helpcmd addtag");
                    break;
            }
        }
        [Command("addtag")]
        [Qmmands.Description("Добавить тег пользователю")]
        public void InsertTag(string tagname, string destination)
        {
            var user = new UserOperations(destination, Context.Connection);

            if (user.AddTag(tagname, 1))
            {
                Context.Socket.SendMessage(Context.Channel, $"Тег {tagname} добавлен пользователю {destination}");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ :(");
            }
        }
    }
}   
