﻿using Newtonsoft.Json;
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

            foreach (var users in query)
            {
                var user = new UserOperations(users.Nick, Context.Connection);
                top.Add((users.Nick, user.CountItem(itemname)));
            }

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
        [Remarks("Стройте укрепления чтобы не получить гаечный ключ в лицо!!!")]
        public void Wrench(string username)
        {
            try
            {
                UserOperations user = new UserOperations(Context.Message.From, Context.Connection);
                int dmg = 0;


                if (user.RemItemFromInv(Shop.getItem("wrenchadv").Name, 1))
                {
                    dmg = 4;
                }
                else
                {
                    // trying with default wrench - if not found just end the command
                    // defalut wrench deals 0 damage bonus
                    if (!user.RemItemFromInv(Shop.getItem("wrench").Name, 1))
                    {
                        Context.SendMessage(Context.Channel, $"У вас нету {Shop.getItem("wrench").Name} или {Shop.getItem("wrenchadv").Name}");
                        return;
                    }
                }
                UserOperations userDest = new UserOperations(username, Context.Connection);
                var takeItems = userDest.GetInventory();

                var rand = new Random();

                if (rand.Next(0, 5 + userDest.CountItem("wall") - dmg) == 0 && username != Context.Message.From)
                {
                    int indexItem = rand.Next(takeItems.Count);
                    int itemCount = rand.Next(1, takeItems[indexItem].ItemCount);
                    user.AddItemToInv(takeItems[indexItem].Item, itemCount);
                    userDest.RemItemFromInv(takeItems[indexItem].Item, itemCount);
                    Context.SendMessage(Context.Channel, $"Вы кинули гаечный ключ с уроном {dmg + 5} в пользователя {username} при этом он потерял {takeItems[indexItem].Item} x{itemCount}");
                    if (rand.Next(0, 3) == 2) {
                        Context.SendMessage(username, $"Вас атакует {Context.Message.From} гаечными ключами! Вы уже потеряли {takeItems[indexItem].Item} x{itemCount} возможно он вас продолжает атаковать!");
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, Models.RandomMsgs.GetRandomMessage(Models.RandomMsgs.MissMessages));
                }
            }
            catch (Core.Exceptions.UserNotFoundException)
            {
                Context.SendMessage(Context.Channel, $"Вы кинули гаечные ключ в пользователя {username} при этом он потерял себя");
            }
        }
    }
}
