using fs24bot3.Core;
using fs24bot3.Models;
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
            await Context.SendMessage(Context.Channel, string.Format("fs24_bot3 Версия: 10.04.2021 | .NET Core: {0} Система: {1}",
                Environment.Version.ToString(), os.VersionString));
        }

        [Command("rsgame")]
        [Checks.CheckAdmin]
        public async Task ResetGame()
        {
            Shop.SongameString = "";
            await Context.SendMessage(Context.Channel, "Игра перезагружена!");
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
            foreach (var item in Shop.ShopItems)
            {
                user.AddItemToInv(item.Slug, 1);
            }
            await Context.SendMessage(Context.Channel, "Вы выдали себе все предметы!");
        }

        [Command("updconfig")]
        [Checks.CheckAdmin]
        [Description("Обновление файла конфигурации")]
        public async Task LoadConfig()
        {
            try
            {
                Configuration.LoadConfiguration();
                await Context.SendMessage(Context.Channel, "Конфигурация успешно загружена");
            }
            catch (Exception e)
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Конфигурацию не удалось загрузить {e.Message}");
            }
        }

        [Command("delalluserrods")]
        [Checks.CheckAdmin]
        public async Task RemoveAllRods()
        {
            Context.BotCtx.Connection.Execute("DROP TABLE UserFishingRods");
            Context.BotCtx.Connection.CreateTable<SQL.UserFishingRods>();
            await Context.SendMessage(Context.Channel, "Удочки у пользователей удалены!");
        }

        [Command("give")]
        [Checks.CheckAdmin]
        public async Task Give(string username, string item, int count)
        {
            User sql = new User(username, Context.BotCtx.Connection);

            sql.AddItemToInv(item, count);
            await Context.SendMessage(Context.Channel, "Вы добавили предмет: " + Shop.GetItem(item).Name + " пользователю " + username);
        }

        [Command("joinch")]
        [Checks.CheckAdmin]
        public async Task JoinChannel(string channel)
        {
            await Context.SendMessage(Context.Channel, $"Зашел на: {channel}");
            await Context.Client.SendRaw("JOIN " + channel);
            await Context.SendMessage(channel, $"Всем перепривет с вами {Configuration.name}");
        }


        [Command("partch")]
        [Checks.CheckAdmin]
        public async Task PartChannel(string channel)
        {
            await Context.SendMessage(channel, "Простите я ухожу, всем пока...");
            await Context.Client.SendRaw("PART " + channel);
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
        [Description("Управление командами, параметр `command` вводить без @")]
        [Checks.CheckAdmin]
        public async Task CommandMgmt(CommandToggles.Switch action, string command)
        {
            var cmdHandle = Service.GetAllCommands().Where(x => x.Aliases.Where(al => al == command).Any()).FirstOrDefault();

            if (cmdHandle == null)
            {
                Context.SendErrorMessage(Context.Channel, $"Команда @{command} не найдена!");
                return;
            }

            switch (action)
            {
                case CommandToggles.Switch.Enable:
                    cmdHandle.Enable();
                    await Context.SendMessage(Context.Channel, $"Команда {cmdHandle.Name} {IrcColors.Green}ВКЛЮЧЕНА!");
                    break;
                case CommandToggles.Switch.Disable:
                    cmdHandle.Disable();
                    await Context.SendMessage(Context.Channel, $"Команда {cmdHandle.Name} {IrcColors.Red}ВЫКЛЮЧЕНА!");
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
                    await Context.SendMessage(Context.Channel, $"Модуль {modHandle.Name} {IrcColors.Green}ВКЛЮЧЕН!");
                    break;
                case CommandToggles.Switch.Disable:
                    modHandle.Disable();
                    await Context.SendMessage(Context.Channel, $"Модуль {modHandle.Name} {IrcColors.Red}ВЫКЛЮЧЕН!");
                    break;
            }
        }

        [Command("mods", "modules")]
        public async Task Mods()
        {
            await Context.SendMessage(Context.Channel, $"{IrcColors.Red}█{IrcColors.Reset} Выключен {IrcColors.Green}█{IrcColors.Reset} Включен. Число в скобках: количество команд в модуле");
            await Context.SendMessage(Context.Channel, $"В данный момент загружено: {Service.GetAllModules().Count} модулей");
            string modi = string.Join(" ", Service.GetAllModules()
                .Select(x => $"{(x.IsEnabled ? IrcColors.Green : IrcColors.Red)}{x.Name}{IrcColors.Reset}({x.Commands.Count()})"));
            await Context.SendMessage(Context.Channel, modi);
        }

        [Command("setcap")]
        [Checks.CheckAdmin]
        public async Task Cap(int cap)
        {
            Shop.MaxCap = cap;
            await Context.SendMessage(Context.Channel, "Установлен лимит невыплаты при: " + Shop.MaxCap);
        }

        [Command("tickrate")]
        [Checks.CheckAdmin]
        public async Task Tickrate(int speed = 5000)
        {
            Shop.Tickrate = speed;
            await Context.SendMessage(Context.Channel, "Установлен тикрейт (мс): " + Shop.Tickrate);
        }


        [Command("loggerlevel")]
        [Checks.CheckAdmin]
        public async Task LoggerLevel(string level = "Verbose")
        {
            Enum.TryParse(level, out LogEventLevel lvlToSet);
            Configuration.LoggerSw.MinimumLevel = lvlToSet;
            await Context.SendMessage(Context.Channel, $"Установлен уровень лога `{level}`");
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
