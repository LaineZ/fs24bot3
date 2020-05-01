using Qmmands;
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

        [Command("version")]
        [Description("Версия проги")]
        public void Version()
        {
            var os = Environment.OSVersion;
            Context.SendMessage(Context.Channel, String.Format("NET: {0} Система: {1} Версия: {2} Версия системы: {3}",
                Environment.Version.ToString(), os.Platform, os.VersionString, os.Version));
        }

        [Command("me")]
        [Description("Макроэкономические показатели")]
        public void Economy()
        {
            Context.SendMessage(Context.Channel, $"Число зарплат: {Shop.PaydaysCount.ToString()} Денежная масса: {Shop.GetMoneyAvg(Context.Connection)}");
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

        [Command("give")]
        [Checks.CheckAdmin]
        public void Give(string username, string item, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.AddItemToInv(item, count);
            Context.SendMessage(Context.Channel, "Вы добавили предмет: " + Shop.getItem(item).Name + " пользователю " + username);
        }

        [Command("xp")]
        [Checks.CheckAdmin]
        public void GiveXp(string username, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.IncreaseXp(count);
            Context.SendMessage(Context.Channel, "Вы установили " +  count + " xp пользователю " + username);
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
