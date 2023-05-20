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
            await botCtx.Client.SendMessage(channel, $"[gray]Место рыбалки не установлено, используйте @nest");
            return false;
        }

        if (!user.RemItemFromInv(botCtx.Shop, "worm", 1).Result)
        {
            await botCtx.Client.SendMessage(channel, $"[gray]У вас нет наживки (worm)");
            return false;
        }

        int fishMult = Math.Clamp((20 * nest.Level) - nest.FishCount, 1, int.MaxValue);

        if (rand.Next(1, fishMult) < user.GetFishLevel())
        {
            var report = new Dictionary<string, IItem>();

            switch (nest.Level)
            {
                case 1:
                    report = user.AddRandomRarityItem(botCtx.Shop, ItemInventory.ItemRarity.Uncommon);
                    break;
                case 2:
                    report = user.AddRandomRarityItem(botCtx.Shop, ItemInventory.ItemRarity.Common);
                    break;
                case 3:
                    report = user.AddRandomRarityItem(botCtx.Shop, ItemInventory.ItemRarity.Rare);
                    break;
                default:
                    break;
            }

            await botCtx.Client.SendMessage(channel, $"Вы поймали {report.First().Value.Name}");
        }
        else
        {
            await botCtx.Client.SendMessage(channel, $"[gray]Рыба сорвалась!");
        }


        if (rand.Next(0, 5) == 1) 
        {
            user.IncreaseFishLevel();
            await botCtx.Client.SendMessage(channel, $"[blue]Вы повысили свой уровень рыбалки до {user.GetFishLevel()}");
        }

        bool broken = rand.Next(0, 5) == 1;

        if (broken) { await botCtx.Client.SendMessage(channel, "Ваша удочка сломалась!"); }

        bool brokenLine = rand.Next(0, 3) == 1;

        if (brokenLine) 
        {
            await user.RemItemFromInv(botCtx.Shop, "line", 1);
        }

        return broken;
    }
}