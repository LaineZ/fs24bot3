﻿using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class ItemInventory
    {
        public class Item
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }

        public class Shop
        {
            public string Name { get; set; }
            public int Price { get; set; }
            public string Slug { get; set; }
            public bool Sellable { get; set; }
        }

        public class Inventory
        {
            public List<Item> Items { get; set; }
        }
    }
}