using fs24bot3.Models;
using Qmmands;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fs24bot3.Commands
{
    public sealed class SystemCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("info", "about", "credits")]
        [Description("Информация о боте")]
        public void Version()
        {
            var os = Environment.OSVersion;
            Context.SendMessage(Context.Channel, string.Format("fs24_bot3 Версия: 23.03.2021 | .NET Core: {0} Система: {1}",
                Environment.Version.ToString(), os.VersionString));
        }

        [Command("rsgame")]
        [Checks.CheckAdmin]
        public void ResetGame()
        {
            Shop.SongameString = "";
            Context.SendMessage(Context.Channel, "Игра перезагружена!");
        }


        [Command("hourstat")]
        [Description("Статистика за час")]
        public void Hourstat()
        {
            List<string> stopwords = new List<string>() { "что", "как", "все", "она", "так", "его", "только", "мне", 
                "было", "вот", "меня", "еще", "нет", "ему", "теперь", "когда", "даже", "вдруг", 
                "если", "уже", "или", "быть", "был", "него", "вас", "нибудь", "опять", "вам", 
                "ведь", "там", "потом", "себя", "ничего", "может", "они", "тут", "где", 
                "есть", "надо", "ней", "для", "мы", "тебя", "чем", "была", "сам", "чтоб", 
                "без", "будто", "чего", "раз", "тоже", "себе", "под", "будет", "тогда", "кто", 
                "этот", "это", "того", "потому", "этого", "какой", "совсем", "ним", "здесь", 
                "этом", "один", "почти", "мой", "тем", "чтобы", "нее", "сейчас", "были", 
                "куда", "зачем", "всех", "никогда", "можно", "при", 
                "наконец", "два", "другой", "хоть", "после", 
                "над", "больше", "тот", "через", "эти", "нас", "про", 
                "всего", "них", "какая", "много", "разве", "три", "эту", "моя", 
                "впрочем", "хорошо", "свою", "этой", "перед", "иногда", "лучше", "чуть", 
                "том", "нельзя", "такой", "более", "всегда", "конечно", "всю" };

            int messageCount = Context.Messages.Count;
            string mostActive = Context.Messages.GroupBy(msg => msg.From).OrderByDescending(grp => grp.Count())
                        .Select(grp => grp.Key).First();
            string concatedMessage = string.Join("\n", Context.Messages.Select(x => x.Message));
            string[] words = concatedMessage.Split(" ");
            string mostUsedword = words.Where(word => word.Length > 2 && !stopwords.Any(s => word.Equals(s))).GroupBy(word => word).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).FirstOrDefault() ?? "не знаю";
            Context.SendMessage(Context.Channel, $"Статистика за текущий час: Сообщений: {messageCount}, Слов: {words.Length}, Символов: {concatedMessage.Length}, Самый активный: {mostActive}, Возможная тема: {mostUsedword}");
        }

        [Command("quit", "exit")]
        [Checks.CheckAdmin]
        [Description("Выход")]
        public void Exit()
        {
            Environment.Exit(0);
        }


        [Command("me")]
        [Description("Макроэкономические показатели")]
        public void Economy()
        {
            Context.SendMessage(Context.Channel, $"Число зарплат: {Shop.PaydaysCount} Денежная масса: {Shop.GetItemAvg(Context.Connection)} Последнее время выполнения Shop.Update(): {Shop.TickSpeed.TotalMilliseconds} ms Период выполнения Shop.Update() {Shop.Tickrate} ms Покупок/Продаж {Shop.Buys}/{Shop.Sells}");
        }

        [Command("gc")]
        [Checks.CheckAdmin]
        [Description("Вывоз мусора")]
        public void CollectGarbage()
        {
            GC.Collect();
            Context.SendMessage(Context.Channel, "Мусор вывезли!");
        }


        [Command("giveall")]
        [Checks.CheckAdmin]
        public void Giveall(string username)
        {
            var user = new UserOperations(username, Context.Connection);
            foreach (var item in Shop.ShopItems)
            {
                user.AddItemToInv(item.Slug, 1);
            }
            Context.SendMessage(Context.Channel, "Вы выдали себе все предметы!");
        }

        [Command("updconfig")]
        [Checks.CheckAdmin]
        [Description("Обновление файла конфигурации")]
        public void LoadConfig()
        {
            try
            {
                Configuration.LoadConfiguration();
                Context.SendMessage(Context.Channel, "Конфигурация успешно загружена");
            }
            catch (Exception e)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Конфигурацию не удалось загрузить {e.Message}");
            }
        }

        [Command("delalluserrods")]
        [Checks.CheckAdmin]
        public void RemoveAllRods()
        {
            Context.Connection.Execute("DROP TABLE UserFishingRods");
            Context.Connection.CreateTable<SQL.UserFishingRods>();
            Context.SendMessage(Context.Channel, "Удочки у пользователей удалены!");
        }

        [Command("give")]
        [Checks.CheckAdmin]
        public void Give(string username, string item, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.AddItemToInv(item, count);
            Context.SendMessage(Context.Channel, "Вы добавили предмет: " + Shop.GetItem(item).Name + " пользователю " + username);
        }

        [Command("joinch")]
        [Checks.CheckAdmin]
        public async void JoinChannel(string channel)
        {
            Context.SendMessage(Context.Channel, $"Зашел на: {channel}");
            await Context.Client.SendRaw("JOIN " + channel);
            Context.SendMessage(channel, $"Всем перепривет с вами {Configuration.name}");
        }


        [Command("partch")]
        [Checks.CheckAdmin]
        public async void PartChannel(string channel)
        {
            Context.SendMessage(channel, "Простите я ухожу, всем пока...");
            await Context.Client.SendRaw("PART " + channel);
            Context.SendMessage(Context.Channel, $"Вышел из: {channel}");
        }

        [Command("xp")]
        [Checks.CheckAdmin]
        public void GiveXp(string username, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.IncreaseXp(count);
            Context.SendMessage(Context.Channel, "Вы установили " + count + " xp пользователю " + username);
        }


        [Command("restartdb", "reinitdb", "refresh", "db")]
        [Checks.CheckAdmin]
        public void ReinitDb()
        {
            Core.Database.InitDatabase(Context.Connection);
            Context.SendMessage(Context.Channel, "База данных загружена!");
        }

        [Command("level")]
        [Checks.CheckAdmin]
        public void GiveLevel(string username, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.SetLevel(count);
            Context.SendMessage(Context.Channel, "Вы установили уровень: " + count + " пользователю " + username);
        }

        [Command("setcap")]
        [Checks.CheckAdmin]
        public void Cap(int cap)
        {
            Shop.MaxCap = cap;
            Context.SendMessage(Context.Channel, "Установлен лимит невыплаты при: " + Shop.MaxCap);
        }

        [Command("tickrate")]
        [Checks.CheckAdmin]
        public void Tickrate(int speed = 5000)
        {
            Shop.Tickrate = speed;
            Context.SendMessage(Context.Channel, "Установлен тикрейт (мс): " + Shop.Tickrate);
        }


        [Command("loggerlevel")]
        [Checks.CheckAdmin]
        public void LoggerLevel(string level = "Verbose")
        {
            Enum.TryParse(level, out LogEventLevel lvlToSet);
            Configuration.LoggerSw.MinimumLevel = lvlToSet;
            Context.SendMessage(Context.Channel, $"Установлен уровень лога `{level}`");
        }

        [Command("sqlt")]
        [Checks.CheckAdmin]
        public void LoggerLevel(bool enabled = true)
        {
            Context.Connection.Tracer = new Action<string>(q => { Log.Warning(q); });
            Context.Connection.Trace = enabled;
            Context.SendMessage(Context.Channel, $"SQL логирование `{enabled}`");

        }

        [Command("delete")]
        [Checks.CheckAdmin]
        public void DeleteUser(string users, int level = 1)
        {
            if (users.Length > 0)
            {
                foreach (var user in users.Split(" "))
                {
                    Context.Connection.Execute("DELETE FROM UserStats WHERE Nick = ?", user);
                    Context.Connection.Execute("DELETE FROM Inventory WHERE Nick = ?", user);
                }
            }
            else
            {
                Context.Connection.Execute("DELETE FROM UserStats WHERE Level = ?", level);
            }
            Context.Connection.Execute("VACUUM;");
            Context.SendMessage(Context.Channel, "Данные удалены!");
        }

        [Command("view")]
        [Checks.CheckAdmin]
        public void ViewUsers()
        {
            Context.SendMessage(Context.Channel, String.Join(' ', Context.Connection.Table<SQL.UserStats>().Select(x => $"{x.Nick}({x.Level})")));
        }

        [Command("resetcache")]
        [Checks.CheckAdmin]
        public void ResetCache()
        {
            Context.Connection.Execute("DELETE FROM LyricsCache WHERE addedby IS NULL");
            Context.SendMessage(Context.Channel, "Кэш песен УДАЛЕН НАВСЕГДА.....................");
        }

        [Command("ignore")]
        [Checks.CheckAdmin]
        public void Ignore(CommandToggles.CommandEdit action, [Remainder] string username)
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
                        Context.Connection.Insert(ignr);
                    }
                    Context.SendMessage(Context.Channel, $"Пользователь(и) {username} добавлен(ы) в игнор!");
                    break;
                case CommandToggles.CommandEdit.Delete:
                    foreach (var item in usernames)
                    {
                        Context.Connection.Execute("DELETE FROM Ignore WHERE Username = ?", item);
                    }
                    Context.SendMessage(Context.Channel, $"Пользователь(и) {username} удален(ы) из игнора!");
                    break;
            }
        }
    }
}
