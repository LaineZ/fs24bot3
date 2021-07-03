using System;
using System.Threading.Tasks;

namespace fs24bot3.Models
{
    public class ItemInventory
    {

        public interface IItem
        {
            public string Name { get; }
            public int Price { get; set; }
            public bool Sellable { get; set; }
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
        }

        public class BasicItem : IItem
        {
            public string Name { get; }
            public int Price { get; set; }
            public bool Sellable { get; set; }

            public BasicItem(string name, int price = 0, bool sellabe = true)
            {
                Name = name;
                Price = price;
                Sellable = sellabe;
            }
        }
    }
}