using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.Properties;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class SystemCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("info", "about", "credits")]
        [Description("Информация о боте")]
        public async Task Version()
        {
            var os = Environment.OSVersion;
            await Context.SendMessage(Context.Channel, string.Format("fs24_bot3 by @140bpmdubstep | .NET Core: {0} Система: {1}",
                Environment.Version.ToString(), os.VersionString));
        }

        [Command("rsgame")]
        [Checks.CheckAdmin]
        public async Task ResetGame()
        {
            Context.BotCtx.SongGame = new BotSystems.Songame(Context.BotCtx.Connection);
            await Context.SendMessage(Context.Channel, "Игра перезагружена!");
        }

        [Command("printstopwords")]
        public async Task PrintStopWords()
        {
            Log.Information("{0}", Resources.stopwords.Replace("\n", ", "));
            await Context.SendMessage(Context.Channel, "Посмотрите в консоль");
        }

        [Command("fixcommand")]
        [Checks.CheckAdmin]
        public async Task FixCommands()
        {
            Context.BotCtx.Connection.Execute("UPDATE CustomUserCommands SET Command = REPLACE(Command, '@', '')");
            await Context.SendMessage(Context.Channel, "Команды починены");
        }

        [Command("toggleppc")]
        [Checks.CheckAdmin]
        public async Task TooglePpc()
        {
            Transalator.AlloPpc = !Transalator.AlloPpc;
            await Context.SendMessage(Context.Channel, "Включить ппц всех команд: " + Transalator.AlloPpc);
        }

        [Command("quit", "exit")]
        [Checks.CheckAdmin]
        [Description("Выход")]
        public void Exit()
        {
            Environment.Exit(0);
        }

        [Command("gc")]
        [Checks.CheckAdmin]
        [Description("Вывоз мусора")]
        public async Task CollectGarbage()
        {
            GC.Collect();
            await Context.SendMessage(Context.Channel, "Мусор вывезли!");
        }


        [Command("giveall")]
        [Checks.CheckAdmin]
        public async Task Giveall(string username)
        {
            var user = new User(username, Context.BotCtx.Connection);
            foreach (var item in Context.BotCtx.Shop.Items)
            {
                user.AddItemToInv(Context.BotCtx.Shop, item.Key, 1);
            }
            await Context.SendMessage(Context.Channel, "Вы выдали себе все предметы!");
        }

        [Command("loggerlevel")]
        [Checks.CheckAdmin]
        public async Task LoggerLevel(LogEventLevel level = LogEventLevel.Verbose)
        {
            ConfigurationProvider.LoggerSw.MinimumLevel = level;
            await Context.SendMessage(Context.Channel, $"Установлен уровень лога `{level}`");
        }

        [Command("updconfig")]
        [Checks.CheckAdmin]
        [Description("Обновление файла конфигурации")]
        public async Task LoadConfig()
        {
            try
            {
                ConfigurationProvider.LoadConfiguration();
                await Context.SendMessage(Context.Channel, "Конфигурация успешно загружена");
            }
            catch (Exception e)
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}Конфигурацию не удалось загрузить {e.Message}");
            }
        }

        [Command("give")]
        [Checks.CheckAdmin]
        public async Task Give(string username, string item, int count)
        {
            User sql = new User(username, Context.BotCtx.Connection);

            sql.AddItemToInv(Context.BotCtx.Shop, item, count);
            await Context.SendMessage(Context.Channel, "Вы добавили предмет: " + Context.BotCtx.Shop.Items[item].Name + " пользователю " + username);
        }

        [Command("joinch")]
        [Checks.CheckAdmin]
        public async Task JoinChannel(string channel)
        {
            await Context.SendMessage(Context.Channel, $"Зашел на: {channel}");
            await Context.BotCtx.BotClient.SendRaw("JOIN " + channel);
            await Context.SendMessage(channel, $"Всем перепривет с вами {Context.BotCtx.Name}");
        }


        [Command("partch")]
        [Checks.CheckAdmin]
        public async Task PartChannel(string channel)
        {
            await Context.SendMessage(channel, "Простите я ухожу, всем пока...");
            await Context.BotCtx.BotClient.SendRaw("PART " + channel);
            await Context.SendMessage(Context.Channel, $"Вышел из: {channel}");
        }

        [Command("xp")]
        [Checks.CheckAdmin]
        public async Task GiveXp(string username, int count)
        {
            User sql = new User(username, Context.BotCtx.Connection);

            sql.IncreaseXp(count);
            await Context.SendMessage(Context.Channel, "Вы установили " + count + " xp пользователю " + username);
        }


        [Command("restartdb", "reinitdb", "refresh", "db")]
        [Checks.CheckAdmin]
        public async Task ReinitDb()
        {
            Core.Database.InitDatabase(Context.BotCtx.Connection);
            await Context.SendMessage(Context.Channel, "База данных загружена!");
        }

        [Command("level")]
        [Checks.CheckAdmin]
        public async Task GiveLevel(string username, int count)
        {
            User sql = new User(username, Context.BotCtx.Connection);

            sql.SetLevel(count);
            await Context.SendMessage(Context.Channel, "Вы установили уровень: " + count + " пользователю " + username);
        }


        [Command("command", "cmd")]
        [Description("Управление копоандами, параметр `command` вводить без")]
        [Checks.CheckAdmin]
        public async Task CommandMgmt(CommandToggles.Switch action, string command)
        {
            var cmdHandle = Service.GetAllCommands().Where(x => x.Aliases.Where(al => al == command).Any()).FirstOrDefault();

            if (cmdHandle == null)
            {
                Context.SendErrorMessage(Context.Channel, $"Команда {Context.User.GetUserPrefix()}{command} не найдена!");
                return;
            }

            switch (action)
            {
                case CommandToggles.Switch.Enable:
                    cmdHandle.Enable();
                    await Context.SendMessage(Context.Channel, $"Команда {cmdHandle.Name} {IrcClrs.Green}ВКЛЮЧЕНА!");
                    break;
                case CommandToggles.Switch.Disable:
                    cmdHandle.Disable();
                    await Context.SendMessage(Context.Channel, $"Команда {cmdHandle.Name} {IrcClrs.Red}ВЫКЛЮЧЕНА!");
                    break;
            }
        }


        [Command("mod", "module")]
        [Description("Управление модулями")]
        [Checks.CheckAdmin]
        public async Task ModMgmt(CommandToggles.Switch action, string module)
        {
            var modHandle = Service.GetAllModules().Where(x => x.Name.ToLower() == module.ToLower()).FirstOrDefault();

            if (modHandle == null)
            {
                Context.SendErrorMessage(Context.Channel, $"Модуль {module} не найден!");
                return;
            }

            switch (action)
            {
                case CommandToggles.Switch.Enable:
                    modHandle.Enable();
                    await Context.SendMessage(Context.Channel, $"Модуль {modHandle.Name} {IrcClrs.Green}ВКЛЮЧЕН!");
                    break;
                case CommandToggles.Switch.Disable:
                    modHandle.Disable();
                    await Context.SendMessage(Context.Channel, $"Модуль {modHandle.Name} {IrcClrs.Red}ВЫКЛЮЧЕН!");
                    break;
            }
        }

        [Command("mods", "modules")]
        public async Task Mods()
        {
            await Context.SendMessage(Context.Channel, $"{IrcClrs.Red}█{IrcClrs.Reset} Выключен {IrcClrs.Green}█{IrcClrs.Reset} Включен. Число в скобках: количество команд в модуле");
            await Context.SendMessage(Context.Channel, $"В данный момент загружено: {Service.GetAllModules().Count} модулей");
            string modi = string.Join(" ", Service.GetAllModules()
                .Select(x => $"{(x.IsEnabled ? IrcClrs.Green : IrcClrs.Red)}{x.Name}{IrcClrs.Reset}({x.Commands.Count()})"));
            await Context.SendMessage(Context.Channel, modi);
        }

        [Command("setprefix", "prefix", "pfx")]
        [Description("Устанавливает префикс, на который отвечает бот")]
        public async Task Prefix(string prefix = "#")
        {
            Context.User.SetUserPrefix(prefix);
            await Context.SendMessage(Context.Channel, $"{Context.Sender}: Вы установили себе префикс {prefix}! Теперь бот для вас будет отвечать на него!");
        }

        [Command("setcap")]
        [Checks.CheckAdmin]
        public async Task Cap(int cap)
        {
            Context.BotCtx.Shop.MaxCap = cap;
            await Context.SendMessage(Context.Channel, "Установлен лимит невыплаты при: " + Context.BotCtx.Shop.MaxCap);
        }

        [Command("tickrate")]
        [Checks.CheckAdmin]
        public async Task Tickrate(int speed = 5000)
        {
            Context.BotCtx.Tickrate = speed;
            await Context.SendMessage(Context.Channel, "Установлен тикрейт (мс): " + Context.BotCtx.Tickrate);
        }

        [Command("sqlt")]
        [Checks.CheckAdmin]
        public async Task LoggerLevel(bool enabled = true)
        {
            Context.BotCtx.Connection.Tracer = new Action<string>(q => { Log.Warning(q); });
            Context.BotCtx.Connection.Trace = enabled;
            await Context.SendMessage(Context.Channel, $"SQL логирование `{enabled}`");

        }

        [Command("delete")]
        [Checks.CheckAdmin]
        public async Task DeleteUser(string users, int level = 1)
        {
            if (users.Length > 0)
            {
                foreach (var user in users.Split(" "))
                {
                    Context.BotCtx.Connection.Execute("DELETE FROM UserStats WHERE Nick = ?", user);
                    Context.BotCtx.Connection.Execute("DELETE FROM Inventory WHERE Nick = ?", user);
                }
            }
            else
            {
                Context.BotCtx.Connection.Execute("DELETE FROM UserStats WHERE Level = ?", level);
            }
            Context.BotCtx.Connection.Execute("VACUUM;");
            await Context.SendMessage(Context.Channel, "Данные удалены!");
        }

        [Command("view")]
        [Checks.CheckAdmin]
        public async Task ViewUsers()
        {
            await Context.SendMessage(Context.Channel, String.Join(' ', Context.BotCtx.Connection.Table<SQL.UserStats>().Select(x => $"{x.Nick}({x.Level})")));
        }

        [Command("resetcache")]
        [Checks.CheckAdmin]
        public async Task ResetCache()
        {
            Context.BotCtx.Connection.Execute("DELETE FROM LyricsCache WHERE addedby IS NULL");
            await Context.SendMessage(Context.Channel, "Кэш песен УДАЛЕН НАВСЕГДА.....................");
        }

        [Command("insertlyrics", "inslyr")]
        public async Task InsertLyrics([Remainder] string song)
        {
            var data = song.Split(" - ");
            if (data.Length > 0)
            {
                try
                {
                    Core.Lyrics lyrics = new Core.Lyrics(data[0], data[1], Context.BotCtx.Connection);
                    await lyrics.GetLyrics();
                    await Context.SendMessage(Context.Channel, "Готово!");
                }
                catch (Exception e)
                {
                    Context.SendErrorMessage(Context.Channel, "Ошибка при получении слов: " + e.Message);
                }
            }
        }

        [Command("ignore")]
        [Checks.CheckAdmin]
        public async Task Ignore(CommandToggles.CommandEdit action, [Remainder] string username)
        {
            var usernames = username.Split(" ");
            switch (action)
            {
                case CommandToggles.CommandEdit.Add:
                    foreach (var item in usernames)
                    {
                        var ignr = new SQL.Ignore()
                        {
                            Username = item
                        };
                        Context.BotCtx.Connection.Insert(ignr);
                    }
                    await Context.SendMessage(Context.Channel, $"Пользователь(и) {username} добавлен(ы) в игнор!");
                    break;
                case CommandToggles.CommandEdit.Delete:
                    foreach (var item in usernames)
                    {
                        Context.BotCtx.Connection.Execute("DELETE FROM Ignore WHERE Username = ?", item);
                    }
                    await Context.SendMessage(Context.Channel, $"Пользователь(и) {username} удален(ы) из игнора!");
                    break;
            }
        }
    }
}
