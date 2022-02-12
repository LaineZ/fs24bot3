using System;
using System.Linq;
using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits
{
    public class Drink : ItemInventory.IItem
    {
        public string Name { get; }
        public int Price { get; set; }
        public bool Sellable { get; set; }
        private int DrunkLevel { get; }
        public ItemInventory.ItemRarity Rarity { get; set; }

        public Drink(string name, int drunk, int price, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Common)
        {
            Name = name;
            Price = price;
            Sellable = true;
            Rarity = rarity;
            DrunkLevel = drunk;
        }
        public async Task<bool> OnUseMyself(Bot botCtx, string channel, User user)
        {
            var rand = new Random();
            var sms = botCtx.MessageBus.Where(x => x.Prefix.From == user.Username && !x.Trailing.StartsWith(ConfigurationProvider.Config.Prefix)).OrderBy(s => rand.Next(0, 2) == 1);

            if (sms.Any())
            {
                string randStr = string.Join(" ", sms.First().Trailing.Split(" ").OrderBy(s => (rand.Next(1, 5)) <= DrunkLevel));
                string message = await Core.Transalator.TranslatePpc(randStr, "ru");
                await botCtx.SendMessage(channel, $"{user.Username} выпил {Name} ({DrunkLevel}) и сказал: {message}");
            }
            else
            {
                await botCtx.SendMessage(channel, $"Вы недостаточно выпили...");
            }

            return true;
        }
    }
}