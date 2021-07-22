using System;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits
{
        public class Bomb : ItemInventory.IItem
    {
            public string Name { get; }
            public int Price { get; set; }
            public bool Sellable { get; set; }
            private int Damage { get; }

            public Bomb(string name, int damage, int price, bool sellabe = true)
            {
                Name = name;
                Price = price;
                Sellable = sellabe;
                Damage = damage;
            }
            public async Task<bool> OnUseOnUser(Bot botCtx, string channel, Core.User user, Core.User targetUser)
            {
                var rand = new Random();

                if (rand.Next(0, 10 * targetUser.CountItem("wall") - Damage) == 0)
                {
                    await targetUser.RemItemFromInv(botCtx.Shop, "wall", 1);

                    int xp = rand.Next(100, 500 + user.GetUserInfo().Level);
                    user.IncreaseXp(xp);

                    await botCtx.SendMessage(channel, $"Вы использовали {Name} с уроном {Damage} на пользователе {targetUser.Username} при этом вы сломали укрепление! и за это вам +{xp} XP");
                    if (rand.Next(0, 7) == 2)
                    {
                        await botCtx.SendMessage(targetUser.Username, $"Вас атакует {user.Username}!");
                    }
                    else
                    {
                        if (rand.Next(0, 1) == 1 || await targetUser.RemItemFromInv(botCtx.Shop, "speaker", 1))
                        {
                            await botCtx.SendMessage(targetUser.Username, $"Вас атакует {user.Username}! Так как у вас мониторные колонки - вы получили это сообщение немедленно, но берегитесь: колонки не бесконечные!");
                        }
                    }
                }
                else
                {
                    await botCtx.SendMessage(channel, RandomMsgs.GetRandomMessage(RandomMsgs.MissMessages));
                }

                return true;
            }
        }
}