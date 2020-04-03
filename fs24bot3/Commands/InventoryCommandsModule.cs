using Newtonsoft.Json;
using Qmmands;
using SQLiteNetExtensions.Extensions;
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
        [Description("Инвентарь")]
        public void Userstat()
        {
            var userop = new UserOperations(Context.Message.User, Context.Connection);
            var userInv = userop.GetInventory();
            if (userInv != null && userInv.Count > 0)
            {
                Context.Socket.SendMessage(Context.Channel, Context.Message.User + ": " + string.Join(" ", userInv.Select(x => $"{x.Item} x{x.ItemCount}")));
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}У вас ничего нет в инвентаре... Хотите сходить в магазин? @help -> @helpcmd buy");
            }
        }

        [Command("buy")]
        [Description("Купить товар")]
        public void Buy(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);

            int buyprice = Shop.getItem(itemname).Price * count;

            bool sucessfully = user.RemItemFromInv("money", buyprice);

            if (sucessfully)
            {
                Context.Socket.SendMessage(Context.Channel, "Вы успешно купили " + Shop.getItem(itemname).Name + " за " + buyprice + " денег");
                Shop.getItem(itemname).Price += 25;
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Недостаточно денег: " + buyprice);
            }
        }

        [Command("sell")]
        [Description("Продать товар")]
        public void Sell(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.User, Context.Connection);

            if (user.RemItemFromInv(Shop.getItem(itemname).Name, count))
            {
                // tin
                int sellprice = (int)Math.Floor((decimal)(Shop.getItem(itemname).Price * count) / 2);
                user.AddItemToInv("money", sellprice);
                Context.Socket.SendMessage(Context.Channel, "Вы успешно продали " + Shop.getItem(itemname).Name + " за " + sellprice + " денег");
            }
            else
            {
                Context.Socket.SendMessage(Context.Channel, "Вы не можете это продать!");
            }
        }

        [Command("transfer")]
        [Description("Передатать вещи")]
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
        [Description("Топ по предматам, по стандарту показывает топ по деньгам")]
        public void TopItem(string itemname = "money")
        {
            var top = new List<(string Name, int Count)>();

            var query = Context.Connection.Table<Models.SQL.UserStats>();
            /*
            foreach (var users in query)
            {
                var userinfo = Context.Connection.GetWithChildren<Models.SQL.UserStats>(Context.Message.User);
                int itemToCount = userinfo.Inv.FindIndex(item => item.Name.Equals(Shop.getItem(itemname).Name));
                if (itemToCount >= 0)
                {
                    top.Add((userinfo.Nick, userinfo.Inv[itemToCount].Count));
                }
            }
            */
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
        [Description("Топ по предматам, по стандарту показывает топ по деньгам")]
        public void Wrench(string username)
        {
            UserOperations user = new UserOperations(username, Context.Connection);
            var userinfo = user.GetUserInfo();
        }
    }
}
