using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class ItemInventory
    {
        public enum ItemType {
            WrenchWeapon,
            WallDestroyer,
            Basic
        }

        public class Shop
        {
            public string Name { get; set; }
            public int Price { get; set; }
            public string Slug { get; set; }
            public bool Sellable { get; set; }
            public ItemType Type { get; set; }
            public int Damage { get; set; } 
        }
    }
}