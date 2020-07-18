using fs24bot3.Models;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fs24bot3
{

    public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {
        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

        [Command("help", "commands")]
        [Description("Список команд")]
        public async void Help()
        {
            Context.SendMessage(Context.Channel, "Генерация спика команд, подождите...");
            var cmds = Service.GetAllCommands();
            string commandsOutput;
            var shop = Shop.ShopItems.Where(x => x.Sellable == true);
            var customCommands = Context.Connection.Table<SQL.CustomUserCommands>().ToList();
            commandsOutput = string.Join('\n', Service.GetAllCommands().Select(x => $"<p style=\"font-family: 'sans-serif';\"><strong>@{x.Name}</strong> {string.Join(' ', x.Parameters)}</p><pre>{x.Description}</pre><p>Требования: {string.Join(' ', x.Checks)}</p><hr>"))
                + "<h3>Магазин:</h3>" +
                string.Join("\n", shop.Select(x => $"<p style=\"font-family: 'sans-serif';\">[{x.Slug}] {x.Name}: Цена: {x.Price}</p>")) +
                "<h3>Кастом команды:</h3>" +
                string.Join("\n", customCommands.Select(x => $"<p>{x.Command}</p>"));

            string link = await http.UploadToTrashbin(commandsOutput);
            Context.SendMessage(Context.Channel, "Выложены команды по этой ссылке: " + link + " также вы можете написать @helpcmd имякоманды для получение дополнительной помощи");
        }

        [Command("helpcmd")]
        [Description("Помощь по команде")]
        public void HelpСmd(string command)
        {
            foreach (Command cmd in Service.GetAllCommands())
            {
                if (cmd.Aliases.Contains(command))
                {
                    Context.SendMessage(Context.Channel, "@" + cmd.Name + " " + string.Join(" ", cmd.Parameters.Select(x => $"[{x.Name} default: {x.DefaultValue}]")) + " - " + cmd.Description);
                    if (cmd.Remarks != null)
                    {
                        Context.SendMultiLineMessage(IrcColors.Bold + cmd.Remarks);
                    }

                    Context.SendMessage(Context.Channel, $"{IrcColors.Bold}Алиасы: {IrcColors.Reset}{String.Join(", ", cmd.Aliases)}");
                    break;
                }
            }
        }

        [Command("stat", "stats")]
        [Description("Статы пользователя или себя")]
        public void Userstat(string nick = null)
        {
            string userNick;
            if (nick != null)
            {
                userNick = nick;
            }
            else
            {
                userNick = Context.Message.From;
            }

            UserOperations usr = new UserOperations(userNick, Context.Connection);

            var data = usr.GetUserInfo();
            if (data != null)
            {
                Context.SendMessage(Context.Channel, $"Статистика: {data.Nick} Уровень: {data.Level} XP: {data.Xp} / {data.Need}");
                try
                {
                    var userTags = usr.GetUserTags();
                    if (userTags.Count > 0)
                    {
                        Context.SendMessage(Context.Channel, "Теги: " + string.Join(' ', userTags.Select(x => $"{x.Color},00⚫{x.TagName}{IrcColors.Reset}")));
                    }
                }
                catch (Core.Exceptions.UserNotFoundException)
                {
                    Context.SendMessage(Context.Channel, "Теги: Нет");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "Пользователя не существует (это как вообще? даже тебя что ли не существует?)");
            }
        }

        [Command("regcmd")]
        [Description("Регистрация команды (Параметр command вводится без @) Документация Lua: https://gist.github.com/LaineZ/67086615e481cb0f5a6c84f8e71103bf")]
        [Remarks("[IsLua = false] Пользовательские команды позволяют добавлять вам собстенные команды которые будут выводить случайный текст с некоторыми шаблонами. Вывод команды можно разнообразить с помощью '||' - данный набор символов разделяют вывод команды, и при вводе пользователем команды будет выводить случайные фразы разделенные '||'\nЗаполнители (placeholders, patterns) - Позволяют динамически изменять вывод команды:\n#USERINPUT - Ввод пользователя после команды\n#USERNAME - Имя пользователя который вызвал команду\n#RNDNICK - рандомный ник в базе данных пользователей\n#RNG - генереатор случайных чисел\n[isLua = true] - Lua движок команд")]
        public void CustomCmdRegister(string command, bool isLua, [Remainder] string output)
        {
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection, Context);
            bool commandIntenral = Service.GetAllCommands().Any(x => x.Aliases.Any(a => a.Equals(command)));

            if (!commandIntenral)
            {
                int isLuaInt = isLua ? 1 : 0;
                var commandInsert = new SQL.CustomUserCommands()
                {
                    Command = "@" + command,
                    Output = output,
                    Nick = Context.Message.From,
                    IsLua = isLuaInt
                };
                try
                {
                    if (usr.RemItemFromInv("money", 8000))
                    {
                        Context.Connection.Insert(commandInsert);
                        Context.SendMessage(Context.Channel, "Команда успешно создана");
                    }
                }
                catch (SQLiteException)
                {
                    usr.AddItemToInv("money", 8000);
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}[ДЕНЬГИ ВОЗВРАЩЕНЫ] Данная команда уже создана! Если вы создали данную команду используйте @editcmd");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Данная команда уже существует в fs24_bot!");
            }
        }

        [Command("regluaurl")]
        [Description("Регистрация команды (Параметр command вводится без @) Документация Lua: https://gist.github.com/LaineZ/67086615e481cb0f5a6c84f8e71103bf")]
        public async void CustomCmdRegisterUrlAsync(string command, string rawurl)
        {
            var response = await http.GetResponseAsync(rawurl);
            
            if (response != null && response.ContentType.Contains("text/plain"))
            {
                Stream responseStream = response.GetResponseStream();
                CustomCmdRegister(command, true, new StreamReader(responseStream).ReadToEnd());
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ =( {response.ContentType}");
            }
        }

        [Command("cmdout")]
        [Description("Редактор строки вывода команды: параметр action: add, del")]
        [Remarks("Параметр action отвечает за действие команды:\nadd - добавить вывод команды при этом параметр value отвечает за строку вывода\ndel - удалить вывод команды, параметр value принимает как числовые значения вывода от 0-n, так и строку вывода которую небоходимо удалить (без ||)")]
        public void CustomCmdEdit(string command, string action, [Remainder] string value)
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection);
            if (query.Any() && query[0].Command == commandConcat && query[0].IsLua == 0)
            {
                if (query[0].Nick == Context.Message.From || usr.GetUserInfo().Admin == 2)
                {
                    switch (action)
                    {
                        case "add":
                            Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output + "||" + value, commandConcat);
                            Context.SendMessage(Context.Channel, IrcColors.Blue + "Команда успешно обновлена!");
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
                                    Context.SendMessage(Context.Channel, IrcColors.Green + "Команда успешно обновлена!");
                                }
                                else
                                {
                                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Максимальное число удаления: " + outputlist.Count);
                                }
                            }
                            catch (FormatException)
                            {
                                if (outputlist.Remove(value))
                                {
                                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), commandConcat);
                                    Context.SendMessage(Context.Channel, IrcColors.Green + "Команда успешно обновлена!");
                                }
                                else
                                {
                                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Такой записи не существует...");
                                }
                            }
                            break;
                        default:
                            Context.SendMessage(Context.Channel, IrcColors.Gray + "Неправильный ввод, введите @helpcmd cmdout");
                            break;
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.From}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, IrcColors.Gray + "Команды не существует или эта команда являктся Lua-командой");
            }
        }


        [Command("cmdown")]
        [Checks.CheckAdmin]
        [Description("Сменить владельца команды")]
        public void CmdOwn(string command, string nick)
        {
            Context.Connection.Execute("UPDATE CustomUserCommands SET Nick = ? WHERE Command = ?", nick, command);
            Context.SendMessage(Context.Channel, IrcColors.Blue + "Команда успешно обновлена!");
        }

        [Command("genname")]
        [Description("Генератор имен")]
        public void GenName(bool isRussian = false, int maxlen = 10, int count = 10)
        {
            if (count <= 20 && maxlen <= 30)
            {
                List<string> names = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    if (!isRussian)
                    {
                        names.Add(Core.MessageUtils.GenerateName(maxlen));
                    }
                    else
                    {
                        names.Add(Core.MessageUtils.GenerateNameRus(maxlen));
                    }
                }
                Context.SendMessage(Context.Channel, string.Join(",", names));
            }
            else
            {
                Context.SendMessage(Context.Channel, IrcColors.Red + "ПРЕВЫШЕН ЛИМИТ!");
            }
        }

        [Command("midi")]
        [Description("Миди ноты")]
        public void Midi(string note, int oct = 4)
        {
            string[] noteString = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            if (uint.TryParse(note, out uint initialNote))
            {
                int octave = (int)(initialNote / 12) - 1;
                uint noteIndex = initialNote % 12;
                string noteName = noteString[noteIndex];
                Context.SendMessage(Context.Channel, $"MIDI: {note} = {IrcColors.Reset}{noteName}{octave}");
            }
            else
            {
                for (int i = 0; i < noteString.Length; i++)
                {
                    if (noteString[i].ToLower() == note.ToLower())
                    {
                        int noteIndex = (12 * (oct + 1)) + i;
                        Context.SendMessage(Context.Channel, $"{note}{oct} = MIDI: {IrcColors.Reset}{noteIndex}");
                        break;
                    }
                }
            }
        }

        [Command("cmdinfo")]
        [Description("Информация о команде")]
        public void CmdInfo(string command)
        {
            // a small workaround for this exception An exception occurred while executing cmdinfo.: `Cannot get SQL for: Add`
            command = "@" + command;
            var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).ToList();
            if (query.Count > 0)
            {
                Context.SendMessage(Context.Channel, IrcColors.Blue + $"Команда {query[0].Command} Создал: `{query[0].Nick}` Размер вывода: {query[0].Output.Length} символов, строк - {query[0].Output.Split("||").Length} Lua: {query[0].IsLua}");
                if (query[0].Nick.Length <= 0)
                {
                    Context.SendMessage(Context.Channel, IrcColors.Yellow + "Внимание: данная команда была создана в старой версии fs24bot, пожалуйста используйте @cmdown чтобы изменить владельца команды!");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, "Команды не существует");
            }
        }

        [Command("cmdrep")]
        [Description("Заменитель строки вывода команды (используете кавычки если замена с пробелом)")]
        [Remarks("Если параметр newstr не заполнен - происходит просто удаление oldstr из команды")]
        public void CustomCmdRepl(string command, string oldstr, string newstr = "")
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection);
            if (query.Any() && query[0].Command == commandConcat || usr.GetUserInfo().Admin == 2)
            {
                if (query[0].Nick == Context.Message.From || usr.GetUserInfo().Admin == 2)
                {
                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output.Replace(oldstr, newstr), commandConcat);
                    Context.SendMessage(Context.Channel, IrcColors.Blue + "Команда успешно обновлена!");
                }
                else
                {
                    Context.SendMessage(Context.Channel, IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.From}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, IrcColors.Gray + "Команды не существует");
            }
        }

        [Command("cmdupd")]
        [Description("Полное обновление вывода команды")]
        public void LuaUpdCoommand(string command, [Remainder] string newstr)
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection);
            if (query.Any() && query[0].IsLua == 1 && query[0].Command == commandConcat || usr.GetUserInfo().Admin == 2)
            {
                if (query[0].Nick == Context.Message.From || usr.GetUserInfo().Admin == 2)
                {
                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", newstr, commandConcat);
                    Context.SendMessage(Context.Channel, IrcColors.Blue + "Команда успешно обновлена!");
                }
                else
                {
                    Context.SendMessage(Context.Channel, IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.From}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, IrcColors.Gray + "Команды не существует");
            }
        }

        [Command("delcmd")]
        [Description("Удалить команду")]
        public void CustomCmdRem(string command)
        {
            var commandConcat = "@" + command;
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection);
            if (usr.GetUserInfo().Admin == 2)
            {
                var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).Delete();
                if (query > 0)
                {
                    Context.SendMessage(Context.Channel, "Команда удалена!");
                }
            }
            else
            {
                var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat) && v.Nick.Equals(Context.Message.From)).Delete();
                if (query > 0)
                {
                    Context.SendMessage(Context.Channel, "Команда удалена!");
                }
                else
                {
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Этого не произошло....");
                }
            }
        }

        [Command("tag")]
        [Description("Управление тегами: параметр action: add/del")]
        [Remarks("Параметр action отвечает за действие команды:\nadd - добавить тег\ndel - удалить тег. Параметр ircolor представляет собой код IRC цвета, его можно узнать например с помощью команды .colors (brote@irc.esper.net)")]
        public void AddTag(string action, string tagname, int ircolor = 1)
        {
            var user = new UserOperations(Context.Message.From, Context.Connection);

            switch (action)
            {
                case "add":
                    if (user.RemItemFromInv("money", 1000))
                    {
                        var tag = new SQL.Tag()
                        {
                            TagName = tagname,
                            Color = ircolor.ToString(),
                            TagCount = 0,
                            Username = Context.Message.From
                        };

                        Context.Connection.Insert(tag);

                        Context.SendMessage(Context.Channel, $"Тег 00,{ircolor}⚫{tagname}{IrcColors.Reset} успешно добавлен!");
                    }
                    else
                    {
                        Log.Information("Error occurred while adding!");
                    }
                    break;
                case "del":
                    var tagDel = new Core.TagsUtils(tagname, Context.Connection);
                    if (tagDel.GetTagByName().Username == Context.Message.From)
                    {
                        Context.Connection.Execute("DELETE FROM Tag WHERE TagName = ?", tagname);
                        Context.SendMessage(Context.Channel, "Тег " + tagname + " успешно удален!");
                    }
                    else
                    {
                        Context.SendMessage(Context.Channel, IrcColors.Gray + $"Тег создал {tagDel.GetTagByName().Username} а не {Context.Message.From}");
                    }
                    break;
                default:
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Неправильный ввод, введите @helpcmd addtag");
                    break;
            }
        }

        [Command("addtag")]
        [Description("Добавить тег пользователю")]
        public void InsertTag(string tagname, string destination)
        {
            var user = new UserOperations(destination, Context.Connection);

            if (user.AddTag(tagname, 1))
            {
                Context.SendMessage(Context.Channel, $"Тег {tagname} добавлен пользователю {destination}");
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ :(");
            }
        }

        [Command("tags")]
        [Description("Список всех тегов")]
        public void AllTags()
        {
            List<SQL.Tag> tags = new List<SQL.Tag>();
            var query = Context.Connection.Table<SQL.Tag>();
            foreach (var tag in query)
            {
                tags.Add(tag);
            }
            Context.SendMessage(Context.Channel, string.Join(' ', tags.Select(x => $"{x.Color},00⚫{x.TagName}{IrcColors.Reset}")));
        }

        [Command("mishareturn", "blocksuntil", "misha")]
        [Description("КОГДА ОМСК БУДЕТ СНОВА ЗАБЛОКИРОВАН?")]
        public void MishaReturn()
        {

            DateTime dateOut = new DateTime(2020, 12, 22, 17, 26, 12);
            TimeSpan dateIn =  dateOut.Subtract(DateTime.Now);
            Context.SendMessage(Context.Channel, $"Дата до появления Миши : {dateIn.Days / 30} месяцев {dateIn.Days % 30} дней {dateIn.Hours} часов {dateIn.Minutes} минут {dateIn.Seconds} секунд {dateIn.Milliseconds} мс...");
        }
    }
}
