using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class InventoryCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("inv", "inventory")]
        [Description("Инвентарь. Параметр useSlugs отвечает за показ id предмета для команд @buy/@sell/@transfer и других")]
        public async Task Userstat(bool useSlugs = false)
        {
            var userop = new User(Context.Sender, Context.Connection);
            var userInv = userop.GetInventory();
            if (userInv.Count > 0)
            {
                if (!useSlugs)
                {
                    await Context.SendMessage(Context.Channel, Context.Sender + ": " + string.Join(" ", userInv.Select(x => $"{x.Item} x{x.ItemCount}")));
                }
                else
                {
                    await Context.SendMessage(Context.Channel, Context.Sender + ": " + string.Join(" ", userInv.Select(x => $"{x.Item}({Shop.GetItem(x.Item).Slug}) x{x.ItemCount}")));
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcColors.Gray}У вас ничего нет в инвентаре... Хотите сходить в магазин? @help -> @helpcmd buy");
            }
        }

        [Command("buy")]
        [Description("Купить товар")]
        public async Task Buy(string itemname, int count = 1)
        {
            User user = new User(Context.Sender, Context.Connection);

            int buyprice = Shop.GetItem(itemname).Price * count;

            bool sucessfully = await user.RemItemFromInv("money", buyprice);

            if (sucessfully)
            {
                user.AddItemToInv(itemname, count);
                await Context.SendMessage(Context.Channel, "Вы успешно купили " + Shop.GetItem(itemname).Name + " за " + buyprice + " денег");
                Shop.GetItem(itemname).Price += 5;
                Shop.Buys++;
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Недостаточно денег: " + buyprice);
            }
        }

        [Command("sell")]
        [Description("Продать товар")]
        public async Task Sell(string itemname, int count = 1)
        {
            User user = new User(Context.Sender, Context.Connection);

            if (await user.RemItemFromInv(itemname, count) && Shop.GetItem(itemname).Sellable)
            {
                // tin
                int sellprice = (int)Math.Floor((decimal)(Shop.GetItem(itemname).Price * count) / 2);
                user.AddItemToInv("money", sellprice);
                await Context.SendMessage(Context.Channel, "Вы успешно продали " + Shop.GetItem(itemname).Name + " за " + sellprice + " денег");
                Shop.Sells++;
            }
            else
            {
                await Context.SendMessage(Context.Channel, "Вы не можете это продать!");
            }
        }

        [Command("sellall")]
        [Description("Продать весь товар")]
        public async Task SellAll()
        {
            User user = new User(Context.Sender, Context.Connection);
            var inv = user.GetInventory();
            int totalPrice = 0;

            foreach (var item in inv)
            {
                if (Shop.GetItem(item.Item).Sellable && await user.RemItemFromInv(Shop.GetItem(item.Item).Slug, item.ItemCount))
                {
                    totalPrice += (int)Math.Floor((decimal)(Shop.GetItem(item.Item).Price * item.ItemCount) / 2);
                }
            }

            user.AddItemToInv("money", totalPrice);
            await Context.SendMessage(Context.Channel, $"Вы продали всё! За {totalPrice} денег!");
        }

        [Command("transfer")]
        [Description("Передатать вещи")]
        public async Task Transfer(string destanationNick, string itemname, int count = 1)
        {
            User user = new User(Context.Sender, Context.Connection);
            User destanation = new User(destanationNick, Context.Connection);

            if (await user.RemItemFromInv(Shop.GetItem(itemname).Name, count))
            {
                destanation.AddItemToInv(itemname, count);
                await Context.SendMessage(Context.Channel, $"Вы успешно передали {Shop.GetItem(itemname).Name} x{count} пользователю {destanationNick}");
            }
            else
            {
                await Context.SendMessage(Context.Channel, "У вас нет таких предметов!");
            }
        }

        [Command("topitem")]
        [Description("Топ по предматам, по стандарту показывает топ по деньгам")]
        public async Task TopItem(string itemname = "money")
        {
            var top = new List<(string Name, int Count)>();

            var query = Context.Connection.Table<SQL.UserStats>();

            foreach (var users in query)
            {
                var user = new User(users.Nick, Context.Connection);
                top.Add((users.Nick, user.CountItem(itemname)));
            }

            var result = top.OrderByDescending(p => p.Count).ToList();

            await Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ У КОТОРЫХ ЕСТЬ: " + Shop.GetItem(itemname).Name);

            foreach (var (Name, Count) in result.Take(5))
            {
                await Context.SendMessage(Context.Channel, IrcColors.Bold + Name + ": " + Count);
            }
        }

        [Command("topmoney")]
        [Description("Топ по деньгам")]
        public async Task TopMoney()
        {
            await TopItem();
        }


        [Command("toplevels", "toplevel", "top")]
        [Description("Топ по уровню")]
        public async Task TopLevels()
        {
            var top = new List<(string Name, int Count)>();

            var query = Context.Connection.Table<SQL.UserStats>();

            foreach (var users in query)
            {
                top.Add((users.Nick, users.Level));
            }

            var result = top.OrderByDescending(p => p.Count).ToList();

            await Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ ПО УРОВНЮ");

            foreach (var topuser in result.Take(5))
            {
                await Context.SendMessage(Context.Channel, IrcColors.Bold + topuser.Name + ": " + topuser.Count);
            }
        }

        [Command("wrench")]
        [Description("Cтарая добрая игра по отъему денег у населения... Слишком жестокая игра...")]
        [Remarks("Стройте укрепления чтобы не получить гаечный ключ в лицо!!! И покупайте колонки чтобы не пропустить сообщения вашей оборонительной системы!!!")]
        public async Task Wrench([Remainder] string username)
        {
            try
            {
                User user = new User(Context.Sender, Context.Connection);
                int dmg = 0;
                string wrname = string.Empty;

                List<(string, int)> wrenches = new List<(string, int)>()
                {
                    // Wrench damage. Sorted in ascend order by damage
                    ("wrenchadv", 8),
                    ("hammer", 5),
                    ("wrench", 3),
                    ("dj", 1)
                };

                foreach ((string wrench, int wrdmg) in wrenches)
                {
                    if (await user.RemItemFromInv(wrench, 1))
                    {
                        dmg = wrdmg;
                        wrname = Shop.GetItem(wrench).Name;
                        break;
                    }
                }

                // wrench not found...... 😥
                if (dmg == 0)
                {
                    await Context.SendMessage(Context.Channel, $"У вас нету: {string.Join(" или ", wrenches.Select(x => Shop.GetItem(x.Item1).Name))}");
                    return;
                }

                User userDest = new User(username, Context.Connection);
                var takeItems = userDest.GetInventory();

                var rand = new Random();

                if (rand.Next(0, 10 + userDest.CountItem("wall") - dmg) == 0 && username != Context.Sender)
                {
                    int indexItem = rand.Next(takeItems.Count);
                    int itemCount = 1;

                    if (takeItems[indexItem].ItemCount / (15 - dmg) > 0)
                    {
                        itemCount = rand.Next(1, takeItems[indexItem].ItemCount / (15 - dmg));
                    }

                    user.AddItemToInv(takeItems[indexItem].Item, itemCount);
                    await userDest.RemItemFromInv(takeItems[indexItem].Item, itemCount);

                    int xp = rand.Next(100, 500 + user.GetUserInfo().Level);

                    user.IncreaseXp(xp);

                    await Context.SendMessage(Context.Channel, $"Вы кинули {wrname} с уроном {dmg} в пользователя {username} при этом он потерял {takeItems[indexItem].Item} x{itemCount} и за это вам +{xp} XP");
                    if (rand.Next(0, 7) == 2)
                    {
                        await Context.SendMessage(username, $"Вас атакует {Context.Sender} гаечными ключами! Вы уже потеряли {takeItems[indexItem].Item} x{itemCount} возможно он вас продолжает атаковать!");
                    }
                    else
                    {
                        if (rand.Next(0, 1) == 1 || await userDest.RemItemFromInv("speaker", 1))
                        {
                            await Context.SendMessage(username, $"Вас атакует {Context.Sender} гаечными ключами! Вы потеряли {takeItems[indexItem].Item} x{itemCount}! Так как у вас мониторные колонки - вы получили это сообщение немедленно, но берегитесь: колонки не бесконечные!");
                        }
                    }
                }
                else
                {
                    await Context.SendMessage(Context.Channel, RandomMsgs.GetRandomMessage(RandomMsgs.MissMessages));
                }
            }
            catch (Core.Exceptions.UserNotFoundException)
            {
                await Context.SendMessage(Context.Channel, $"Вы кинули гаечный ключ в {username}!");
            }
        }

        [Command("break")]
        [Description("С определенным шансом позволяет пробить укрепления - требуется пистолет или 💣")]
        public async Task Shot(string username)
        {
            try
            {
                User user = new User(Context.Sender, Context.Connection);
                int dmg = 0;
                string brname = String.Empty;

                List<(string, int)> wrenches = new List<(string, int)>()
                {
                    // Walls damage. Sorted in ascend order by damage
                    ("bomb", 9),
                    ("pistol", 7),
                };

                foreach ((string wrench, int wrdmg) in wrenches)
                {
                    if (await user.RemItemFromInv(wrench, 1))
                    {
                        dmg = wrdmg;
                        brname = Shop.GetItem(wrench).Name;
                        break;
                    }
                }

                // destroy item not found...... 😥
                if (dmg == 0)
                {
                    await Context.SendMessage(Context.Channel, $"У вас нету пистолета или бомбы!");
                    return;
                }

                User userDest = new User(username, Context.Connection);
                var rand = new Random();

                if (userDest.CountItem("wall") > 0 && rand.Next(0, 10 - dmg) == 0 && username != Context.Sender)
                {
                    await userDest.RemItemFromInv("wall", 1);
                    await Context.SendMessage(Context.Channel, $"Вы атаковали с помощью {brname} уроном {dmg} укрепления пользователя {username} и сломали 1 укрепление!");
                    if (rand.Next(0, 3) == 2)
                    {
                        await Context.SendMessage(username, $"Вас атакует {Context.Sender}!");
                    }
                }
                else
                {
                    await Context.SendMessage(Context.Channel, "Вы не попали по укреплению или их вообще нет!");
                }
            }
            catch (Core.Exceptions.UserNotFoundException)
            {
                await Context.SendMessage(Context.Channel, $"Вы потеряли себя...");
            }
        }
    }
}
