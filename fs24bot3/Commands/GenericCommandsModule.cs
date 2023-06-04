using fs24bot3.Models;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using fs24bot3.QmmandsProcessors;
using fs24bot3.Core;
using System.Globalization;
using fs24bot3.Systems;
using fs24bot3.Helpers;
using SQLite;
using System.Diagnostics;
using fs24bot3.Properties;
using System.Text;
using NLua;
using Serilog;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace fs24bot3.Commands;
public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{
    public CommandService Service { get; set; }

    private string ToReadableString(TimeSpan span)
    {
        string formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? string.Format("{0:0} дн. ", span.Days) : string.Empty,
            span.Duration().Hours > 0 ? string.Format("{0:0} ч. ", span.Hours) : string.Empty,
            span.Duration().Minutes > 0 ? string.Format("{0:0} мин. ", span.Minutes) : string.Empty,
            span.Duration().Seconds > 0 ? string.Format("{0:0} сек.", span.Seconds) : string.Empty);
        if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

        if (string.IsNullOrEmpty(formatted)) formatted = "0 секунд";

        return formatted;
    }

    private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
    {
        for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
            yield return day;
    }

    private string TrimTimezoneName(string name)
    {
        return name.Split(" ")[0];
    }

    [Command("help", "commands")]
    [Description("Список команд")]
    public async Task Help()
    {
        var cmds = Service.GetAllCommands();
        string commandsOutput = Resources.help;
        var customCommands = Context.BotCtx.Connection.Query<SQL.CustomUserCommands>("SELECT * FROM CustomUserCommands ORDER BY length(Output) DESC");
        string commandList = string.Join('\n', cmds.Where(x => !x.Checks.Any(x => x is Checks.CheckAdmin)).
            Select(x => 
            $"<strong>{ConfigurationProvider.Config.Prefix}{x.Name}</strong> {string.Join(' ', x.Parameters)}</p><p class=\"desc\">{x.Description}</p><p>Требования: {string.Join(' ', x.Checks)}</p><hr>"));
        string customList = string.Join('\n', string.Join("\n", customCommands.
            Select(x => 
            $"<p>{ConfigurationProvider.Config.Prefix}{x.Command} Создал: <strong>{x.Nick}</strong> Lua: {(x.IsLua == 1 ? "Да" : "Нет")} </p>")));

        commandsOutput = commandsOutput.Replace("[CMDS]", commandList);
        commandsOutput = commandsOutput.Replace("[CUSTOMLIST]", customList);

        string link = await InternetServicesHelper.UploadToTrashbin(commandsOutput);
        await Context.SendMessage(Context.Channel, 
            $"Выложены команды по этой ссылке: {link} также вы можете написать {ConfigurationProvider.Config.Prefix}helpcmd имякоманды для получения дополнительной помощи");
    }

    [Command("helpcmd")]
    [Description("Помощь по команде")]
    public async Task HelpСmd(string command = "helpcmd")
    {
        foreach (Command cmd in Service.GetAllCommands())
        {
            if (cmd.Aliases.Contains(command))
            {
                await Context.SendMessage(Context.Channel, ConfigurationProvider.Config.Prefix + cmd.Name + " " + string.Join(" ", cmd.Parameters.Select(x => $"[{x.Name} default: {x.DefaultValue}]")) + " - " + cmd.Description);
                if (cmd.Remarks != null)
                {
                    await Context.SendMessage(Context.Channel, cmd.Remarks);
                }

                await Context.SendMessage(Context.Channel, $"[b]Алиасы: [r]{string.Join(", ", cmd.Aliases)}");
                return;
            }
        }

        Context.SendSadMessage(Context.Channel, 
            $"К сожалению команда не найдена, если вы пытаетесь посмотреть справку по кастом команде: используйте {ConfigurationProvider.Config.Prefix}cmdinfo");
    }

    [Command("remind", "in")]
    [Description("Напоминание. time вводится в формате 1m;30s (1 минута и 30 секунд = 90 секунд)")]
    public async Task Remind(string time = "1m", [Remainder] string message = "Remind")
    {
        double totalSecs = 0;

        if (time.Contains('-'))
        {
            Context.SendSadMessage(Context.Channel, "Отрицательные числа недопустимы");
            return;
        }

        foreach (var part in time.Split(';'))
        {
            switch (part[^1])
            {
                case 'y':
                    totalSecs += 31556926 * uint.Parse(part.Trim('y'));
                    break;
                case 'w':
                    totalSecs += 604800 * uint.Parse(part.Trim('w'));
                    break;
                case 'd':
                    totalSecs += 86400 * uint.Parse(part.Trim('d'));
                    break;
                case 'h':
                    totalSecs += 3600 * uint.Parse(part.Trim('h'));
                    break;
                case 'm':
                    totalSecs += 60 * uint.Parse(part.Trim('m'));
                    break;
                case 's':
                    totalSecs += 1 * uint.Parse(part.Trim('s'));
                    break;
                default:
                    Context.SendErrorMessage(Context.Channel, $"Неизвестная единица измерения времени: {part[^1]}");
                    return;
            }
        }

        if (totalSecs < 0)
        {
            Context.SendSadMessage(Context.Channel, "Потерялся во времени?");
            return;
        }

        TimeSpan ts = TimeSpan.FromSeconds(totalSecs);
        Context.User.AddRemind(ts, message, Context.Channel);
        await Context.SendMessage(Context.Channel, $"{message} через {ToReadableString(ts)}");
    }

    [Command("delmind", "delremind", "deleteremind")]
    [Description("Удалить напоминание")]
    [Checks.FullAccount]
    [Checks.BridgeLimitedFunctions]
    public async Task DeleteRemind(uint id)
    {
        if (Context.User.DeleteRemind(id))
        {
            await Context.SendMessage(Context.Channel, $"Напоминание удалено!");
        }
        else
        {
            Context.SendSadMessage(Context.Channel);
        }
    }

    [Command("time")]
    [Description("Время")]
    [Checks.BridgeLimitedFunctions]
    public async Task UserTime(string username = "")
    {
        if (string.IsNullOrEmpty(username))
        {
            username = Context.User.Username;
        }

        var usr = new User(username, in Context.BotCtx.Connection);
        var timezone = usr.GetTimeZone();
        var time = DateTime.Now.ToUniversalTime();
        CultureInfo rus = new CultureInfo("ru-RU", false);

        await Context.SendMessage($"Сейчас у {username} {TimeZoneInfo.ConvertTimeFromUtc(time, timezone).ToString(rus)} {TrimTimezoneName(timezone.DisplayName)}");
    }

    [Command("reminds", "rems")]
    [Description("Список напоминаний")]
    [Checks.BridgeLimitedFunctions]
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
            Context.SendSadMessage(Context.Channel, $"У пользователя {username} нет напоминаний!");
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
                    $"id: {remind.RemindDate}: \"{remind.Message}\" в [b]{dtDateTime.ToString(rus)}" +
                    $"{TrimTimezoneName(timezone.DisplayName)} [r]или через [blue]{ToReadableString(dt.Subtract(DateTime.UtcNow))}\n");
            }
            else
            {
                rems.Append($"\"{remind.Message}\" в [b]{dtDateTime.ToString(rus)} " +
                $"{TrimTimezoneName(timezone.DisplayName)} [r]или через [blue]{ToReadableString(dt.Subtract(DateTime.UtcNow))}\n");
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
            Context.SendSadMessage(Context.Channel, $"У вас нет предупреждений!");
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
            await http.DownloadFile("chars.sqlite", "https://bpm140.xyz/chars.sqlite");
            await Context.SendMessage(Context.Channel, $"Таблица юникода, УСПЕШНО ЗАГРУЖЕНА!");
        }

        SQLiteConnection connect = new SQLiteConnection("chars.sqlite");

        var query = connect.Table<SQL.Chars>()
                    .Where(x => x.Symbol == find || x.Hexcode == find || x.Name.ToLower()
                    .Contains(find))
                    .Take(10);

        if (query.Any())
        {
            await Context.SendMessage(string.Join(", ", query.Select(x => $"[r][b]{x.Name}[r] {x.Hexcode} [green]({x.Symbol})")));
        }
        else
        {
            Context.SendSadMessage(Context.Channel, "Символы не обнаружены");
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

    [Command("getimages", "images", "img", "imagefind", "findimage")]
    [Description("Получает изображение из логов")]
    public async Task GetImagesFromLogs(string nickname, string dStart, string dEnd, bool htmlOutput = true)
    {
        Regex regex = new("https?://.*.(png|jpg|gif|webp|jpeg)");
        var dateStart = DateTime.Now;
        DateTime.TryParse(dStart, out dateStart);

        Stopwatch stopWatch = new Stopwatch();
        var result = DateTime.TryParse(dEnd, out DateTime dateEnd);
        string output = "";

        if (!result)
        {
            await Context.SendMessage(Context.Channel, "Ошибка ввода конечной даты!");
            return;
        }

        var totalDays = EachDay(dateStart, dateEnd).Count();
        int current = 0;

        foreach (var date in EachDay(dateStart, dateEnd))
        {
            stopWatch.Start();
            var messages = await Context.ServicesHelper.GetMessages(date);
            foreach (var message in messages)
            {
                var captures = regex.Match(message.Message);
                if (message.Nick == nickname && captures.Success)
                {
                    if (htmlOutput)
                    {
                        output += $"<p>{message.Date} from <strong>{message.Nick}</strong></p><img src='{captures.Value}' alt='{captures.Value}' style='width: auto; height: 100%;'>\n";
                    }
                    else
                    {
                        output += $"{captures.Value}\n";
                    }
                }
            }
            stopWatch.Stop();
            current++;

            if (Context.Random.Next(0, 1000) == 25)
            {
                var left = stopWatch.ElapsedTicks * (totalDays - current);
                await Context.SendMessage(Context.Channel, $"Обработка логфайла: {current}/{totalDays} Осталось: {ToReadableString(new TimeSpan(left))}. Обработка одного логфайла занимает: {stopWatch.ElapsedMilliseconds} ms");
            }

            stopWatch.Restart();
        }
        await Context.SendMessage(Context.Channel, await InternetServicesHelper.UploadToTrashbin(output, htmlOutput ? "add" : "addplain"));
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
            await Context.SendMessage(Context.Channel, $"Последний раз я видел [b]{destination}[r] {ToReadableString(date)} назад");
            var messages = await Context.ServicesHelper.GetMessages(user.GetLastMessage());
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

    [Command("rndl", "randomlyrics")]
    [Description("Рандомная песня")]
    public async Task RandomSong()
    {
        var query = Context.BotCtx.Connection.Table<SQL.LyricsCache>().ToList();

        if (query.Count > 0)
        {
            string[] lyrics = query.Random().Lyrics.Split("\n");
            int baseoffset = Context.Random.Next(0, lyrics.Length - 1);
            string outputmsg = "";

            for (int i = 0; i < Context.Random.Next(1, 5); i++)
            {
                if (lyrics.Length > baseoffset + i) { outputmsg += " " + lyrics[baseoffset + i].Trim(); }
            }

            await Context.SendMessage(Context.Channel, outputmsg);
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

    [Command("calculator", "c", "calc")]
    [Description("Калькулятор")]
    public async Task Calculator([Remainder] string expression)
    {
        IntPtr pointer = IntPtr.Zero;
        long currentMemoryUsage = 0;
        var timeout = 5000;

        var input = expression;

        Lua lua = new Lua();

        lua.State.SetAllocFunction(((ud, ptr, osize, nsize) =>
        {
            currentMemoryUsage += (long)nsize;

            Log.Verbose("[LUA] MEMORY ALLOCATION: {0} bytes width: {1}",
                currentMemoryUsage, (int)nsize.ToUInt32());

            if (currentMemoryUsage > LuaExecutor.MEMORY_LIMIT)
            {
                return IntPtr.Zero;
            }

            return ptr != IntPtr.Zero && nsize.ToUInt32() > 0 ?
            Marshal.ReAllocHGlobal(ptr, unchecked((IntPtr)(long)(ulong)nsize)) :
            Marshal.AllocHGlobal((int)nsize.ToUInt32());
        }), ref pointer);
        lua.State.Encoding = Encoding.UTF8;


        // block danger functions
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


        var task = Task.Run(async () =>
        {
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
                lua.Close();
                lua.Dispose();
            }
        });

        if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
        {
            try
            {
                lua.State.Error("too long run time (10 seconds)");
                lua.State.Close();
                lua.State.Dispose();
            }
            catch (SEHException)
            {
                await Context.SendMessage($"Превышено время работы скрипта!");
            }
        }
    }
}
