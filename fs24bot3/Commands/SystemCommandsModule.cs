﻿using fs24bot3.Models;
using Qmmands;
using Serilog.Events;
using SQLite;
using System;
using System.IO;
using System.Linq;

namespace fs24bot3
{
    public sealed class SystemCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("info")]
        [Description("Информация о боте")]
        public void Version()
        {
            var os = Environment.OSVersion;
            Context.SendMessage(Context.Channel, String.Format(".NET Core: {0} Система: {1}",
                Environment.Version.ToString(), os.VersionString));
        }

        [Command("me")]
        [Description("Макроэкономические показатели")]
        public void Economy()
        {
            Context.SendMessage(Context.Channel, $"Число зарплат: {Shop.PaydaysCount} Денежная масса: {Shop.GetMoneyAvg(Context.Connection)} Последнее время выполнения Shop.Update(): {Shop.TickSpeed.TotalMilliseconds} ms Частота выполнения Shop.Update() {Shop.Tickrate} ms");
        }

        [Command("gc")]
        [Checks.CheckAdmin]
        [Description("Вывоз мусора")]
        public void CollectGarbage()
        {
            GC.Collect();
            Context.SendMessage(Context.Channel, "Мусор вывезли!");
        }

        [Command("httpcache")]
        [Checks.CheckAdmin]
        public void CacheStatus(string option = "status")
        {
            SQLiteConnection cache = new SQLiteConnection("fscache.sqlite");

            switch (option)
            {
                case "status":
                    var query = cache.Table<SQL.HttpCache>().Count();
                    FileInfo fi = new FileInfo("fscache.sqlite");
                    Context.SendMessage(Context.Channel, $"Число записей: {query} Размер базы: {fi.Length / 1024} KB");
                    break;
            }
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


        [Command("testsplit")]
        [Checks.CheckAdmin]
        public void TestSplit(int count = 200, char ch = 'а')
        {
            Context.SendMessage(Context.Channel, new string(ch, count));
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


        [Command("testfishing")]
        [Checks.CheckAdmin]
        public void TestFishing(int numberOfLaunches = 1000, int factor = 20, int baseFac = 10)
        {
            var user = new UserOperations(Context.Message.From, Context.Connection, Context);
            var userRod = user.GetRod();

            if (userRod == null)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка не найдена");
                return;
            }

            if (userRod.Nest == null)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Место рыбалки не установлено, используйте @nest");
                return;
            }

            var rod = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(userRod.RodName)).ToList()[0];
            var nest = Context.Connection.Table<SQL.FishingNests>().Where(v => v.Name.Equals(userRod.Nest)).ToList()[0];

            int catched = 0;
            int failed = 0;


            for (int i = 0; i < numberOfLaunches; i++)
            {
                Random rand = new Random();
                if (rand.Next(rod.HookSize, baseFac + nest.Level - rod.HookSize - rod.FishingLine - nest.FishCount) == factor)
                {
                    catched++;
                }
                else
                {
                    failed++;
                }
            }
            Context.SendMessage(Context.Channel, $"{IrcColors.Gray}ok {catched} failed {failed} {(catched / failed) * 100}%");
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

        [Command("delete")]
        [Checks.CheckAdmin]
        public void DeleteUser(string users)
        {
            foreach (var user in users.Split(" "))
            {
                Context.Connection.Execute("DELETE FROM UserStats WHERE Nick = ?", user);   
                Context.Connection.Execute("DELETE FROM Inventory WHERE Nick = ?", user);
            }
            Context.Connection.Execute("VACUUM;");
            Context.SendMessage(Context.Channel, "Данные удалены!");
        }

        [Command("view")]
        [Checks.CheckAdmin]
        public void ViewUsers()
        {
            Context.SendMessage(Context.Channel, String.Join(' ', Context.Connection.Table<SQL.UserStats>().ToList()));
        }

        [Command("ignore")]
        [Checks.CheckAdmin]
        public void Ignore(string action, [Remainder] string username)
        {
            var usernames = username.Split(" ");
            switch (action)
            {
                case "add":
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
                case "del":
                    foreach (var item in usernames)
                    {
                        Context.Connection.Execute("DELETE FROM Ignore WHERE Username = ?", item);
                    }
                    Context.SendMessage(Context.Channel, $"Пользователь(и) {username} удален(ы) из игнора!");
                    break;
                default:
                    break;
            }
        }
    }
}
