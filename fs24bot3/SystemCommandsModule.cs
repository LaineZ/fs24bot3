using Qmmands;
using System;
using System.Collections.Generic;

namespace fs24bot3
{
    public sealed class SystemCommandModule : ModuleBase<CustomCommandContext>
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
            SQLTools sql = new SQLTools();

            sql.AddItemToInv(item, username, count, Context.Connection);
            Context.Socket.SendMessage(Context.Channel, "Вы добавили предмет: " + Shop.getItem(item).Name + " пользователю " + username);
        }
    }
}
