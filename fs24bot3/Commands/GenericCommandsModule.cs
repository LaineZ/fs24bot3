using fs24bot3.Models;
using Qmmands;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace fs24bot3.Commands
{
    public sealed class GenericCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {
        public CommandService Service { get; set; }

        readonly HttpTools http = new HttpTools();


        private string RemoveArticles(string line)
        {
            string[] art = new string[] { "the", "are", "a", "an", "i" };
            foreach (string word in art)
            {
                Regex regexArticle = new Regex(@"\b" + word + @"\b");
                line = regexArticle.Replace(line.ToLower(), " ");
            }

            // remove double spaces
            Regex regex = new Regex("[ ]{2,}");
            return new string(regex.Replace(line.Trim(), " ").ToCharArray().Where(c => !char.IsPunctuation(c)).ToArray());
        }

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
        public void HelpСmd(string command = "helpcmd")
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

        [Command("remind", "in")]
        [Description("Напоминание. time вводится в формате 1m;30s (1 минута и 30 секунд = 90 секунд)")]
        public void Remind(string time = "1m", [Remainder] string message = "Remind")
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
            var user = new User(Context.Message.From, Context.Connection);
            user.AddRemind(ts, message);
            Context.SendMessage(Context.Channel, $"{message} через ({time})!");
        }

        [Command("reminds", "ins", "rems")]
        [Description("Список напоминаний")]
        public void Reminds(string username = "")
        {
            if (string.IsNullOrEmpty(username))
            {
                username = Context.Message.From;
            }
            var reminds = Context.Connection.Table<SQL.Reminds>().Where(x => x.Nick == username).Take(5);

            if (!reminds.Any())
            {
                Context.SendSadMessage(Context.Channel, $"У пользователя {username} нет напоминаний!");
            }
            foreach (var remind in reminds)
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(remind.RemindDate).ToLocalTime();

                Context.SendMessage(Context.Channel, $"Напоминание {username}: {remind.Message} в {dtDateTime}");
            }
        }

        [Command("songame", "songg", "sg")]
        [Description("Игра-перевод песен: введите по русски так чтобы получилось ...")]
        public async void Songame([Remainder] string translated = "")
        {
            var user = new User(Context.Message.From, Context.Connection, Context);
            int timeout = 10;

            if (Shop.SongameTries <= 0)
            {
                Context.SendMessage(Context.Channel, $"ВЫ ПРОИГРАЛИ!!!! ПЕРЕЗАГРУЗКА!!!!");
                Shop.SongameString = "";
                Shop.SongameTries = 5;
                user.RemItemFromInv("money", 1000);
                return;
            }
            Random rand = new Random();
            List<SQL.LyricsCache> query = Context.Connection.Query<SQL.LyricsCache>("SELECT * FROM LyricsCache");

            if (Shop.SongameString.Length == 0)
            {
                while (Shop.SongameString.Length == 0 && timeout > 0)
                {
                    if (query.Count > 0)
                    {
                        string[] lyrics = query[rand.Next(0, query.Count - 1)].Lyrics.Split("\n");

                        foreach (string line in lyrics)
                        {
                            if (Regex.IsMatch(line, @"^([A-Za-z\s]*)$"))
                            {
                                Shop.SongameString = RemoveArticles(line);
                                break;
                            }
                        }

                    }
                    Shop.SongameTries = 5;
                    timeout--;
                }
            }

            if (timeout <= 0)
            {
                Context.SendErrorMessage(Context.Channel, "Не удалось найти нормальную строку песни... Может попробуем поискать что-нибудь с помощью @lyrics?");
                return;
            }

            if (translated.Length == 0)
            {
                Context.SendMessage(Context.Channel, $"Введи на русском так чтобы получилось: {Shop.SongameString} попыток: {Shop.SongameTries}");
            }
            else
            {
                if (!Regex.IsMatch(translated, @"([A-Za-z])"))
                {
                    var translatedOutput = await Core.Transalator.Translate(translated, "ru", "en");

                    string trOutFixed = RemoveArticles(translatedOutput.text.ToString());

                    if (trOutFixed == Shop.SongameString)
                    {
                        int reward = 450 * Shop.SongameTries;
                        user.AddItemToInv("money", reward);
                        Context.SendMessage(Context.Channel, $"ВЫ УГАДАЛИ И ВЫИГРАЛИ {reward} ДЕНЕГ!");
                        // reset the game
                        Shop.SongameString = "";
                    }
                    else
                    {
                        Context.SendMessage(Context.Channel, $"Неправильно, ожидалось | получилось: {Shop.SongameString} | {trOutFixed}");
                        Shop.SongameTries--;
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, "Обнаружен английский язык!!!");
                }
            }
        }

        [Command("genname")]
        [Description("Генератор имен")]
        public void GenName(bool isRussian = false, int maxlen = 10, uint count = 10)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < Math.Clamp(count, 1, 10); i++)
            {
                if (!isRussian)
                {
                    names.Add(Core.MessageUtils.GenerateName(Math.Clamp(maxlen, 5, 20)));
                }
                else
                {
                    names.Add(Core.MessageUtils.GenerateNameRus(Math.Clamp(maxlen, 5, 20)));
                }
            }
            Context.SendMessage(Context.Channel, string.Join(",", names));
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

        [Command("tag")]
        [Description("Управление тегами: параметр action: add/del")]
        [Remarks("Параметр action отвечает за действие команды:\nadd - добавить тег\ndelete - удалить тег. Параметр ircolor представляет собой код IRC цвета, его можно узнать например с помощью команды .colors (brote@irc.esper.net)")]
        public void AddTag(CommandToggles.CommandEdit action, string tagname, int ircolor = 1)
        {
            var user = new User(Context.Message.From, Context.Connection);

            switch (action)
            {
                case CommandToggles.CommandEdit.Add:
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
                case CommandToggles.CommandEdit.Delete:
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
            }
        }

        [Command("addtag")]
        [Description("Добавить тег пользователю")]
        public void InsertTag(string tagname, string destination)
        {
            var user = new User(destination, Context.Connection);

            if (user.AddTag(tagname, 1))
            {
                Context.SendMessage(Context.Channel, $"Тег {tagname} добавлен пользователю {destination}");
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}НЕ ПОЛУЧИЛОСЬ :(");
            }
        }

        [Command("seen")]
        [Description("Когда последний раз пользователь писал сообщени")]
        public void LastSeen(string destination)
        {
            var user = new User(destination, Context.Connection);
            TimeSpan date = DateTime.Now.Subtract(user.GetLastMessage());
            if (date.Days < 1000)
            {
                Context.SendMessage(Context.Channel, $"Последний раз я видел {destination} {date.Days} дн. {date.Hours} час. {date.Minutes} мин. {date.Seconds} сек. назад");
            }
            else
            {
                Context.SendMessage(Context.Channel, $"Я уже не помню как выглядит {destination}... Даже не помню когда я его видел");
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

        [Command("rndl", "randomlyrics")]
        [Description("Рандомная песня")]
        public void RandomSong()
        {
            var query = Context.Connection.Table<SQL.LyricsCache>().ToList();

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

                Context.SendMessage(Context.Channel, outputmsg);
            }
        }
    }
}
