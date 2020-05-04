using Qmmands;
using Serilog.Core;
using Serilog.Events;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
            Context.SendMessage(Context.Channel, $"Число зарплат: {Shop.PaydaysCount.ToString()} Денежная масса: {Shop.GetMoneyAvg(Context.Connection)} Последнее время выполнения Shop.Update(): {Shop.TickSpeed.TotalMilliseconds} ms Частота выполнения Shop.Update() {Shop.Tickrate} ms");
        }

        [Command("gc")]
        [Checks.CheckAdmin]
        [Description("Вывоз мусора")]
        public void CollectGarbage()
        {
            GC.Collect();
            Context.SendMessage(Context.Channel, "Мусор вывезли!");
        }

        [Command("htppcache")]
        [Checks.CheckAdmin]
        public void CacheStatus(string option = "status")
        {
            SQLiteConnection cache = new SQLiteConnection("fscache.sqlite");

            switch (option)
            {
                case "status":
                    var query = cache.Table<Models.SQL.HttpCache>().Count();
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
                Context.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}Конфигурацию не удалось загрузить {e.Message}");
            }
        }


        [Command("testsplit")]
        [Checks.CheckAdmin]
        public void TestSplit(int count = 200, char ch = 'а')
        {
            Context.SendMessage(Context.Channel, new string(ch, count));
        }

        [Command("give")]
        [Checks.CheckAdmin]
        public void Give(string username, string item, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.AddItemToInv(item, count);
            Context.SendMessage(Context.Channel, "Вы добавили предмет: " + Shop.GetItem(item).Name + " пользователю " + username);
        }

        [Command("xp")]
        [Checks.CheckAdmin]
        public void GiveXp(string username, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.IncreaseXp(count);
            Context.SendMessage(Context.Channel, "Вы установили " + count + " xp пользователю " + username);
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
        public void Tickrate(string level = "Verbose")
        {
            Enum.TryParse(level, out LogEventLevel lvlToSet);
            Configuration.LoggerSw.MinimumLevel = lvlToSet;
            Context.SendMessage(Context.Channel, $"Установлен уровень лога `{level}`");
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
                        var ignr = new Models.SQL.Ignore()
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
