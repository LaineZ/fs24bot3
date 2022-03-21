using fs24bot3.Models;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using fs24bot3.QmmandsProcessors;
using fs24bot3.Core;
using System.Globalization;
using fs24bot3.BotSystems;
using fs24bot3.Helpers;
using fs24bot3.Properties;

namespace fs24bot3.Commands
{
    public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {
        public CommandService Service { get; set; }
        readonly HttpTools http = new HttpTools();


        private string ToReadableString(TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} дн. ", span.Days) : string.Empty,
                span.Duration().Hours > 0 ? string.Format("{0:0} ч. ", span.Hours) : string.Empty,
                span.Duration().Minutes > 0 ? string.Format("{0:0} мин. ", span.Minutes) : string.Empty,
                span.Duration().Seconds > 0 ? string.Format("{0:0} сек.", span.Seconds) : string.Empty);
            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }

        [Command("help", "commands")]
        [Description("Список команд")]
        public async Task Help()
        {
            var prefix = Context.User.GetUserPrefix();

            await Context.SendMessage(Context.Channel, "Генерация спика команд, подождите...");
            var cmds = Service.GetAllCommands();
            string commandsOutput = Resources.help;
            var customCommands = Context.BotCtx.Connection.Query<SQL.CustomUserCommands>("SELECT * FROM CustomUserCommands ORDER BY length(Output) DESC");
            string commandList = string.Join('\n', Service.GetAllCommands().Select(x => $"<strong>{prefix}{x.Name}</strong> {string.Join(' ', x.Parameters)}</p><p class=\"desc\">{x.Description}</p><p>Требования: {string.Join(' ', x.Checks)}</p><hr>"));
            string customList = string.Join('\n', string.Join("\n", customCommands.Select(x => $"<p>{prefix}{x.Command} Создал: <strong>{x.Nick}</strong> Lua: {x.IsLua == 1} </p>")));

            commandsOutput = commandsOutput.Replace("[CMDS]", commandList);
            commandsOutput = commandsOutput.Replace("[CUSTOMLIST]", customList);
            
            string link = await InternetServicesHelper.UploadToTrashbin(commandsOutput);
            await Context.SendMessage(Context.Channel, $"Выложены команды по этой ссылке: {link} также вы можете написать {prefix}helpcmd имякоманды для получение дополнительной помощи");
        }

        [Command("helpcmd")]
        [Description("Помощь по команде")]
        public async Task HelpСmd(string command = "helpcmd")
        {
            foreach (Command cmd in Service.GetAllCommands())
            {
                if (cmd.Aliases.Contains(command))
                {
                    await Context.SendMessage(Context.Channel, Context.User.GetUserPrefix() + cmd.Name + " " + string.Join(" ", cmd.Parameters.Select(x => $"[{x.Name} default: {x.DefaultValue}]")) + " - " + cmd.Description);
                    if (cmd.Remarks != null)
                    {
                        await Context.SendMessage(Context.Channel, IrcClrs.Bold + cmd.Remarks);
                    }

                    await Context.SendMessage(Context.Channel, $"{IrcClrs.Bold}Алиасы: {IrcClrs.Reset}{String.Join(", ", cmd.Aliases)}");
                    return;
                }
            }

            Context.SendSadMessage(Context.Channel, $"К сожалению команда не найдена, если вы пытаетесь найти кастом команду: используйте {Context.User.GetUserPrefix()}cmdinfo");
        }

        [Command("remind", "in")]
        [Description("Напоминание. time вводится в формате 1m;30s (1 минута и 30 секунд = 90 секунд)")]
        public async Task Remind(string time = "1m", [Remainder] string message = "Remind")
        {
            double totalSecs = 0;
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

            TimeSpan ts = TimeSpan.FromSeconds(totalSecs);
            var user = new User(Context.Sender, Context.BotCtx.Connection);
            user.AddRemind(ts, message);
            await Context.SendMessage(Context.Channel, $"{message} через {ToReadableString(ts)}");
        }

        [Command("time")]
        [Description("Время")]
        public async Task UserTime(string username = "")
        {
            if (string.IsNullOrEmpty(username))
            {
                username = Context.Sender;
            }

            var usr = new User(username, Context.BotCtx.Connection);
            var timezone = usr.GetTimeZone();
            var time = DateTime.Now.ToUniversalTime();

            await Context.SendMessage($"Сейчас у {username} {TimeZoneInfo.ConvertTimeFromUtc(time, timezone)} {timezone.DisplayName}");
        }

        [Command("reminds", "rems")]
        [Description("Список напоминаний")]
        public async Task Reminds(string username = "", string locale = "ru-RU")
        {
            if (string.IsNullOrEmpty(username))
            {
                username = Context.Sender;
            }

            var usr = new User(username, Context.BotCtx.Connection);

            var reminds = usr.GetReminds();
            var timezone = usr.GetTimeZone();

            if (!reminds.Any())
            {
                Context.SendSadMessage(Context.Channel, $"У пользователя {username} нет напоминаний!");
                return;
            }

            string rems = string.Empty;

            foreach (var remind in reminds)
            {
                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                CultureInfo rus = new CultureInfo(locale, false);
                dt = dt.AddSeconds(remind.RemindDate).ToUniversalTime();
                var dtDateTime = TimeZoneInfo.ConvertTimeFromUtc(dt, timezone);

                rems += $"{IrcClrs.Bold}Напоминание {username}: {IrcClrs.Reset}\"{remind.Message}\" в {IrcClrs.Bold}{dtDateTime.ToString(rus)} {timezone.DisplayName} {IrcClrs.Reset}или через {IrcClrs.Blue}{ToReadableString(dtDateTime.Subtract(DateTime.UtcNow))}\n";
            }

            await Context.SendMessage(Context.Channel, rems);
        }


        [Command("warnings", "warns")]
        public async Task GetWarns()
        {
            var warns = Context.User.GetWarnings();
            var warnsStr = string.Empty;

            if (!warns.Any())
            {
                Context.SendSadMessage(Context.Channel, $"У вас нет предупреждений!");
                return;
            }
            foreach (var warn in warns)
            {
                warnsStr += $"{Context.Sender}: {warn.Message}\n";
            }

            await Context.SendMessage(Context.Channel, warnsStr);
        }

        [Command("songame", "songg", "sg")]
        [Description("Игра-перевод песен: введите по русски так чтобы получилось ...")]
        public async Task Songame([Remainder] string translated = "")
        {
            var user = new User(Context.Sender, Context.BotCtx.Connection, Context);

            if (Context.BotCtx.SongGame.SongameString.Length <= 0)
            {
                Context.SendErrorMessage(Context.Channel, $"Не удалось найти нормальную строку песни... Может попробуем поискать что-нибудь с помощью {Context.User.GetUserPrefix()}lyrics?");
                return;
            }

            if (Context.BotCtx.SongGame.Tries <= 0)
            {
                await Context.SendMessage(Context.Channel, $"ВЫ ПРОИГРАЛИ!!!! ПЕРЕЗАГРУЗКА!!!!");
                await user.RemItemFromInv(Context.BotCtx.Shop, "money", 1000);
                Context.BotCtx.SongGame = new Songame(Context.BotCtx.Connection);
                return;
            }

            if (translated.Length == 0)
            {
                await Context.SendMessage(Context.Channel, $"Введи на русском так чтобы получилось: {Context.BotCtx.SongGame.SongameString} попыток: {Context.BotCtx.SongGame.Tries}");
            }
            else
            {
                if (!Regex.IsMatch(translated, @"([A-Za-z])"))
                {
                    try
                    {
                        var translatedOutput = Transalator.TranslateBing(translated, "ru", "en").Result.translations.First().text;
                        string trOutFixed = Context.BotCtx.SongGame.RemoveArticles(translatedOutput);

                        if (trOutFixed == Context.BotCtx.SongGame.SongameString)
                        {
                            int reward = 450 * Context.BotCtx.SongGame.Tries;
                            user.AddItemToInv(Context.BotCtx.Shop, "money", reward);
                            await Context.SendMessage(Context.Channel, $"ВЫ УГАДАЛИ И ВЫИГРАЛИ {reward} ДЕНЕГ!");
                        }
                        else
                        {
                            await Context.SendMessage(Context.Channel, $"Неправильно, ожидалось | получилось: {Context.BotCtx.SongGame.SongameString} | {trOutFixed} // у вас осталось {Context.BotCtx.SongGame.Tries} попыток!");
                            Context.BotCtx.SongGame.Tries--;
                        }
                    }
                    catch (FormatException)
                    {
                        Context.SendErrorMessage(Context.Channel, "К сожалению, в данный момент игра недоступна...");
                    }
                }
                else
                {
                    await Context.SendMessage(Context.Channel, "Обнаружен английский язык!!!");
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
                await Context.SendMessage(Context.Channel, $"MIDI: {note} = {IrcClrs.Reset}{noteName}{octave}");
            }
            else
            {
                for (int i = 0; i < noteString.Length; i++)
                {
                    if (noteString[i].ToLower() == note.ToLower())
                    {
                        int noteIndex = (12 * (oct + 1)) + i;
                        await Context.SendMessage(Context.Channel, $"{note}{oct} = MIDI: {IrcClrs.Reset}{noteIndex}");
                        break;
                    }
                }
            }
        }

        [Command("tag")]
        [Description("Управление тегами: параметр action: add/del")]
        [Remarks("Параметр action отвечает за действие команды:\nadd - добавить тег\ndelete - удалить тег. Параметр ircolor представляет собой код IRC цвета.")]
        public async Task AddTag(CommandToggles.CommandEdit action, string tagname, int ircolor = 1)
        {
            var user = new User(Context.Sender, Context.BotCtx.Connection);

            switch (action)
            {
                case CommandToggles.CommandEdit.Add:
                    if (await user.RemItemFromInv(Context.BotCtx.Shop, "money", 1000))
                    {
                        var tag = new SQL.Tag()
                        {
                            TagName = tagname,
                            Color = ircolor.ToString(),
                            TagCount = 0,
                            Username = Context.Sender
                        };

                        Context.BotCtx.Connection.Insert(tag);

                        await Context.SendMessage(Context.Channel, $"Тег 00,{ircolor}⚫{tagname}{IrcClrs.Reset} успешно добавлен!");
                    }
                    else
                    {
                        Log.Information("Error occurred while adding!");
                    }
                    break;
                case CommandToggles.CommandEdit.Delete:
                    var tagDel = new TagsUtils(tagname, Context.BotCtx.Connection);
                    if (tagDel.GetTagByName().Username == Context.Sender)
                    {
                        Context.BotCtx.Connection.Execute("DELETE FROM Tag WHERE TagName = ?", tagname);
                        await Context.SendMessage(Context.Channel, "Тег " + tagname + " успешно удален!");
                    }
                    else
                    {
                        await Context.SendMessage(Context.Channel, IrcClrs.Gray + $"Тег создал {tagDel.GetTagByName().Username} а не {Context.Sender}");
                    }
                    break;
            }
        }

        [Command("addtag")]
        [Description("Добавить тег пользователю")]
        public async Task InsertTag(string tagname, string destination)
        {
            var user = new User(destination, Context.BotCtx.Connection);

            if (user.AddTag(tagname, 1))
            {
                await Context.SendMessage(Context.Channel, $"Тег {tagname} добавлен пользователю {destination}");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}НЕ ПОЛУЧИЛОСЬ :(");
            }
        }

        [Command("seen")]
        [Description("Когда последний раз пользователь писал сообщения")]
        public async Task LastSeen(string destination)
        {
            if (destination == Context.BotCtx.Name)
            {
                await Context.SendMessage(Context.Channel, "Я ЗДЕСЬ!");
                return;
            }

            var user = new User(destination, Context.BotCtx.Connection);
            TimeSpan date = DateTime.Now.Subtract(user.GetLastMessage());

            if (date.Days < 1000)
            {
                await Context.SendMessage(Context.Channel, $"Последний раз я видел {destination} {ToReadableString(date)} назад");
                var messages = await InternetServicesHelper.GetMessages(user.GetLastMessage());
                await Context.SendMessage(Context.Channel, $"Последнее сообщение от пользователя: {messages.Where(x => x.Nick == destination).FirstOrDefault().Message}");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"Я никогда не видел {destination}...");
            }

        }

        [Command("tags")]
        [Description("Список всех тегов")]
        public async Task AllTags()
        {
            var query = Context.BotCtx.Connection.Table<SQL.Tag>().ToList();
            await Context.SendMessage(Context.Channel, string.Join(' ', query.Select(x => $"00,{x.Color}⚫{x.TagName}{IrcClrs.Reset}")));
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
    }
}
