using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.Properties;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace fs24bot3.Commands;
public sealed class InventoryCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
{

    public CommandService Service { get; set; }

    private async Task BuyOrSellInternal(string itemnamecount, bool sell = false)
    {
        int.TryParse(Regex.Match(itemnamecount, @"\d+").Value, out var count);
        count = Math.Clamp(count, 1, int.MaxValue);
        string itemname = Regex.Replace(itemnamecount, @"\d+", string.Empty).Trim();

        var (success, price) = (false, 0);

        if (sell)
        {
            (success, price) = await Context.BotCtx.Shop.Sell(Context.User, itemname, count);
        }
        else
        {
            (success, price) = await Context.BotCtx.Shop.Buy(Context.User, itemname, count);
        }

        if (success)
        {
            await Context.SendMessage(Context.Channel,
            $"{IrcClrs.Green}Вы успешно {(sell ? "продали" : "купили")} {Context.BotCtx.Shop.Items[itemname].Name} x{count} за {price} денег");
        }
        else
        {
            if (Context.BotCtx.Shop.Items[itemname].Sellable)
            {
                Context.SendSadMessage(Context.Channel,
                $"Данный предмет невозможно {(sell ? "продать" : $"купить. Недостаточно денег: {price}")}.");
            }
            else
            {
                Context.SendSadMessage(Context.Channel, $"У вас нет такого предмета!");
            }
        }
    }

    [Command("shop")]
    [Description("Магазин")]
    public async Task Shop()
    {
        var shopitems = string.Join(' ',
        Context.BotCtx.Shop.Items
        .Where(x => x.Value.Sellable)
        .Select(x => $"<div class=\"shopbox\"><p>{x.Value.Name} {x.Key}</p><h1></h1><p> Цена: {x.Value.Price}</p></div>"));

        var shopTemplate = Resources.shop.Replace("[SHOPITEMS]", shopitems);

        var http = new HttpTools();

        await Context.SendMessage(Context.Channel, await Helpers.InternetServicesHelper.UploadToTrashbin(shopTemplate));
    }

    [Command("inv", "inventory")]
    [Checks.FullAccount]
    [Description("Инвентарь. Параметр useSlugs отвечает за показ id предмета для команд buy/sell/transfer и других")]
    public async Task Userstat(bool useSlugs = false)
    {
        var userInv = Context.User.GetInventory();
        if (userInv.Count > 0)
        {
            if (!useSlugs)
            {
                await Context.SendMessage(Context.Channel,
                Context.User.Username + ": " + string.Join(" ", userInv.Select(x => $"{Context.BotCtx.Shop.Items[x.Item].Name} x{x.ItemCount}")));
            }
            else
            {
                await Context.SendMessage(Context.Channel,
                Context.User.Username + ": " + string.Join(" ", userInv.Select(x => $"{x.Item}({Context.BotCtx.Shop.Items[x.Item].Name}) x{x.ItemCount}")));
            }
        }
        else
        {
            await Context.SendMessage(Context.Channel,
            $"{IrcClrs.Gray}У вас ничего нет в инвентаре... Хотите сходить в магазин? {Context.User.GetUserPrefix()}shop -> {ConfigurationProvider.Config.Prefix}helpcmd buy");
        }
    }

    [Command("buy")]
    [Checks.FullAccount]
    [Description("Купить товар")]
    public async Task Buy([Remainder] string itemnamecount)
    {
        Context.User.EnableSilentMode();
        await BuyOrSellInternal(itemnamecount, false);
    }

    [Command("sell")]
    [Checks.FullAccount]
    [Description("Продать товар")]
    public async Task Sell([Remainder] string itemnamecount)
    {
        Context.User.EnableSilentMode();
        await BuyOrSellInternal(itemnamecount, true);
    }

    [Command("sellall")]
    [Checks.FullAccount]
    [Description("Продать весь товар")]
    public async Task SellAll()
    {
        Context.User.EnableSilentMode();
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
    [Checks.FullAccount]
    [Description("Передать вещи")]
    public async Task Transfer(string destanationNick, [Remainder] string itemnamecount)
    {
        Context.User.EnableSilentMode();
        int.TryParse(Regex.Match(itemnamecount, @"\d+").Value, out int count);
        string itemname = itemnamecount.Replace(count.ToString(), string.Empty).Trim();
        User destanation = new User(destanationNick, in Context.BotCtx.Connection);

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
            var user = new User(users.Nick, in Context.BotCtx.Connection);
            top.Add((users.Nick, user.CountItem(itemname)));
        }

        var result = top.OrderByDescending(p => p.Count).ToList();

        await Context.SendMessage(Context.Channel, $"ТОП 5 ПОЛЬЗОВАТЕЛЕЙ У КОТОРЫХ ЕСТЬ: {itemname}");

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

    [Command("tags")]
    [Description("Список всех тегов")]
    public async Task Tags()
    {
        var tags = Context.BotCtx.Connection.Table<SQL.Tag>().
                   ToList().Select(x => $"00,{x.Color}⚫{x.Name}{IrcClrs.Reset}");
        await Context.SendMessage(Context.Channel, string.Join(" ", tags));
    }

    [Command("tag")]
    [Checks.FullAccount]
    [Description("Управление тегами")]
    public async Task TagManager(CommandToggles.CommandEdit action, string tagname, ushort irccolor = 4)
    {
        Context.User.SetContext(Context);

        switch (action)
        {
            case CommandToggles.CommandEdit.Add:
                if (await Context.User.RemItemFromInv(Context.BotCtx.Shop, "money", 30000))
                {
                    try
                    {
                        Context.BotCtx.Connection.Insert(new SQL.Tag
                        {
                            Color = (uint)irccolor,
                            CreatedBy = Context.User.Username,
                            Name = tagname
                        });
                        await Context.SendMessage(Context.Channel, 
                        $"Тег успешно добавлен чтобы кого-то наградить им напишите {Context.User.GetUserPrefix()}addtag пользователь {tagname}!");
                    }
                    catch (SQLiteException)
                    {
                        Context.User.AddItemToInv(Context.BotCtx.Shop, "money", 3000);
                        Context.SendSadMessage(Context.Channel, "Тег уже существует!");
                    }
                }
                break;
            case CommandToggles.CommandEdit.Delete:
                var query = Context.BotCtx.Connection.Table<SQL.Tag>().Where(v => v.Name.Equals(tagname)).FirstOrDefault();

                if (query != null && query.Name == tagname)
                {
                    if (query.CreatedBy == Context.User.Username || Context.User.GetUserInfo().Admin == 2)
                    {
                        Context.BotCtx.Connection.Table<SQL.Tag>().Where(v => v.Name.Equals(tagname)).Delete();
                        await Context.SendMessage(Context.Channel, "Тег успешно удален, также этот тег будет удален с пользователей!");
                    }
                    else
                    {
                        Context.SendSadMessage(Context.Channel, "Вы не создатель тега!");
                    }
                }
                else
                {
                    Context.SendSadMessage(Context.Channel);
                }
                break;
        }
    }

    [Command("addtag")]
    [Checks.FullAccount]
    [Description("Добавить тег пользователю")]
    public async Task AddTag(string tagname, string destanation)
    {
        var query = Context.BotCtx.Connection.Table<SQL.Tag>().Where(v => v.Name.Equals(tagname)).FirstOrDefault();

        if (query == null)
        {
            Context.SendSadMessage(Context.Channel, "Тег не найден");
        }

        var destuser = new User(destanation, Context.BotCtx.Connection, Context);
        destuser.AddTag(query);

        await Context.SendMessage(Context.Channel, "Тег успешно добавлен пользователю");
    }

    [Command("use")]
    [Checks.FullAccount]
    [Description("Использовать предмет")]
    public async Task Use(string itemname, string nick = null)
    {
        Context.User.EnableSilentMode();
        bool delete = false;
        if (Context.User.CountItem(itemname) > 0)
        {
            if (nick != null && nick != Context.User.Username)
            {
                User targetUser = new User(nick, in Context.BotCtx.Connection);
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
