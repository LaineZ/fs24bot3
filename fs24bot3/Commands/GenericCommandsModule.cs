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

namespace fs24bot3.Commands
{
    public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {
        public CommandService Service { get; set; }
        readonly HttpTools http = new HttpTools();

        [Command("help", "commands")]
        [Description("Список команд")]
        public async Task Help()
        {
            await Context.SendMessage(Context.Channel, "Генерация спика команд, подождите...");
            var cmds = Service.GetAllCommands();
            string commandsOutput = File.ReadAllText("static/help.html"); ;
            var customCommands = Context.BotCtx.Connection.Table<SQL.CustomUserCommands>().ToList();
            string commandList = string.Join('\n', Service.GetAllCommands().Select(x => $"<strong>@{x.Name}</strong> {string.Join(' ', x.Parameters)}</p><p class=\"desc\">{x.Description}</p><p>Требования: {string.Join(' ', x.Checks)}</p><hr>"));
            string customList = string.Join('\n', string.Join("\n", customCommands.Select(x => $"<p>{x.Command}</p>")));

            commandsOutput = commandsOutput.Replace("[CMDS]", commandList);
            commandsOutput = commandsOutput.Replace("[CUSTOMLIST]", customList);
            
            string link = await http.UploadToTrashbin(commandsOutput);
            await Context.SendMessage(Context.Channel, "Выложены команды по этой ссылке: " + link + " также вы можете написать @helpcmd имякоманды для получение дополнительной помощи");
        }

        [Command("helpcmd")]
        [Description("Помощь по команде")]
        public async Task HelpСmd(string command = "helpcmd")
        {
            foreach (Command cmd in Service.GetAllCommands())
            {
                if (cmd.Aliases.Contains(command))
                {
                    await Context.SendMessage(Context.Channel, "@" + cmd.Name + " " + string.Join(" ", cmd.Parameters.Select(x => $"[{x.Name} default: {x.DefaultValue}]")) + " - " + cmd.Description);
                    if (cmd.Remarks != null)
                    {
                        await Context.SendMessage(Context.Channel, IrcColors.Bold + cmd.Remarks);
                    }

                    await Context.SendMessage(Context.Channel, $"{IrcColors.Bold}Алиасы: {IrcColors.Reset}{String.Join(", ", cmd.Aliases)}");
                    break;
                }
            }
        }

        [Command("remind", "in")]
        [Description("Напоминание. time вводится в формате 1m;30s (1 минута и 30 секунд = 90 секунд)")]
        public async Task Remind(string time = "1m", [Remainder] string message = "Remind")
        {
            // sorry for this idk how to make more coolest code!!!!
            double totalSecs = 0;
            foreach (var part in time.Split(';'))
            {
                switch (part[^1])
                {
                    case 'y':
                        totalSecs += 31556926 * uint.Parse(part.Trim('y'));
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
            await Context.SendMessage(Context.Channel, $"{message} через ({time})!");
        }

        [Command("reminds", "rems")]
        [Description("Список напоминаний")]
        public async Task Reminds(string username = "", string locale = "ru-RU")
        {
            if (string.IsNullOrEmpty(username))
            {
                username = Context.Sender;
            }
            var reminds = new User(username, Context.BotCtx.Connection).GetReminds();

            if (!reminds.Any())
            {
                Context.SendSadMessage(Context.Channel, $"У пользователя {username} нет напоминаний!");
                return;
            }

            string rems = string.Empty;

            foreach (var remind in reminds)
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                CultureInfo rus = new CultureInfo(locale, false);
                dtDateTime = dtDateTime.AddSeconds(remind.RemindDate).ToLocalTime();

                rems += $"{IrcColors.Bold}Напоминание {username}: {IrcColors.Reset}\"{remind.Message}\" в {dtDateTime.ToString(rus)}\n";
            }

            await Context.SendMessage(Context.Channel, rems);
        }

        [Command("songame", "songg", "sg")]
        [Description("Игра-перевод песен: введите по русски так чтобы получилось ...")]
        public async Task Songame([Remainder] string translated = "")
        {
            var user = new User(Context.Sender, Context.BotCtx.Connection, Context);

            if (Context.BotCtx.SongGame.SongameString.Length <= 0)
            {
                Context.SendErrorMessage(Context.Channel, "Не удалось найти нормальную строку песни... Может попробуем поискать что-нибудь с помощью @lyrics?");
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
                        var translatedOutput = await Core.Transalator.Translate(translated, "ru", "en");
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
                    names.Add(MessageUtils.GenerateName(Math.Clamp(maxlen, 5, 20)));
                }
                else
                {
                    names.Add(MessageUtils.GenerateNameRus(Math.Clamp(maxlen, 5, 20)));
                }
            }
            await Context.SendMessage(Context.Channel, string.Join(",", names));
        }

        [Command("midi")]
        [Description("Миди ноты")]
        public async Task Midi(string note, int oct = 4)
        {
            string[] noteString = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

            if (uint.TryParse(note, out uint initialNote))
            {
                int octave = (int)(initialNote / 12) - 1;
                uint noteIndex = initialNote % 12;
                string noteName = noteString[noteIndex];
                await Context.SendMessage(Context.Channel, $"MIDI: {note} = {IrcColors.Reset}{noteName}{octave}");
            }
            else
            {
                for (int i = 0; i < noteString.Length; i++)
                {
                    if (noteString[i].ToLower() == note.ToLower())
                    {
                        int noteIndex = (12 * (oct + 1)) + i;
                        await Context.SendMessage(Context.Channel, $"{note}{oct} = MIDI: {IrcColors.Reset}{noteIndex}");
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

                        await Context.SendMessage(Context.Channel, $"Тег 00,{ircolor}⚫{tagname}{IrcColors.Reset} успешно добавлен!");
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
                        await Context.SendMessage(Context.Channel, IrcColors.Gray + $"Тег создал {tagDel.GetTagByName().Username} а не {Context.Sender}");
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
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ :(");
            }
        }

        [Command("seen")]
        [Description("Когда последний раз пользователь писал сообщения")]
        public async Task LastSeen(string destination)
        {
            var user = new User(destination, Context.BotCtx.Connection);
            TimeSpan date = DateTime.Now.Subtract(user.GetLastMessage());
            if (date.Days < 1000)
            {
                await Context.SendMessage(Context.Channel, $"Последний раз я видел {destination} {date.Days} дн. {date.Hours} час. {date.Minutes} мин. {date.Seconds} сек. назад");
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"Я уже не помню как выглядит {destination}... Даже не помню когда я его видел");
            }

        }

        [Command("tags")]
        [Description("Список всех тегов")]
        public async Task AllTags()
        {
            List<SQL.Tag> tags = new List<SQL.Tag>();
            var query = Context.BotCtx.Connection.Table<SQL.Tag>();
            foreach (var tag in query)
            {
                tags.Add(tag);
            }
            await Context.SendMessage(Context.Channel, string.Join(' ', tags.Select(x => $"{x.Color},00⚫{x.TagName}{IrcColors.Reset}")));
        }

        [Command("rndl", "randomlyrics")]
        [Description("Рандомная песня")]
        public async Task RandomSong()
        {
            var query = Context.BotCtx.Connection.Table<SQL.LyricsCache>().ToList();

            if (query.Count > 0)
            {
                Random rand = new Random();
                string[] lyrics = query[rand.Next(0, query.Count - 1)].Lyrics.Split("\n");
                int baseoffset = rand.Next(0, lyrics.Length - 1);
                string outputmsg = "";

                for (int i = 0; i < rand.Next(1, 5); i++)
                {
                    if (lyrics.Length > baseoffset + i) { outputmsg += " " + lyrics[baseoffset + i].Trim(); }
                }

                await Context.SendMessage(Context.Channel, outputmsg);
            }
        }
    }
}
