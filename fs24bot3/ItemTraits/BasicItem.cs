using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits
{
    public class BasicItem : IItem
    {
        public string Name { get; }
        public int Price { get; set; }
        public bool Sellable { get; set; }
        public ItemInventory.ItemRarity Rarity { get; set; }

        public BasicItem(string name, int price = 0, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Common, bool sellabe = true)
        {
            Name = name;
            Price = price;
            Sellable = sellabe;
            Rarity = rarity;
        }
    }
}