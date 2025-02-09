﻿using fs24bot3.Models;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using fs24bot3.QmmandsProcessors;
using fs24bot3.Core;
using System.Globalization;
using fs24bot3.Helpers;
using SQLite;
using fs24bot3.Properties;
using System.Text;
using fs24bot3.Parsers;
using HandlebarsDotNet;
using Serilog;

namespace fs24bot3.Commands;

public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }
    


    private string TrimTimezoneName(string name)
    {
        return name.Split(" ")[0];
    }

    private async Task HelpAll()
    {
        var cmds = Service.GetAllCommands();
        string commandsOutput = "";
        //string commandsOutput = Resources.help;
        try
        {
            commandsOutput = File.ReadAllText("help.html");
        }
        catch (FileNotFoundException)
        {
            Log.Verbose("help.html not found, using compiled one");
            commandsOutput = Resources.help;
        }

        var template = Handlebars.Compile(commandsOutput);

        var customCommands =
            Context.BotCtx.Connection.Query<SQL.CustomUserCommands>(
                "SELECT * FROM CustomUserCommands ORDER BY length(Output) DESC");
        var commandList = cmds.Select(x =>
        {
            return new
            {
                prefix = ConfigurationProvider.Config.Prefix,
                name = x.Name,
                parameters = x.Parameters.Select(x => x.Name),
                description = x.Description,
                checks = x.Checks.Select(x => x.ToString())
            };
        });
        var data = new
        {
            commands = commandList,
            customCommands = customCommands.Select(x => new
            {
                prefix = ConfigurationProvider.Config.Prefix,
                name = x.Command,
                createdBy = string.IsNullOrWhiteSpace(x.Nick) ? "fs24bot" : x.Nick,
                isLua = x.IsLua == 1
            })
        };

        string link = await InternetServicesHelper.UploadToTrashbin(template(data));
        await Context.SendMessage(Context.Channel,
            $"Выложены команды по этой ссылке: {link} также вы можете написать `{ConfigurationProvider.Config.Prefix}help имякоманды` для получения дополнительной помощи");
    }

    private async Task HelpCmd(string command)
    {
        foreach (Command cmd in Service.GetAllCommands())
        {
            if (cmd.Aliases.Contains(command))
            {
                await Context.SendMessage(Context.Channel,
                    ConfigurationProvider.Config.Prefix + cmd.Name + " " +
                    string.Join(" ", cmd.Parameters.Select(x => $"[{x.Name} default: {x.DefaultValue}]")) + " - " +
                    cmd.Description);
                if (cmd.Remarks != null)
                {
                    await Context.SendMessage(Context.Channel, cmd.Remarks);
                }

                await Context.SendMessage(Context.Channel, $"[b]Алиасы: [r]{string.Join(", ", cmd.Aliases)}");
                return;
            }
        }

        await Context.SendSadMessage(Context.Channel,
            $"К сожалению команда не найдена, если вы пытаетесь посмотреть справку по кастом команде: используйте {ConfigurationProvider.Config.Prefix}cmdinfo");
    }

    [Command("help", "commands", "cmds", "helpcmd")]
    [Description("Справка, если параметр `commandName` пуст - выведет список всех команд в виде HTML страницы")]
    public async Task Help(string commandName = "")
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            await HelpAll();
        }
        else
        {
            await HelpCmd(commandName);
        }
    }

    [Command("remind", "in")]
    [Description("Напоминание. time вводится в формате 1m30s (1 минута и 30 секунд = 90 секунд)")]
    public async Task Remind(string time = "1m", [Remainder] string message = "")
    {
        double totalSecs = 0;

        if (message == "")
        {
            message = RandomMsgs.RemindMessages.Random();
        }

        if (time.Contains('-'))
        {
            await Context.SendSadMessage(Context.Channel, "Отрицательные числа недопустимы");
            return;
        }

        var timeSegments = Regex.Matches(time, @"(\d+[ywdhms])");

        if (!timeSegments.Any())
        {
            await Context.SendErrorMessage(Context.Channel,
                $"Неверный формат времени или неверные единицы измерения времени");
            return;
        }

        foreach (Match segment in timeSegments)
        {
            var value = double.Parse(segment.Value.TrimEnd('y', 'w', 'd', 'h', 'm', 's'));
            switch (segment.Value[^1])
            {
                case 'y':
                    totalSecs += 31556926 * value;
                    break;
                case 'w':
                    totalSecs += 604800 * value;
                    break;
                case 'd':
                    totalSecs += 86400 * value;
                    break;
                case 'h':
                    totalSecs += 3600 * value;
                    break;
                case 'm':
                    totalSecs += 60 * value;
                    break;
                case 's':
                    totalSecs += value;
                    break;
                default:
                    await Context.SendErrorMessage(Context.Channel,
                        $"Неизвестная единица измерения времени: {segment.Value[^1]}");
                    return;
            }
        }

        if (totalSecs <= 0)
        {
            await Context.SendSadMessage(Context.Channel, "Потерялся во времени?");
            return;
        }

        try
        {
            TimeSpan ts = TimeSpan.FromSeconds(totalSecs);
            Context.User.AddRemind(ts, message, Context.Channel);
            await Context.SendMessage(Context.Channel, $"{message} через {ts.ToReadableString()}");
        }
        catch (OverflowException)
        {
            await Context.SendSadMessage(Context.Channel, "ddos.sh");
        }
    }


    [Command("at", "remindat")]
    [Description("Напоминание. Введите дату и время в формате 'yyyy-MM-dd HH:mm:ss'")]
    public async Task RemindAt(string dateTimeString, [Remainder] string message = "")
    {
        if (message == "")
        {
            message = RandomMsgs.RemindMessages.Random();
        }

        string[] formats = { "yyyy-MM-dd HH:mm:ss", "HH:mm", "HH:mm:ss", "yyyy-MM-dd" };
        foreach (var item in formats)
        {
            var timeZone = Context.User.GetTimeZone();
            if (DateTime.TryParseExact(dateTimeString, item, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out DateTime remindDateTime))
            {
                remindDateTime = TimeZoneInfo.ConvertTimeToUtc(remindDateTime, timeZone);

                if (remindDateTime <= DateTime.UtcNow)
                {
                    await Context.SendSadMessage(Context.Channel, "Потерялся во времени?");
                    return;
                }

                TimeSpan timeUntilRemind = remindDateTime - DateTime.UtcNow;
                Context.User.AddRemind(timeUntilRemind, message, Context.Channel);
                await Context.SendMessage(Context.Channel,
                    $"{message} в {dateTimeString} {TrimTimezoneName(timeZone.DisplayName)}");
                return;
            }

            continue;
        }

        await Context.SendSadMessage(Context.Channel,
            $"Неверный формат времени, допустимые форматы: {string.Join(", ", formats)}");
    }

    [Command("delmind", "delremind", "deleteremind")]
    [Description("Удалить напоминание")]
    public async Task DeleteRemind(uint id)
    {
        if (Context.User.DeleteRemind(id))
        {
            await Context.SendMessage(Context.Channel, $"Напоминание удалено!");
        }
        else
        {
            await Context.SendSadMessage();
        }
    }

    [Command("time")]
    [Description("Время")]
    public async Task UserTime(string username = "")
    {
        if (string.IsNullOrEmpty(username))
        {
            if (Context.FromBridge)
            {
                await Context.SendSadMessage(Context.Channel, "Укажите ник пользователя");
                return;
            }

            username = Context.User.Username;
        }

        var usr = new User(username, in Context.BotCtx.Connection);
        var timezone = usr.GetTimeZone();
        var time = DateTime.Now.ToUniversalTime();
        CultureInfo rus = new CultureInfo("ru-RU", false);

        await Context.SendMessage(
            $"Сейчас у {username} {TimeZoneInfo.ConvertTimeFromUtc(time, timezone).ToString(rus)} {TrimTimezoneName(timezone.DisplayName)}");
    }

    [Command("reminds", "rems")]
    [Description("Список напоминаний")]
    public async Task Reminds(string username = "", string locale = "ru-RU")
    {
        if (string.IsNullOrEmpty(username))
        {
            username = Context.User.Username;
        }

        var usr = new User(username, in Context.BotCtx.Connection);

        var reminds = usr.GetReminds();
        var timezone = usr.GetTimeZone();

        if (!reminds.Any())
        {
            await Context.SendSadMessage(Context.Channel, $"У пользователя {username} нет напоминаний!");
            return;
        }

        StringBuilder rems = new StringBuilder($"[b]Напоминания {username}:\n");

        foreach (var remind in reminds)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            CultureInfo rus = new CultureInfo(locale, false);
            dt = dt.AddSeconds(remind.RemindDate).ToUniversalTime();
            var dtDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt, timezone);

            if (usr.Username == Context.User.Username)
            {
                rems.Append(
                    $"id: {remind.Id}: \"{remind.Message}\" в [b]{dtDateTime.ToString(rus)}" +
                    $"{TrimTimezoneName(timezone.DisplayName)} [r]или через [blue]{dt.Subtract(DateTime.UtcNow).ToReadableString()}\n");
            }
            else
            {
                rems.Append($"\"{remind.Message}\" в [b]{dtDateTime.ToString(rus)} " +
                            $"{TrimTimezoneName(timezone.DisplayName)} [r]или через [blue]{dt.Subtract(DateTime.UtcNow).ToReadableString()}\n");
            }
        }

        await Context.SendMessage(Context.Channel, rems.ToString());
    }


    [Command("warnings", "warns")]
    [Checks.FullAccount]
    public async Task GetWarns()
    {
        var warns = Context.User.GetWarnings();
        var warnsStr = new StringBuilder();

        if (!warns.Any())
        {
            await Context.SendSadMessage(Context.Channel, $"У вас нет предупреждений!");
            return;
        }

        await Context.SendMessage(Context.Channel, string.Join(" ", warns.Select(x => $"{x.Nick}: {x.Message}")));

        Context.User.DeleteWarnings();
    }

    [Command("unicode", "u")]
    [Description("Поиск кодпоинтов и симоволов юникода")]
    public async Task UnicodeFind([Remainder] string find)
    {
        find = find.ToLower();
        if (!File.Exists("chars.sqlite"))
        {
            var http = new HttpTools();
            await http.DownloadFile("chars.sqlite", "https://storage.buttex.ru/data/fs24bot/chars.sqlite");
            await Context.SendMessage(Context.Channel, $"Таблица юникода... УСПЕШНО ЗАГРУЖЕНА!");
        }

        SQLiteConnection connect = new SQLiteConnection("chars.sqlite");


        if (find.Length <= 2)
        {
            var character = connect.Query<SQL.Chars>("SELECT * FROM chars WHERE hexcode = ? LIMIT 1", find)
                .FirstOrDefault();

            if (character != null)
            {
                await Context.SendMessage($"[r][b]{character.Name}[r] {character.Hexcode} [green]({character.Symbol})");
            }
            else
            {
                await Context.SendSadMessage(Context.Channel, "Символы не обнаружены");
            }
        }
        else
        {
            var query = connect.Table<SQL.Chars>()
                .Where(x => x.Symbol == find || x.Name.ToLower().Contains(find))
                .Take(10);

            if (query.Any())
            {
                await Context.SendMessage(string.Join(", ",
                    query.Select(x => $"[r][b]{x.Name}[r] {x.Hexcode} [green]({x.Symbol})")));
            }
            else
            {
                await Context.SendSadMessage(Context.Channel, "Символы не обнаружены");
            }
        }
    }

    [Command("genname")]
    [Description("Генератор имен")]
    public async Task GenName(bool isRussian = false, int maxlen = 10, uint count = 10)
    {
        List<string> names = new List<string>();
        for (int i = 0; i < Math.Clamp(count, 1, 10); i++)
        {
            if (!isRussian)
            {
                names.Add(MessageHelper.GenerateName(Math.Clamp(maxlen, 5, 20)));
            }
            else
            {
                names.Add(MessageHelper.GenerateNameRus(Math.Clamp(maxlen, 5, 20)));
            }
        }

        await Context.SendMessage(Context.Channel, string.Join(",", names));
    }

    [Command("midi")]
    [Description("Миди ноты")]
    public async Task Midi(string note = "a", int oct = 4)
    {
        string[] noteString = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        if (uint.TryParse(note, out uint initialNote))
        {
            int octave = (int)(initialNote / 12) - 1;
            uint noteIndex = initialNote % 12;
            string noteName = noteString[noteIndex];
            await Context.SendMessage(Context.Channel, $"MIDI: {note} = [r]{noteName}{octave}");
        }
        else
        {
            for (int i = 0; i < noteString.Length; i++)
            {
                if (noteString[i].ToLower() == note.ToLower())
                {
                    int noteIndex = (12 * (oct + 1)) + i;
                    await Context.SendMessage(Context.Channel, $"{note}{oct} = MIDI: [r]{noteIndex}");
                    break;
                }
            }
        }
    }

    [Command("seen", "see", "lastseen")]
    [Description("Когда последний раз пользователь писал сообщения")]
    public async Task LastSeen(string destination)
    {
        if (destination == Context.BotCtx.Client.Name)
        {
            await Context.SendMessage(Context.Channel, "Я ЗДЕСЬ!");
            return;
        }

        var user = new User(destination, in Context.BotCtx.Connection);
        TimeSpan date = DateTime.Now.Subtract(user.GetLastMessage());

        if (date.Days < 1000)
        {
            await Context.SendMessage(Context.Channel,
                $"Последний раз я видел [b]{destination}[r] {date.ToReadableString()} назад");
            var messages = await Context.ServicesHelper.GetMessagesSprout(user.GetLastMessage());
            var lastmsg = messages.LastOrDefault(x => x.Nick == destination);
            if (lastmsg != null)
            {
                await Context.SendMessage(Context.Channel,
                    $"Последнее сообщение от пользователя: [b]" +
                    $"{messages.LastOrDefault(x => x.Nick == destination)?.Message}");
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel, $"Я никогда не видел {destination}...");
        }
    }

    [Command("dtfnamegen", "dtfname")]
    [Description("Генератор имен")]
    public async Task Namegen(uint count = 1)
    {
        count = Math.Clamp(count, 1, 10);
        var output = new StringBuilder();
        var nouns = Resources.nouns.Split("\n");
        var adjectives = Resources.adjectives.Split("\n");

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                output.Append(", ");
            }

            output.Append(adjectives.Random()).Append(' ').Append(nouns.Random());
        }

        await Context.SendMessage(output.ToString());
    }

    private async Task ExecuteLua(string input)
    {
        var lua = LuaExecutor.CreateLuaState();
        // Block danger functions
        lua["os.execute"] = null;
        lua["os.exit"] = null;
        lua["os.remove"] = null;
        lua["os.getenv"] = null;
        lua["os.rename"] = null;
        lua["os.setlocale"] = null;
        lua["os.tmpname"] = null;

        lua["io"] = null;
        lua["debug"] = null;
        lua["require"] = null;
        lua["print"] = null;
        lua["pcall"] = null;
        lua["xpcall"] = null;
        lua["load"] = null;
        lua["loadfile"] = null;
        lua["dofile"] = null;
        lua["luanet"] = null;
        lua["getmetatable"] = null;
        lua["setmetatable"] = null;
        lua["package"] = null;

        try
        {
            var res = lua.DoString(input)[0];
            if (res != null)
            {
                await Context.SendMessage(res.ToString());
            }
            else
            {
                await Context.SendMessage("PENETRATION TEST, TEST. СПЕЦИАЛЬНО ДЛЯ ТЕБЯ!!!");
            }
        }
        catch (Exception e)
        {
            await Context.SendMessage($"Ошибка выражения: {e.Message}");
        }
        finally
        {
            lua.Close();
            lua.Dispose();
        }
    }

    [Command("calculator", "c", "calc")]
    [Description("Калькулятор")]
    public async Task Calculator([Remainder] string expression)
    {
        await ExecuteLua("return " + expression);
    }

    [Command("lua")]
    [Description("Lua интерпретатор")]
    [Checks.CheckAdmin]
    public async Task Lua([Remainder] string expression)
    {
        await ExecuteLua(expression);
    }

    [Command("creategoal", "addgoal", "newgoal", "ag")]
    [Description("Создать цель. Лимит описания цели 50 символов")]
    [Checks.FullAccount]
    public async Task CreateGoal([Remainder] string goalDescription = "")
    {
        if (goalDescription.Length > 50)
        {
            await Context.SendErrorMessage("У вас слишком длинное название цели, максимальная длина цели 50 символов.");
            return;
        }
        
        var taskComplexityMatch = Regex.Matches(goalDescription, @"(\d+)\/(\d+)").FirstOrDefault();

        uint completed = 0;
        uint total = 1;

        if (taskComplexityMatch != null)
        {
            completed = uint.TryParse(taskComplexityMatch.Groups.Values.First().Value, out completed) ? completed : 0;
            total = uint.TryParse(taskComplexityMatch.Groups.Values.Last().Value, out total) ? total : 1;
        }

        var goal = Context.User.AddGoal(goalDescription, completed, total);

        await Context.SendMessage(
            $"Цель {Context.User.Username} `{goal.Goal}` успешно создана просмотреть можно с помощью {ConfigurationProvider.Config.Prefix}goal get {goal.Id}!");
    }

    [Command("setgoal", "updategoal", "updgoal", "upgoal", "sg")]
    [Description("Изменить прогресс цели. ID цели можно получить в goals или goal get [часть описания цели]")]
    [Remarks("Параметр progress принимает значения (числа) в стиле [progress]/[total] или [progress]. Обратите внимание что [total] не может быть равен 0")]
    [Checks.FullAccount]
    public async Task SetGoal(string goalDescriptionOrId, GoalProgress progress)
    {
        var goal = FindGoalFuzzy(goalDescriptionOrId);

        if (goal is null)
        {
            await Context.SendSadMessage();
            return;
        }

        if (progress.Total > 0)
        {
            goal.Total = progress.Total;
        }

        goal.Progress = Math.Clamp(progress.Progress, 0, goal.Total);
        
        Context.User.UpdateGoal(goal);
        await Context.SendMessage(goal.ToString());
    }


    private SQL.Goals FindGoalFuzzy(string goal)
    {
        SQL.Goals finding;

        if (int.TryParse(goal, out int id))
        {
            finding = Context.User.FindGoalById(id);
        }
        else
        {
            finding = Context.User.SearchGoals(goal).FirstOrDefault();
        }

        return finding;
    }

    [Command("goals")]
    [Description("Список ваших целей")]
    [Checks.FullAccount]
    public async Task GetGoals()
    {
        var goals = Context.User.GetAllGoals();
        var sb = new StringBuilder();

        if (!goals.Any())
        {
            await Context.SendSadMessage(Context.Channel, "У вас нет целей...");
            return;
        }

        foreach (var g in goals)
        {
            sb.Append($"{g}\n");
        }

        await Context.SendMessage(sb.ToString());
    }

    [Command("goal")]
    [Description("Простотр и удаление вашей цели, параметр goalDescriptionOrId принимает как ID, так и часть описания задачи")]
    [Remarks("Параметр action принимает следующие значения:\nget - получить информацию по цели\ndelete - удалить цель")]
    [Checks.FullAccount]
    public async Task Goal(CommandToggles.Goal action, [Remainder] string goalDescriptionOrId)
    {
        var finding = FindGoalFuzzy(goalDescriptionOrId);
        
        if (finding is null)
        {
            await Context.SendSadMessage();
            return;
        }
        
        switch (action)
        {
            case CommandToggles.Goal.Get:
                await Context.SendMessage(finding.ToString());
                break;
            case CommandToggles.Goal.Delete:
                Context.User.DeleteGoal(finding.Id);
                await Context.SendMessage($"Цель от {Context.User} с ID {finding.Id} была удалена!");
                break;
        }
    }

    [Command("color", "rgb", "clr")]
    [Disabled]
    public async Task ColorTransform(Color color, CommandToggles.ColorFormats outputFormat = CommandToggles.ColorFormats.Hex)
    {
        switch (outputFormat)
        {
            case CommandToggles.ColorFormats.Hex:
                var colorstring = string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B); 
                await Context.SendMessage(colorstring);
                break;
            case CommandToggles.ColorFormats.RGB255:
                await Context.SendMessage($"rgb({color.R}, {color.G}, {color.B})");
                break;
            case CommandToggles.ColorFormats.RGB1:
                await Context.SendMessage($"{((float)color.R / 255.0):0.###}, {((float)color.G / 255.0):0.###}, {((float)color.B / 255.0):0.###}");
                break;
        }
    }
}
