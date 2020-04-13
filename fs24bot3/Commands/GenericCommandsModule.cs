using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
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
            commandsOutput = string.Join('\n', Service.GetAllCommands().Select(x => $"{string.Join(' ', x.Checks)} @{x.Name} {string.Join(' ', x.Parameters)} - {x.Description}")) + "\nМагазин:\n" + string.Join("\n", shop.Select(x => $"[{x.Slug}] {x.Name}: Цена: {x.Price}"));
            try
            {
                string link = await http.UploadToPastebin(commandsOutput);
                Context.SendMessage(Context.Channel, "Выложены команды по этой ссылке: " + link + " также вы можете написать @helpcmd имякоманды для получение дополнительной помощи");
            }
            catch (NullReferenceException)
            {
                Context.SendMessage(Context.Channel, "Да блин чё такое link снова null!");
            }
        }

        [Command("helpcmd")]
        [Description("Помощь по команде")]
        public void HelpСmd(string command)
        {
            foreach (var cmd in Service.GetAllCommands())
            {
                if (cmd.Name == command)
                {
                    Context.SendMessage(Context.Channel,
                        cmd.Module.Name + ".cs : @" + cmd.Name + " " + string.Join(" ", cmd.Parameters) + " - " + cmd.Description);
                    if (cmd.Remarks != null)
                    {
                        foreach (string help in cmd.Remarks.Split("\n"))
                        {
                            Context.SendMessage(Context.Channel, Models.IrcColors.Gray + help);
                        }
                    }
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
                Context.SendMessage(Context.Channel, "Статистика: " + data.Nick + " Уровень: " + data.Level + " XP: " + data.Xp + "/" + data.Need);
                try
                {
                    var userTags = usr.GetUserTags();
                    if (userTags.Count > 0)
                    {
                        Context.SendMessage(Context.Channel, "Теги: " + string.Join(' ', userTags.Select(x => $"{x.Color},00⚫{x.TagName}{Models.IrcColors.Reset}")));
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
        [Description("Регистрация команды (Параметр command вводится без @)")]
        [Remarks("Пользовательские команды позволяют добавлять вам собстенные команды которые будут выводить случайный текст с некоторыми шаблонами. Вывод команды можно разнообразить с помощью '||' - данный набор символов разделяют вывод команды, и при вводе пользователем команды будет выводить случайные фразы разделенные '||'\nЗаполнители (placeholders, patterns) - Позволяют динамически изменять вывод команды:\n#USERINPUT - Ввод пользователя после команды\n#USERNAME - Имя пользователя который вызвал команду")]
        public void CustomCmdRegister(string command, [Remainder] string output)
        {
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection, Context);
            if (usr.RemItemFromInv("money", 10000))
            {
                var commandIntenral = Service.GetAllCommands().Where(x => x.Name.Equals(command));
                if (!commandIntenral.Any())
                {
                    var commandInsert = new Models.SQL.CustomUserCommands()
                    {
                        Command = "@" + command,
                        Output = output,
                        Nick = Context.Message.From,
                    };
                    try
                    {
                        Context.Connection.Insert(commandInsert);
                        Context.SendMessage(Context.Channel, "Команда успешно создана");
                    }
                    catch (SQLiteException)
                    {
                        Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Данная команда уже создана! Если вы создали данную команду используйте @editcmd");
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Данная команда уже суещствует в fs24_bot");
                }
            }
        }

        [Command("cmdout")]
        [Description("Редактор строки вывода команды: параметр action: add, del")]
        [Remarks("Параметр action отвечает за действие команды:\nadd - добавить вывод команды при этом параметр value отвечает за строку вывода\ndel - удалить вывод команды, параметр value принимает как числовые значения вывода от 0-n, так и строку вывода которую небоходимо удалить (без ||)")]
        public void CustomCmdEdit(string command, string action, [Remainder] string value)
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection);
            if (query.Any() && query[0].Command == commandConcat)
            {
                if (query[0].Nick == Context.Message.From || usr.GetUserInfo().Admin == 2)
                {
                    switch (action)
                    {
                        case "add":
                            Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output + "||" + value, commandConcat);
                            Context.SendMessage(Context.Channel, Models.IrcColors.Blue + "Команда успешно обновлена!");
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
                                    Context.SendMessage(Context.Channel, Models.IrcColors.Green + "Команда успешно обновлена!");
                                }
                                else
                                {
                                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Максимальное число удаления: " + outputlist.Count);
                                }
                            }
                            catch (FormatException)
                            {
                                if (outputlist.Remove(value))
                                {
                                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), commandConcat);
                                    Context.SendMessage(Context.Channel, Models.IrcColors.Green + "Команда успешно обновлена!");
                                }
                                else
                                {
                                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Такой записи не существует...");
                                }
                            }
                            break;
                        default:
                            Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Неправильный ввод, введите @helpcmd editout");
                            break;
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.From}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Команды не существует");
            }
        }

        [Command("cmdrep")]
        [Description("Заменитель строки вывода команды (используете кавычки если замена с пробелом)")]
        [Remarks("Если параметр newstr не заполнен - происходит просто удаление oldstr из команды")]
        public void CustomCmdRepl(string command, string oldstr, string newstr = "")
        {
            var commandConcat = "@" + command;
            var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).ToList();
            UserOperations usr = new UserOperations(Context.Message.From, Context.Connection);
            if (query.Any() && query[0].Command == commandConcat || usr.GetUserInfo().Admin == 2)
            {
                if (query[0].Nick == Context.Message.From || usr.GetUserInfo().Admin == 2)
                {
                    Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output.Replace(oldstr, newstr), commandConcat);
                    Context.SendMessage(Context.Channel, Models.IrcColors.Blue + "Команда успешно обновлена!");
                }
                else
                {
                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + $"Команду создал {query[0].Nick} а не {Context.Message.From}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Команды не существует");
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
                var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat)).Delete();
                if (query > 0)
                {
                    Context.SendMessage(Context.Channel, "Команда удалена!");
                }
            }
            else
            {
                var query = Context.Connection.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat) && v.Nick.Equals(Context.Message.From)).Delete();
                if (query > 0)
                {
                    Context.SendMessage(Context.Channel, "Команда удалена!");
                }
                else
                {
                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Этого не произошло....");
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
                        var tag = new Models.SQL.Tag()
                        {
                            TagName = tagname,
                            Color = ircolor.ToString(),
                            TagCount = 0,
                            Username = Context.Message.From
                        };

                        Context.Connection.Insert(tag);

                        Context.SendMessage(Context.Channel, $"Тег 00,{ircolor}⚫{tagname}{Models.IrcColors.Reset} успешно добавлен!");
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
                        Context.SendMessage(Context.Channel, Models.IrcColors.Gray + $"Тег создал {tagDel.GetTagByName().Username} а не {Context.Message.From}");
                    }
                    break;
                default:
                    Context.SendMessage(Context.Channel, Models.IrcColors.Gray + "Неправильный ввод, введите @helpcmd addtag");
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
                Context.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ :(");
            }
        }

        [Command("tags")]
        [Description("Список всех тегов")]
        public void AllTags()
        {
            List<Models.SQL.Tag> tags = new List<Models.SQL.Tag>();
            var query = Context.Connection.Table<Models.SQL.Tag>();
            foreach (var tag in query)
            {
                tags.Add(tag);
            }
            Context.SendMessage(Context.Channel, string.Join(' ', tags.Select(x => $"{x.Color},00⚫{x.TagName}{Models.IrcColors.Reset}")));
        }
    }
}   
