using System;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits;

public class Bomb : IItem
{
    public string Name { get; }
    public int Price { get; set; }
    public bool Sellable { get; set; }
    private int Damage { get; }
    public ItemInventory.ItemRarity Rarity { get; set; }

    public Bomb(string name, int damage, int price, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Rare)
    {
        Name = name;
        Price = price;
        Rarity = rarity;
        Sellable = true;
        Damage = damage;
    }

    public async Task<bool> OnUseOnUser(Bot botCtx, string channel, Core.User user, Core.User targetUser)
    {
        var rand = new Random();

        if (targetUser.CountItem("wall") - Damage < 0)
        {
            await botCtx.Client.SendMessage(channel, RandomMsgs.MissMessages.Random());
            return false;
        }

        if (rand.Next(0, 10 * targetUser.CountItem("wall") - Damage) == 0)
        {
            await targetUser.RemItemFromInv(botCtx.Shop, "wall", 1);

            int xp = rand.Next(100, 500 + user.GetUserInfo().Level);
            user.IncreaseXp(xp);

            await botCtx.Client.SendMessage(channel, $"Вы использовали {Name} с уроном {Damage} на пользователе {targetUser.Username} при этом вы сломали укрепление! и за это вам +{xp} XP");
            if (rand.Next(0, 7) == 2)
            {
                await botCtx.Client.SendMessage(targetUser.Username, $"Вас атакует {user.Username}!");
            }
            else
            {
                if (rand.Next(0, 1) == 1 || await targetUser.RemItemFromInv(botCtx.Shop, "speaker", 1))
                {
                    await botCtx.Client.SendMessage(targetUser.Username, $"Вас атакует {user.Username}! Так как у вас мониторные колонки - вы получили это сообщение немедленно, но берегитесь: колонки не бесконечные!");
                }
            }
        }
        else
        {
            await botCtx.Client.SendMessage(channel, RandomMsgs.MissMessages.Random());
        }

        return true;
    }
}