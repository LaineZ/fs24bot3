using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using SQLite;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class CustomCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }

    /// <summary>
    /// Cost needed for creating command
    /// </summary>
    const int COMMAND_COST = 250000;

    private async Task CustomCmdRegisterpublic(string command, bool isLua, [Remainder] string output)
    {
        Context.User.SetContext(Context);
        bool commandIntenral = Service.GetAllCommands().Any(x => x.Aliases.Any(a => a.Equals(command)));

        if (!commandIntenral)
        {
            int isLuaInt = isLua ? 1 : 0;
            var commandInsert = new SQL.CustomUserCommands()
            {
                Command = command,
                Output = output,
                Nick = Context.User.Username,
                IsLua = isLuaInt
            };
            try
            {
                if (await Context.User.RemItemFromInv(Context.BotCtx.Shop, "money", COMMAND_COST))
                {
                    Context.BotCtx.Connection.Insert(commandInsert);
                    await Context.SendMessage(Context.Channel, "Команда успешно создана");
                }
            }
            catch (SQLiteException)
            {
                Context.User.AddItemToInv(Context.BotCtx.Shop, "money", COMMAND_COST);
                await Context.SendMessage(Context.Channel, $"[gray][ДЕНЬГИ ВОЗВРАЩЕНЫ] Данная команда уже создана! Если вы создали данную команду используйте {ConfigurationProvider.Config.Prefix}cmdout");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, $"[gray]Данная команда уже существует в fs24_bot!");
        }
    }

    [Command("regcmd")]
    [Description("Регистрация команды (Параметр command вводится без префикса)")]
    [Remarks("Пользовательские команды позволяют добавлять вам собстенные команды которые будут выводить случайный текст с некоторыми шаблонами. Вывод команды можно разнообразить с помощью '||' - данный набор символов разделяют вывод команды, и при вводе пользователем команды будет выводить случайные фразы разделенные '||'\nЗаполнители (placeholders, patterns) - Позволяют динамически изменять вывод команды:\n#USERINPUT - Ввод пользователя после команды\n#USERNAME - Имя пользователя который вызвал команду\n#RNDNICK - рандомный ник в базе данных пользователей\n#RNG - генереатор случайных чисел")]
    [Checks.FullAccount]
    public async Task CustomCmdRegister(string command, [Remainder] string output)
    {
        await CustomCmdRegisterpublic(command, false, output);
    }

    [Command("regcmdlua", "regcmdl", "reglua")]
    [Description("Регистрация команды (Параметр command вводится без префикса). Документация Lua: https://gist.github.com/LaineZ/67086615e481cb0f5a6c84f8e71103bf")]
    [Checks.FullAccount]
    public async Task CustomCmdRegisterLua(string command, [Remainder] string code)
    {
        await CustomCmdRegisterpublic(command, true, code);
    }

    [Command("cmdout")]
    [Description("Редактор строки вывода команды: параметр action: add, delete")]
    [Remarks("Параметр action отвечает за действие команды:\nadd - добавить вывод команды при этом параметр value отвечает за строку вывода\ndelete - удалить вывод команды, параметр value принимает как числовые значения вывода от 0-n, так и строку вывода которую небоходимо удалить (без ||)")]
    [Checks.FullAccount]
    public async Task CustomCmdEdit(string command, CommandToggles.CommandEdit action, [Remainder] string value)
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).FirstOrDefault();
        
        if (query != null && query.Command == command && query.IsLua == 0)
        {
            if (query.Nick == Context.User.Username || Context.User.GetUserInfo().Admin == 2)
            {
                switch (action)
                {
                    case CommandToggles.CommandEdit.Add:
                        Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query.Output + "||" + value, command);
                        await Context.SendMessage(Context.Channel, "[blue]Команда успешно обновлена!");
                        break;
                    case CommandToggles.CommandEdit.Delete:
                        var outputlist = query.Output.Split("||").ToList();
                        try
                        {
                            int val = int.Parse(value);
                            if (val < outputlist.Count && val >= 0)
                            {
                                outputlist.RemoveAt(val);
                                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), command);
                                await Context.SendMessage(Context.Channel, "[green]Команда успешно обновлена!");
                            }
                            else
                            {
                                await Context.SendMessage(Context.Channel, "[gray]Максимальное число удаления: " + outputlist.Count);
                            }
                        }
                        catch (FormatException)
                        {
                            if (outputlist.Remove(value))
                            {
                                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), command);
                                await Context.SendMessage(Context.Channel, "[green]Команда успешно обновлена!");
                            }
                            else
                            {
                                await Context.SendMessage(Context.Channel, "[gray]Такой записи не существует...");
                            }
                        }
                        break;
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"[gray]Команду создал {query.Nick} а не {Context.User.Username}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, 
                "[gray]Команды не существует или эта команда являктся Lua-командой");
        }
    }


    [Command("cmdown")]
    [Checks.CheckAdmin]
    [Description("Сменить владельца команды")]
    public async Task CmdOwn(string command, string nick)
    {
        Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Nick = ? WHERE Command = ?", nick, command);
        await Context.SendMessage(Context.Channel, "[blue]Команда успешно обновлена!");
    }

    [Command("cmdinfo")]
    [Description("Информация о команде")]
    public async Task CmdInfo(string command)
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).FirstOrDefault();
        if (query != null)
        {
            await Context.SendMessage(Context.Channel, 
                $"[blue]Команда {query.Command} Создал: `{query.Nick}` Размер вывода: {query.Output.Length} символов, строк - {query.Output.Split("||").Length} Lua: {query.IsLua}");
            if (query.Nick.Length <= 0)
            {
                await Context.SendMessage(Context.Channel, 
                    $"Внимание: данная команда была создана в старой версии fs24bot, пожалуйста используйте .cmdown чтобы изменить владельца команды!");
            }
            if (query.IsLua == 1)
            {
                string url = await Helpers.InternetServicesHelper.UploadToTrashbin(query.Output, "addplain");
                await Context.SendMessage(Context.Channel, $"Исходник команды: {url}");
            }
            else
            {
                string output = "";
                string[] splitted = query.Output.Split("||");

                for (int i = 0; i < splitted.Length; i++)
                {
                    output += $"[{i}]: {splitted[i]}\n";
                }
                string url = await Helpers.InternetServicesHelper.UploadToTrashbin(output, "addplain");
                await Context.SendMessage(Context.Channel, $"Исходник команды: {url}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, "Команды не существует");
        }
    }

    [Command("cmdrep")]
    [Description("Заменитель строки вывода команды (используете кавычки если замена с пробелом)")]
    [Remarks("Если параметр newstr не заполнен - происходит просто удаление oldstr из команды")]
    [Checks.FullAccount]
    public async Task CustomCmdRepl(string command, string oldstr, string newstr = "")
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).FirstOrDefault();
        if (query != null && query.Command == command || Context.User.GetUserInfo().Admin == 2)
        {
            if (query.Nick == Context.User.Username || Context.User.GetUserInfo().Admin == 2)
            {
                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query.Output.Replace(oldstr, newstr), command);
                await Context.SendMessage(Context.Channel, "[blue]Команда успешно обновлена!");
            }
            else
            {
                await Context.SendMessage(Context.Channel,$"[gray]Команду создал {query.Nick} а не {Context.User.Username}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, "[gray]Команды не существует");
        }
    }

    [Command("cmdupd")]
    [Description("Полное обновление вывода команды")]
    [Checks.FullAccount]
    public async Task LuaUpdCoommand(string command, [Remainder] string newstr)
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).FirstOrDefault();
        if (query != null && query.IsLua == 1 && query.Command == command || Context.User.GetUserInfo().Admin == 2)
        {
            if (query.Nick == Context.User.Username || Context.User.GetUserInfo().Admin == 2)
            {
                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", newstr, command);
                await Context.SendMessage(Context.Channel, "[blue]Команда успешно обновлена!");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"[gray]Команду создал {query.Nick} а не {Context.User.Username}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, "[gray]Команды не существует");
        }
    }

    [Command("delcmd")]
    [Description("Удалить команду")]
    [Checks.FullAccount]
    public async Task CustomCmdRem(string command)
    {
        if (Context.User.GetUserInfo().Admin == 2)
        {
            var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).Delete();
            if (query > 0)
            {
                Context.BotCtx.Connection.Table<SQL.ScriptStorage>().Where(v => v.Command.Equals(command)).Delete();
                await Context.SendMessage(Context.Channel, "Команда удалена!");
            }
        }
        else
        {
            var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command) && v.Nick.Equals(Context.User.Username)).Delete();
            if (query > 0)
            {
                Context.BotCtx.Connection.Table<SQL.ScriptStorage>().Where(v => v.Command.Equals(command)).Delete();
                await Context.SendMessage(Context.Channel, "Команда удалена!");
                Context.User.AddItemToInv(Context.BotCtx.Shop, "money", COMMAND_COST);
            }
            else
            {
                await Context.SendMessage(Context.Channel, "[gray]Этого не произошло....");
            }
        }
    }
}
