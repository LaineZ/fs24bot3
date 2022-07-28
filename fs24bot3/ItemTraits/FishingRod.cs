using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits;

public class FishingRod : IItem
{
    public string Name { get; }
    public int Price { get; set; }
    public bool Sellable { get; set; }
    public ItemInventory.ItemRarity Rarity { get; set; }

    public FishingRod(string name, int price, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Rare)
    {
        Name = name;
        Price = price;
        Rarity = rarity;
        Sellable = true;
    }

    public async Task<bool> OnUseMyself(Bot botCtx, string channel, Core.User user)
    {
        var rand = new Random();
        var nestName = user.GetFishNest();
        var nest = botCtx.Connection.Table<SQL.FishingNests>().
                    Where(v => v.Name.Equals(nestName)).
                    FirstOrDefault();

        if (nest == null)
        {
            await botCtx.Client.SendMessage(channel, $"{IrcClrs.Gray}Место рыбалки не установлено, используйте @nest");
            return false;
        }

        if (!user.RemItemFromInv(botCtx.Shop, "worm", 1).Result)
        {
            await botCtx.Client.SendMessage(channel, $"{IrcClrs.Gray}У вас нет наживки, @buy worm");
            return false;
        }

        int fishMult = Math.Clamp((20 * nest.Level) - nest.FishCount, 1, int.MaxValue);

        if (rand.Next(1, fishMult) <= user.GetFishLevel())
        {

            var report = new Dictionary<string, IItem>();

            if (nest.Level == 1)
            {
                report = user.AddRandomRarityItem(botCtx.Shop, ItemInventory.ItemRarity.Uncommon, 1, 1, 1);
            }
            if (nest.Level == 2)
            {
                report = user.AddRandomRarityItem(botCtx.Shop, ItemInventory.ItemRarity.Common, 1, 1, 1);
            }
            if (nest.Level == 3)
            {
                report = user.AddRandomRarityItem(botCtx.Shop, ItemInventory.ItemRarity.Rare, 1, 1, 1);
            }

            await botCtx.Client.SendMessage(channel, $"Вы поймали {report.First().Value.Name}");
        }
        else
        {
            await botCtx.Client.SendMessage(channel, $"{IrcClrs.Gray}Рыба сорвалась!");
        }


        if (rand.Next(0, 3) == 1) 
        {
            user.IncreaseFishLevel();
            await botCtx.Client.SendMessage(channel, $"{IrcClrs.Blue}Вы повысили свой уровень рыбалки до {user.GetFishLevel()}");
        }

        bool broken = rand.Next(0, 5) == 1;

        if (broken) { await botCtx.Client.SendMessage(channel, "Ваша удочка сломалась!"); }

        return broken;
    }
}