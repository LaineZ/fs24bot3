using System;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits
{
        public class Wrenchable : ItemInventory.IItem
    {
            public string Name { get; }
            public int Price { get; set; }
            public bool Sellable { get; set; }
            private int Damage { get; }
            public ItemInventory.ItemRarity Rarity { get; set; }

        public Wrenchable(string name, int damage, int price, ItemInventory.ItemRarity rarity)
            {
                Name = name;
                Price = price;
                Sellable = true;
                Rarity = rarity;
                Damage = damage;
            }
            public async Task<bool> OnUseOnUser(Bot botCtx, string channel, Core.User user, Core.User targetUser)
            {
                var rand = new Random();
                var takeItems = targetUser.GetInventory();

                if (rand.Next(0, 10 + targetUser.CountItem("wall") - Damage) == 0)
                {
                    int indexItem = rand.Next(takeItems.Count);
                    int itemCount = 1;

                    if (takeItems[indexItem].ItemCount / (15 - Damage) > 0)
                    {
                        itemCount = rand.Next(1, takeItems[indexItem].ItemCount / (15 - Damage));
                    }

                    user.AddItemToInv(botCtx.Shop, takeItems[indexItem].Item, itemCount);
                    await targetUser.RemItemFromInv(botCtx.Shop, takeItems[indexItem].Item, itemCount);

                    int xp = rand.Next(100, 500 + user.GetUserInfo().Level);
                    string itemnameLocaled = botCtx.Shop.Items[takeItems[indexItem].Item].Name;


                    user.IncreaseXp(xp);

                    await botCtx.SendMessage(channel, $"Вы кинули {Name} с уроном {Damage} в пользователя {targetUser.Username} при этом он потерял {itemnameLocaled} x{itemCount} и за это вам +{xp} XP");
                    if (rand.Next(0, 7) == 2)
                    {
                        await botCtx.SendMessage(targetUser.Username, $"Вас атакует {user.Username}! Вы уже потеряли {itemnameLocaled} x{itemCount} возможно он вас продолжает атаковать!");
                    }
                    else
                    {
                        if (rand.Next(0, 1) == 1 || await targetUser.RemItemFromInv(botCtx.Shop, "speaker", 1))
                        {
                            await botCtx.SendMessage(targetUser.Username, $"Вас атакует {user.Username} гаечными ключами! Вы потеряли {itemnameLocaled} x{itemCount}! Так как у вас мониторные колонки - вы получили это сообщение немедленно, но берегитесь: колонки не бесконечные!");
                        }
                    }
                }
                else
                {
                    await botCtx.SendMessage(channel, RandomMsgs.MissMessages.Random());
                }

                return true;
            }
        }
}