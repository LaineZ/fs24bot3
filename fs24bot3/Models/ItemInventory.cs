using System;
using System.Threading.Tasks;

namespace fs24bot3.Models
{
    public class ItemInventory
    {

        public enum ItemRarity
        {
            Common = 1,
            Uncommon = 2,
            Rare = 3,
            Epic = 4,
            Legendary = 5,
            Unbeliveable = 6
        }

        public interface IItem
        {
            public string Name { get; }
            public int Price { get; set; }
            public bool Sellable { get; set; }
            public ItemRarity Rarity { get; set; }

            public async Task<bool> OnUseMyself(Bot botCtx, string channel, Core.User user)
            {
                await botCtx.SendMessage(channel, "Этот предмет невозможно использовать на себе!");
                return false;
            }
            public async Task<bool> OnUseOnUser(Bot botCtx, string channel, Core.User user, Core.User targetUser)
            {
                await botCtx.SendMessage(channel, "Этот предмет невозможно применить на другом пользователе!");
                return false;
            }

            public bool OnAdd(Bot botCtx, string channel, Core.User user, Core.User targetUser)
            {
                return false;
            }

            public bool OnDel(Bot botCtx, string channel, Core.User user, Core.User targetUser)
            {
                return false;
            }
        }

        public class BasicItem : IItem
        {
            public string Name { get; }
            public int Price { get; set; }
            public bool Sellable { get; set; }
            public ItemRarity Rarity { get; set; }

            public BasicItem(string name, int price = 0, ItemRarity rarity = ItemRarity.Common, bool sellabe = true)
            {
                Name = name;
                Price = price;
                Sellable = sellabe;
                Rarity = rarity;
            }
        }
    }
}