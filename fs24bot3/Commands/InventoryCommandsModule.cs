﻿using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fs24bot3.Commands
{
    public sealed class InventoryCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("shop")]
        [Description("Магазин")]
        public async Task Shop(string item = "")
        {
            await Context.SendMessage(Context.Channel, string.Join(' ',
            Context.BotCtx.Shop.Items
            .Where(x => x.Key.Contains(item) || x.Value.Name.Contains(item))
            .Where(x => x.Value.Sellable)
            .Select(x => $"[{x.Key}] {x.Value.Name} 💰{x.Value.Price},")));
        }

        [Command("inv", "inventory")]
        [Description("Инвентарь. Параметр useSlugs отвечает за показ id предмета для команд buy/sell/transfer и других")]
        public async Task Userstat(bool useSlugs = false)
        {
            var userInv = Context.User.GetInventory();
            if (userInv.Count > 0)
            {
                if (!useSlugs)
                {
                    await Context.SendMessage(Context.Channel, Context.Sender + ": " + string.Join(" ", userInv.Select(x => $"{Context.BotCtx.Shop.Items[x.Item].Name} x{x.ItemCount}")));
                }
                else
                {
                    await Context.SendMessage(Context.Channel, Context.Sender + ": " + string.Join(" ", userInv.Select(x => $"{x.Item}({Context.BotCtx.Shop.Items[x.Item].Name}) x{x.ItemCount}")));
                }
            }
            else
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Gray}У вас ничего нет в инвентаре... Хотите сходить в магазин? {Context.User.GetUserPrefix()}shop -> {ConfigurationProvider.Config.Prefix}helpcmd buy");
            }
        }

        [Command("buy")]
        [Description("Купить товар")]
        public async Task Buy([Remainder] string itemnamecount)
        {
            int.TryParse(Regex.Match(itemnamecount, @"\d+").Value, out int count);
            string itemname = itemnamecount.Replace(count.ToString(), string.Empty).Trim();
            var (success, price) = await Context.BotCtx.Shop.Buy(Context.User, itemname, count);

            if (success)
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Green}Вы успешно купили {Context.BotCtx.Shop.Items[itemname].Name} x{count} за {price} денег");
            }
            else
            {
                Context.SendSadMessage(Context.Channel, $"Недостаточно денег: {price} чтобы купить {Context.BotCtx.Shop.Items[itemname].Name} x{count}");
            }
        }

        [Command("sell")]
        [Description("Продать товар")]
        public async Task Sell([Remainder] string itemnamecount)
        {
            int.TryParse(Regex.Match(itemnamecount, @"\d+").Value, out int count);
            string itemname = itemnamecount.Replace(count.ToString(), string.Empty).Trim();

            var (success, price) = await Context.BotCtx.Shop.Sell(Context.User, itemname, count);

            if (success)
            {
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Green}Вы успешно продали {Context.BotCtx.Shop.Items[itemname].Name} x{count} за {price} денег");
            }
            else
            {
                if (Context.BotCtx.Shop.Items[itemname].Sellable)
                {
                    Context.SendSadMessage(Context.Channel, $"Такой предмет не продается!");
                }
                else
                {
                    Context.SendSadMessage(Context.Channel, $"У вас нет такого предмета!");
                }
            }
        }

        [Command("sellall")]
        [Description("Продать весь товар")]
        public async Task SellAll()
        {
            var inv = Context.User.GetInventory();
            int totalPrice = 0;

            foreach (var item in inv)
            {
                var (selled, sellprice) = await Context.BotCtx.Shop.Sell(Context.User, item.Item, item.ItemCount);
                if (selled)
                {
                    totalPrice += sellprice;
                }
            }

            await Context.SendMessage(Context.Channel, $"Вы продали всё! За {totalPrice} денег!");
        }

        [Command("transfer")]
        [Description("Передать вещи")]
        public async Task Transfer(string destanationNick, [Remainder] string itemnamecount)
        {
            int.TryParse(Regex.Match(itemnamecount, @"\d+").Value, out int count);
            string itemname = itemnamecount.Replace(count.ToString(), string.Empty).Trim();
            User destanation = new User(destanationNick, Context.BotCtx.Connection);

            if (await Context.User.RemItemFromInv(Context.BotCtx.Shop, itemname, count))
            {
                destanation.AddItemToInv(Context.BotCtx.Shop, itemname, count);
                await Context.SendMessage(Context.Channel, $"Вы успешно передали {itemname} x{count} пользователю {destanationNick}");
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

            var query = Context.BotCtx.Connection.Table<SQL.UserStats>();

            foreach (var users in query)
            {
                var user = new User(users.Nick, Context.BotCtx.Connection);
                top.Add((users.Nick, user.CountItem(itemname)));
            }

            var result = top.OrderByDescending(p => p.Count).ToList();

            await Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ У КОТОРЫХ ЕСТЬ: " + itemname);

            foreach (var (Name, Count) in result.Take(5))
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Bold + Name + ": " + Count);
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

            var query = Context.BotCtx.Connection.Table<SQL.UserStats>();

            foreach (var users in query)
            {
                top.Add((users.Nick, users.Level));
            }

            var result = top.OrderByDescending(p => p.Count).ToList();

            await Context.SendMessage(Context.Channel, "ТОП 5 ПОЛЬЗОВАТЕЛЕЙ ПО УРОВНЮ");

            foreach (var (Name, Count) in result.Take(5))
            {
                await Context.SendMessage(Context.Channel, IrcClrs.Bold + Name + ": " + Count);
            }
        }

        [Command("use")]
        [Description("Использовать предмет")]
        public async Task Use(string itemname, string nick = null)
        {
            bool delete = false;
            if (Context.User.CountItem(itemname) > 0)
            {
                if (nick != null && nick != Context.Sender)
                {
                    User targetUser = new User(nick, Context.BotCtx.Connection);
                    delete = await Context.BotCtx.Shop.Items[itemname].OnUseOnUser(Context.BotCtx, Context.Channel, Context.User, targetUser);
                }
                else
                {
                    delete = await Context.BotCtx.Shop.Items[itemname].OnUseMyself(Context.BotCtx, Context.Channel, Context.User);
                }
            }
            else
            {
                Context.SendSadMessage(Context.Channel, $"У вас нет предмета {Context.BotCtx.Shop.Items[itemname].Name} чтобы его использовать");
            }

            if (delete)
            {
                await Context.User.RemItemFromInv(Context.BotCtx.Shop, itemname, 1);
                await Context.SendMessage(Context.Channel, $"{IrcClrs.Red}Предмет {Context.BotCtx.Shop.Items[itemname].Name} использован!");
            }
        }
    }
}
