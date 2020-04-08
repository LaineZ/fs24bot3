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
            var userop = new UserOperations(Context.Message.From, Context.Connection);
            var userInv = userop.GetInventory();
            if (userInv != null && userInv.Count > 0)
            {
                Context.SendMessage(Context.Channel, Context.Message.From + ": " + string.Join(" ", userInv.Select(x => $"{x.Item} x{x.ItemCount}")));
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}У вас ничего нет в инвентаре... Хотите сходить в магазин? @help -> @helpcmd buy");
            }
        }

        [Command("buy")]
        [Description("Купить товар")]
        public void Buy(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

            int buyprice = Shop.getItem(itemname).Price * count;

            bool sucessfully = user.RemItemFromInv("money", buyprice);

            if (sucessfully)
            {
                user.AddItemToInv(itemname, count);
                Context.SendMessage(Context.Channel, "Вы успешно купили " + Shop.getItem(itemname).Name + " за " + buyprice + " денег");
                Shop.getItem(itemname).Price += 5;
            }
            else
            {
                Context.SendMessage(Context.Channel, "Недостаточно денег: " + buyprice);
            }
        }

        [Command("sell")]
        [Description("Продать товар")]
        public void Sell(string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

            if (user.RemItemFromInv(itemname, count) && Shop.getItem(itemname).Sellable)
            {
                // tin
                int sellprice = (int)Math.Floor((decimal)(Shop.getItem(itemname).Price * count) / 2);
                user.AddItemToInv("money", sellprice);
                Context.SendMessage(Context.Channel, "Вы успешно продали " + Shop.getItem(itemname).Name + " за " + sellprice + " денег");
            }
            else
            {
                Context.SendMessage(Context.Channel, "Вы не можете это продать!");
            }
        }

        [Command("sellall")]
        [Description("Продать весь товар")]
        public void SellAll()
        {
            UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

            var inv = user.GetInventory();

            int totalPrice = 0;

            foreach (var item in inv)
            {
                if (Shop.getItem(item.Item).Sellable && user.RemItemFromInv(Shop.getItem(item.Item).Slug, item.ItemCount))
                {
                    totalPrice += (int)Math.Floor((decimal)(Shop.getItem(item.Item).Price * item.ItemCount) / 2);
                }
            }

            user.AddItemToInv("money", totalPrice);
            Context.SendMessage(Context.Channel, $"Вы продали всё! За {totalPrice} денег!");
        }

        [Command("transfer")]
        [Description("Передатать вещи")]
        public void Transfer(string destanationNick, string itemname, int count)
        {
            UserOperations user = new UserOperations(Context.Message.From, Context.Connection);
            UserOperations destanation = new UserOperations(destanationNick, Context.Connection);

            if (user.RemItemFromInv(Shop.getItem(itemname).Name, count))
            {
                destanation.AddItemToInv(itemname, count);
                Context.SendMessage(Context.Channel, $"Вы успешно передали {Shop.getItem(itemname).Name} x{count} пользователю {destanationNick}");
            }
            else
            {
                Context.SendMessage(Context.Channel, "У вас нет таких предметов!");
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
                var userinfo = Context.Connection.GetWithChildren<Models.SQL.UserStats>(Context.Message.From);
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

            Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ У КОТОРЫХ ЕСТЬ: " + Shop.getItem(itemname).Name);

            foreach (var topuser in result)
            {
                Context.SendMessage(Context.Channel, Models.IrcColors.Bold + topuser.Name + ": " + topuser.Count);
            }
        }


        [Command("wrench")]
        [Description("Cтарая добрая игра по отъему денег у населения... Слишком жестокая игра...")]
        public void Wrench(string username)
        {
            try
            {
                UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

                if (user.RemItemFromInv(Shop.getItem("money").Name, 1))
                {
                    UserOperations userDest = new UserOperations(username, Context.Connection);
                    var takeItems = userDest.GetInventory();

                    var rand = new Random();

                    if (rand.Next(0, 1) == 0)
                    {
                        int indexItem = rand.Next(takeItems.Count);
                        int itemCount = rand.Next(1, takeItems[indexItem].ItemCount);
                        user.AddItemToInv(takeItems[indexItem].Item, itemCount);
                        userDest.RemItemFromInv(takeItems[indexItem].Item, itemCount);
                        Context.SendMessage(Context.Channel, $"Вы кинули гаечные ключ в пользователя {username} при этом он потерял {takeItems[indexItem].Item} x{itemCount}");
                    }
                    else
                    {
                        Context.SendMessage(Context.Channel, "Вы не попали...");
                    }
                }
            }
            catch (Core.Exceptions.UserNotFoundException)
            {
                Context.SendMessage(Context.Channel, $"Вы кинули гаечные ключ в пользователя {username} при этом он потерял себя");
            }
        }
    }
}
