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
        public void Buy(string itemname, int count = 1)
        {
            UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

            int buyprice = Shop.GetItem(itemname).Price * count;

            bool sucessfully = user.RemItemFromInv("money", buyprice);

            if (sucessfully)
            {
                user.AddItemToInv(itemname, count);
                Context.SendMessage(Context.Channel, "Вы успешно купили " + Shop.GetItem(itemname).Name + " за " + buyprice + " денег");
                Shop.GetItem(itemname).Price += 5;
                Shop.Buys++;
            }
            else
            {
                Context.SendMessage(Context.Channel, "Недостаточно денег: " + buyprice);
            }
        }

        [Command("sell")]
        [Description("Продать товар")]
        public void Sell(string itemname, int count = 1)
        {
            UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

            if (user.RemItemFromInv(itemname, count) && Shop.GetItem(itemname).Sellable)
            {
                // tin
                int sellprice = (int)Math.Floor((decimal)(Shop.GetItem(itemname).Price * count) / 2);
                user.AddItemToInv("money", sellprice);
                Context.SendMessage(Context.Channel, "Вы успешно продали " + Shop.GetItem(itemname).Name + " за " + sellprice + " денег");
                Shop.Sells++;
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
                if (Shop.GetItem(item.Item).Sellable && user.RemItemFromInv(Shop.GetItem(item.Item).Slug, item.ItemCount))
                {
                    totalPrice += (int)Math.Floor((decimal)(Shop.GetItem(item.Item).Price * item.ItemCount) / 2);
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

            if (user.RemItemFromInv(Shop.GetItem(itemname).Name, count))
            {
                destanation.AddItemToInv(itemname, count);
                Context.SendMessage(Context.Channel, $"Вы успешно передали {Shop.GetItem(itemname).Name} x{count} пользователю {destanationNick}");
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

            Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ У КОТОРЫХ ЕСТЬ: " + Shop.GetItem(itemname).Name);

            foreach (var (Name, Count) in result.Take(5))
            {
                Context.SendMessage(Context.Channel, Models.IrcColors.Bold + Name + ": " + Count);
            }
        }

        [Command("topmoney")]
        [Description("Топ по деньгам")]
        public void TopMoney()
        {
            TopItem();
        }


        [Command("toplevels", "toplevel", "top")]
        [Description("Топ по уровню")]
        public void TopLevels()
        {
            var top = new List<(string Name, int Count)>();

            var query = Context.Connection.Table<Models.SQL.UserStats>();

            foreach (var users in query)
            {
                top.Add((users.Nick, users.Level));
            }

            var result = top.OrderByDescending(p => p.Count).ToList();

            Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ ПО УРОВНЮ");

            foreach (var topuser in result.Take(5))
            {
                Context.SendMessage(Context.Channel, Models.IrcColors.Bold + topuser.Name + ": " + topuser.Count);
            }
        }

        [Command("wrench")]
        [Description("Cтарая добрая игра по отъему денег у населения... Слишком жестокая игра...")]
        [Remarks("Стройте укрепления чтобы не получить гаечный ключ в лицо!!! И покупайте колонки чтобы не пропустить сообщения вашей оборонительной системы!!!")]
        public void Wrench([Remainder] string username)
        {
            try
            {
                UserOperations user = new UserOperations(Context.Message.From, Context.Connection);
                int dmg = 0;
                string wrname = String.Empty;

                List<(string, int)> wrenches = new List<(string, int)>() 
                {
                    // Wrench damage. Sorted in ascend order by damage
                    ("wrenchadv", 10),
                    ("wrench", 5),
                };

                foreach ((string wrench, int wrdmg) in wrenches)
                {
                    if (user.RemItemFromInv(wrench, 1))
                    {
                        dmg = wrdmg;
                        wrname = Shop.GetItem(wrench).Name;
                        break;
                    }
                }

                // wrench not found...... 😥
                if (dmg == 0)
                {
                    Context.SendMessage(Context.Channel, $"У вас нету гаечных ключей!");
                    return;
                }

                UserOperations userDest = new UserOperations(username, Context.Connection);
                var takeItems = userDest.GetInventory();

                var rand = new Random();

                if (rand.Next(0, 10 + userDest.CountItem("wall") - dmg) == 0 && username != Context.Message.From)
                {
                    int indexItem = rand.Next(takeItems.Count);
                    int itemCount = 1;

                    if (takeItems[indexItem].ItemCount / (15 - dmg) > 0) {
                        itemCount = rand.Next(1, takeItems[indexItem].ItemCount / (15 - dmg));
                    }
                    
                    user.AddItemToInv(takeItems[indexItem].Item, itemCount);
                    userDest.RemItemFromInv(takeItems[indexItem].Item, itemCount);

                    int xp = rand.Next(100, 500 + user.GetUserInfo().Level);

                    user.IncreaseXp(xp);

                    Context.SendMessage(Context.Channel, $"Вы кинули {wrname} с уроном {dmg + 5} в пользователя {username} при этом он потерял {takeItems[indexItem].Item} x{itemCount} и за это вам +{xp} XP");
                    if (rand.Next(0, 7) == 2)
                    {
                        Context.SendMessage(username, $"Вас атакует {Context.Message.From} гаечными ключами! Вы уже потеряли {takeItems[indexItem].Item} x{itemCount} возможно он вас продолжает атаковать!");
                    }
                    else
                    {
                        if (rand.Next(0, 1) == 1 || userDest.RemItemFromInv("speaker", 1))
                        {
                            Context.SendMessage(username, $"Вас атакует {Context.Message.From} гаечными ключами! Вы потеряли {takeItems[indexItem].Item} x{itemCount}! Так как у вас мониторные колонки - вы получили это сообщение немедленно, но берегитесь: колонки не бесконечные!");
                        }
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, Models.RandomMsgs.GetRandomMessage(Models.RandomMsgs.MissMessages));
                }
            }
            catch (Core.Exceptions.UserNotFoundException)
            {
                Context.SendMessage(Context.Channel, $"Вы кинули гаечный ключ в {username}!");
            }
        }

        [Command("break")]
        [Description("С определенным шансом позволяет пробить укрепления - требуется пистолет или 💣")]
        public void Shot(string username)
        {
            try
            {
                // TODO: Refactor
                UserOperations user = new UserOperations(Context.Message.From, Context.Connection);
                int dmg = 0;


                if (user.RemItemFromInv(Shop.GetItem("bomb").Name, 1))
                {
                    dmg = 3;
                }
                else
                {
                    // trying with pistol - if not found just end the command
                    // pistol deals 0 damage bonus
                    if (!user.RemItemFromInv(Shop.GetItem("pistol").Name, 1))
                    {
                        Context.SendMessage(Context.Channel, $"У вас нету {Shop.GetItem("pistol").Name} или {Shop.GetItem("bomb").Name}");
                        return;
                    }
                }
                UserOperations userDest = new UserOperations(username, Context.Connection);

                var rand = new Random();

                if (userDest.CountItem("wall") > 0 && rand.Next(0, 10 - dmg) == 0 && username != Context.Message.From)
                {
                    userDest.RemItemFromInv("wall", 1);
                    Context.SendMessage(Context.Channel, $"Вы атаковали с уроном {dmg + 5} укрепления пользователя {username} и сломали 1 укрепление!");
                    if (rand.Next(0, 3) == 2)
                    {
                        Context.SendMessage(username, $"Вас атакует {Context.Message.From}!");
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, "Вы не попали по укреплению! =(");
                }
            }
            catch (Core.Exceptions.UserNotFoundException)
            {
                Context.SendMessage(Context.Channel, $"Вы потеряли себя...");
            }
        }
    }
}
