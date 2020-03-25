using Newtonsoft.Json;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fs24bot3
{
    public sealed class InventoryCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("inv", "inventory")]
        [Qmmands.Description("Инвентарь")]
        public void Userstat()
        {
            UserOperations usr = new UserOperations(Context.Message.User, Context.Connection);
            var userinfo = usr.GetUserInfo();
            var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);

            Context.Socket.SendMessage(Context.Channel, Context.Message.User + ": " + string.Join(" ", userInv.Items.Select(x => $"{x.Name} x{x.Count}")));
        }

        [Command("buy")]
        [Qmmands.Description("Купить товар")]
        public void Buy(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);

            bool sucessfully = user.RemItemFromInv("money", count);

            if (sucessfully)
            {
                user.AddItemToInv(itemname, count);
                Context.Socket.SendMessage(Context.Channel, "Вы успешно купили " + Shop.getItem(itemname).Name + " за " + Shop.getItem(itemname).Price * count + " денег");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Недостаточно денег: " + Shop.getItem(itemname).Price * count);
            }
        }

        [Command("sell")]
        [Qmmands.Description("Продать товар")]
        public void Sell(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);

            if (user.RemItemFromInv(Shop.getItem(itemname).Name, count))
            {
                user.AddItemToInv("money", Shop.getItem(itemname).Price * count);
                Context.Socket.SendMessage(Context.Channel, "Вы успешно продали " + Shop.getItem(itemname).Name + " за " + Shop.getItem(itemname).Price * count + " денег");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Вы не можете это продать!");
            }
        }

        [Command("transfer")]
        [Qmmands.Description("Передатать вещи")]
        public void Transfer(string destanationNick, string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);
            UserOperations destanation = new UserOperations(destanationNick, Context.Connection);

            if (user.RemItemFromInv(Shop.getItem(itemname).Name, count))
            {
                destanation.AddItemToInv(itemname, count);
                Context.Socket.SendMessage(Context.Channel, $"Вы успешно передали {Shop.getItem(itemname).Name} x{count} пользователю {destanationNick}");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "У вас нет таких предметов!");
            }
        }

        [Command("topitem")]
        [Qmmands.Description("Топ по предматам, по стандарту показывает топ по деньгам")]
        public void TopItem(string itemname = "money")
        {
            var top = new List<(string Name, int Count)>();

            var query = Context.Connection.Table<Models.SQLUser.UserStats>();
            foreach (var users in query)
            {
                UserOperations user = new UserOperations(users.Nick, Context.Connection);
                var userinfo = user.GetUserInfo();
                var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);
                int itemToCount = userInv.Items.FindIndex(item => item.Name.Equals(Shop.getItem(itemname).Name));
                if (itemToCount > 0)
                {
                    top.Add((userinfo.Nick, userInv.Items[itemToCount].Count));
                }
            }
            var result = top.OrderByDescending(p => p.Count).ToList();

            if (result.Count > 4)
            {
                result.RemoveRange(4, result.Count - 4);
            }

            Context.Socket.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ У КОТОРЫХ ЕСТЬ: " + Shop.getItem(itemname).Name);

            foreach (var topuser in result)
            {
                Context.Socket.SendMessage(Context.Channel, Models.IrcColors.Bold + topuser.Name + ": " + topuser.Count);
            }
        }


        [Command("wrench")]
        [Qmmands.Description("Топ по предматам, по стандарту показывает топ по деньгам")]
        public void Wrench(string username)
        {
            UserOperations user = new UserOperations(username, Context.Connection);
            var userinfo = user.GetUserInfo();
            var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);
        }
    }
}
