using Qmmands;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace fs24bot3
{
    public sealed class SystemCommandModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("version")]
        [Qmmands.Description("Версия проги")]
        public void Version()
        {
            var os = Environment.OSVersion;
            Context.Socket.SendMessage(Context.Channel, String.Format("NET: {0} Система: {1} Версия: {2} Версия системы: {3}",
                Environment.Version.ToString(), os.Platform, os.VersionString, os.Version));
        }

        [Command("gc")]
        [Checks.CheckAdmin]
        [Qmmands.Description("Вывоз мусора")]
        public void CollectGarbage()
        {
            GC.Collect();
            Context.Socket.SendMessage(Context.Channel, "Мусор вывезли!");
        }


        [Command("give")]
        [Checks.CheckAdmin]
        public void Give(string username, string item, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.AddItemToInv(item, count);
            Context.Socket.SendMessage(Context.Channel, "Вы добавили предмет: " + Shop.getItem(item).Name + " пользователю " + username);
        }

        [Command("xp")]
        [Checks.CheckAdmin]
        public void GiveXp(string username, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.IncreaseXp(count);
            Context.Socket.SendMessage(Context.Channel, "Вы установили " +  count + " xp пользователю " + username);
        }

        [Command("level")]
        [Checks.CheckAdmin]
        public void GiveLevel(string username, int count)
        {
            UserOperations sql = new UserOperations(username, Context.Connection);

            sql.SetLevel(count);
            Context.Socket.SendMessage(Context.Channel, "Вы установили уровень: " + count + " пользователю " + username);
        }
    }
}
