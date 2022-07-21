using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using SQLite;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class CustomCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }

    readonly HttpTools http = new HttpTools();

    /// <summary>
    /// Cost needed for creating command
    /// </summary>
    const int COMMAND_COST = 50000;

    private async Task CustomCmdRegisterpublic(string command, bool isLua, [Remainder] string output)
    {
        User usr = new User(Context.Sender, Context.BotCtx.Connection, Context);
        bool commandIntenral = Service.GetAllCommands().Any(x => x.Aliases.Any(a => a.Equals(command)));

        if (!commandIntenral)
        {
            int isLuaInt = isLua ? 1 : 0;
            var commandInsert = new SQL.CustomUserCommands()
            {
                Command = command,
                Output = output,
                Nick = Context.Sender,
                IsLua = isLuaInt
            };
            try
            {
                if (await usr.RemItemFromInv(Context.BotCtx.Shop, "money", COMMAND_COST))
                {
                    Context.BotCtx.Connection.Insert(commandInsert);
                    await Context.SendMessage(Context.Channel, "Команда успешно создана");
                }
            }
            catch (SQLiteException)
            {
                usr.AddItemToInv(Context.BotCtx.Shop, "money", COMMAND_COST);
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}[ДЕНЬГИ ВОЗВРАЩЕНЫ] Данная команда уже создана! Если вы создали данную команду используйте {Context.User.GetUserPrefix()}cmdout");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}Данная команда уже существует в fs24_bot!");
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

    [Command("regcmdurl", "regluaurl")]
    [Description("Регистрация команды (Параметр command вводится без префикса) Документация Lua: https://gist.github.com/LaineZ/67086615e481cb0f5a6c84f8e71103bf")]
    [Checks.FullAccount]
    public async Task CustomCmdRegisterUrlAsync(string command, string rawurl)
    {
        var response = await http.GetTextPlainResponse(rawurl);
        await CustomCmdRegisterpublic(command, true, response);
    }

    [Command("cmdout")]
    [Description("Редактор строки вывода команды: параметр action: add, delete")]
    [Remarks("Параметр action отвечает за действие команды:\nadd - добавить вывод команды при этом параметр value отвечает за строку вывода\ndelete - удалить вывод команды, параметр value принимает как числовые значения вывода от 0-n, так и строку вывода которую небоходимо удалить (без ||)")]
    [Checks.FullAccount]
    public async Task CustomCmdEdit(string command, CommandToggles.CommandEdit action, [Remainder] string value)
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).ToList();
        User usr = new User(Context.Sender, Context.BotCtx.Connection);
        if (query.Any() && query[0].Command == command && query[0].IsLua == 0)
        {
            if (query[0].Nick == Context.Sender || usr.GetUserInfo().Admin == 2)
            {
                switch (action)
                {
                    case CommandToggles.CommandEdit.Add:
                        Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output + "||" + value, command);
                        await Context.SendMessage(Context.Channel, IrcClrs.Blue + "Команда успешно обновлена!");
                        break;
                    case CommandToggles.CommandEdit.Delete:
                        var outputlist = query[0].Output.Split("||").ToList();
                        try
                        {
                            int val = int.Parse(value);
                            if (val < outputlist.Count && val >= 0)
                            {
                                outputlist.RemoveAt(val);
                                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), command);
                                await Context.SendMessage(Context.Channel, IrcClrs.Green + "Команда успешно обновлена!");
                            }
                            else
                            {
                                await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Максимальное число удаления: " + outputlist.Count);
                            }
                        }
                        catch (FormatException)
                        {
                            if (outputlist.Remove(value))
                            {
                                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", string.Join("||", outputlist), command);
                                await Context.SendMessage(Context.Channel, IrcClrs.Green + "Команда успешно обновлена!");
                            }
                            else
                            {
                                await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Такой записи не существует...");
                            }
                        }
                        break;
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Gray + $"Команду создал {query[0].Nick} а не {Context.Sender}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Команды не существует или эта команда являктся Lua-командой");
        }
    }


    [Command("cmdown")]
    [Checks.CheckAdmin]
    [Description("Сменить владельца команды")]
    public async Task CmdOwn(string command, string nick)
    {
        Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Nick = ? WHERE Command = ?", nick, command);
        await Context.SendMessage(Context.Channel, IrcClrs.Blue + "Команда успешно обновлена!");
    }

    [Command("cmdinfo")]
    [Description("Информация о команде")]
    public async Task CmdInfo(string command)
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).FirstOrDefault();
        if (query != null)
        {
            await Context.SendMessage(Context.Channel, IrcClrs.Blue + $"Команда {query.Command} Создал: `{query.Nick}` Размер вывода: {query.Output.Length} символов, строк - {query.Output.Split("||").Length} Lua: {query.IsLua}");
            if (query.Nick.Length <= 0)
            {
                await Context.SendMessage(Context.Channel, $"Внимание: данная команда была создана в старой версии fs24bot, пожалуйста используйте {Context.User.GetUserPrefix()}cmdown чтобы изменить владельца команды!");
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
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).ToList();
        User usr = new User(Context.Sender, Context.BotCtx.Connection);
        if (query.Any() && query[0].Command == command || usr.GetUserInfo().Admin == 2)
        {
            if (query[0].Nick == Context.Sender || usr.GetUserInfo().Admin == 2)
            {
                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", query[0].Output.Replace(oldstr, newstr), command);
                await Context.SendMessage(Context.Channel, IrcClrs.Blue + "Команда успешно обновлена!");
            }
            else
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Gray + $"Команду создал {query[0].Nick} а не {Context.Sender}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Команды не существует");
        }
    }

    [Command("cmdupd")]
    [Description("Полное обновление вывода команды")]
    [Checks.FullAccount]
    public async Task LuaUpdCoommand(string command, [Remainder] string newstr)
    {
        var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command)).ToList();
        User usr = new User(Context.Sender, Context.BotCtx.Connection);
        if (query.Any() && query[0].IsLua == 1 && query[0].Command == command || usr.GetUserInfo().Admin == 2)
        {
            if (query[0].Nick == Context.Sender || usr.GetUserInfo().Admin == 2)
            {
                Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Output = ? WHERE Command = ?", newstr, command);
                await Context.SendMessage(Context.Channel, IrcClrs.Blue + "Команда успешно обновлена!");
            }
            else
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Gray + $"Команду создал {query[0].Nick} а не {Context.Sender}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Команды не существует");
        }
    }

    [Command("delcmd")]
    [Description("Удалить команду")]
    [Checks.FullAccount]
    public async Task CustomCmdRem(string command)
    {
        User usr = new User(Context.Sender, Context.BotCtx.Connection);
        if (usr.GetUserInfo().Admin == 2)
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
            var query = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(command) && v.Nick.Equals(Context.Sender)).Delete();
            if (query > 0)
            {
                Context.BotCtx.Connection.Table<SQL.ScriptStorage>().Where(v => v.Command.Equals(command)).Delete();
                await Context.SendMessage(Context.Channel, "Команда удалена!");
                usr.AddItemToInv(Context.BotCtx.Shop, "money", COMMAND_COST);
            }
            else
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Этого не произошло....");
            }
        }
    }

    [Command("dellrc", "deletelyrics", "removelyrics", "remlyr", "dellyr")]
    [Description("Удалить свои слова из базы бота: параметр song должен быть в формате `artist - trackname`")]
    [Checks.FullAccount]
    public async Task CustomLyrRem([Remainder] string song)
    {
        var data = song.Split(" - ");
        string artist = data[0];
        string track = data[1];

        if (data.Length <= 1)
        {
            Context.SendErrorMessage(Context.Channel, "Недопустмый синтаксис команды: параметр song должен быть в формате `artist - trackname`!");
            return;
        }
        else
        {
            artist = data[0];
            track = data[1];
        }

        User usr = new User(Context.Sender, Context.BotCtx.Connection);
        if (usr.GetUserInfo().Admin == 2)
        {
            var query = Context.BotCtx.Connection.Table<SQL.LyricsCache>().Where(v => v.Artist.Equals(artist) && v.Track.Equals(track)).Delete();
            if (query > 0)
            {
                await Context.SendMessage(Context.Channel, "Песня удалена!");
            }
            else
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Этого не произошло....");
            }
        }
        else
        {
            var query = Context.BotCtx.Connection.Table<SQL.LyricsCache>().Where(v => v.Artist.Equals(artist) && v.Track.Equals(track) && v.AddedBy.Equals(Context.Sender)).Delete();
            if (query > 0)
            {
                await Context.SendMessage(Context.Channel, "Песня удалена!");
            }
            else
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Gray + "Этого не произошло....");
            }
        }
    }
}
