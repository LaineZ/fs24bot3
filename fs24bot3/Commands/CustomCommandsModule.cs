﻿using fs24bot3.Models;
using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace fs24bot3.Commands
{
    public sealed class CustomCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {
        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();

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
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}[ДЕНЬГИ ВОЗВРАЩЕНЫ] Данная команда уже создана! Если вы создали данную команду используйте @cmdout");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Данная команда уже существует в fs24_bot!");
            }
        }

        [Command("regcmdurl", "regluaurl")]
        [Description("Регистрация команды (Параметр command вводится без @) Документация Lua: https://gist.github.com/LaineZ/67086615e481cb0f5a6c84f8e71103bf")]
        public async void CustomCmdRegisterUrlAsync(string command, string rawurl)
        {
            var response = await http.GetResponseAsync(rawurl);
            if (response != null)
            {
                if (response.ContentType == "text/plain")
                {
                    Stream responseStream = response.GetResponseStream();
                    CustomCmdRegister(command, true, new StreamReader(responseStream).ReadToEnd());
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ =( {response.ContentType}");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Не удалось выполнить запрос...");
            }
        }

        [Command("cmdout")]
        [Description("Редактор строки вывода команды: параметр action: add, delete")]
        [Remarks("Параметр action отвечает за действие команды:\nadd - добавить вывод команды при этом параметр value отвечает за строку вывода\ndelete - удалить вывод команды, параметр value принимает как числовые значения вывода от 0-n, так и строку вывода которую небоходимо удалить (без ||)")]
        public void CustomCmdEdit(string command, CommandToggles.CommandEdit action, [Remainder] string value)
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
                        case CommandToggles.CommandEdit.Add:
                            Context.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output + "||" + value, commandConcat);
                            Context.SendMessage(Context.Channel, IrcColors.Blue + "Команда успешно обновлена!");
                            break;
                        case CommandToggles.CommandEdit.Delete:
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

        [Command("cmdinfo")]
        [Description("Информация о команде")]
        public async void CmdInfo(string command)
        {
            // a small workaround for this exception An exception occurred while executing cmdinfo.: `Cannot get SQL for: Add`
            command = "@" + command;
            var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).FirstOrDefault();
            if (query != null)
            {
                Context.SendMessage(Context.Channel, IrcColors.Blue + $"Команда {query.Command} Создал: `{query.Nick}` Размер вывода: {query.Output.Length} символов, строк - {query.Output.Split("||").Length} Lua: {query.IsLua}");
                if (query.Nick.Length <= 0)
                {
                    Context.SendMessage(Context.Channel, IrcColors.Yellow + "Внимание: данная команда была создана в старой версии fs24bot, пожалуйста используйте @cmdown чтобы изменить владельца команды!");
                }
                if (query.IsLua == 1)
                {
                    string url = await http.UploadToTrashbin(query.Output, "addplain");
                    Context.SendMessage(Context.Channel, $"Исходник команды: {url}");
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
                    Context.Connection.Table<SQL.ScriptStorage>().Where(v => v.Command.Equals(commandConcat)).Delete();
                    Context.SendMessage(Context.Channel, "Команда удалена!");
                }
            }
            else
            {
                var query = Context.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(commandConcat) && v.Nick.Equals(Context.Message.From)).Delete();
                if (query > 0)
                {
                    Context.Connection.Table<SQL.ScriptStorage>().Where(v => v.Command.Equals(commandConcat)).Delete();
                    Context.SendMessage(Context.Channel, "Команда удалена!");
                }
                else
                {
                    Context.SendMessage(Context.Channel, IrcColors.Gray + "Этого не произошло....");
                }
            }
        }
    }
}
