using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3
{
    public static class Shop
    {
        public static List<Models.ItemInventory.Shop> ShopItems = new List<Models.ItemInventory.Shop>();

        public static void Init()
        {
            Log.Information("loading shop...");
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💰 Деньги", Price = 0, Sellable = false, Slug = "money" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍺 Пиво", Price = 500, Sellable = true, Slug = "beer" });
            Log.Information("done");
        }

        public static Models.ItemInventory.Shop getItem(string name)
        {
            foreach (var item in ShopItems)
            {
                Log.Verbose("items: {0} {1} need: {2}", item.Name, item.Slug, name);
                if (item.Name == name || item.Slug == name)
                {
                    return item;
                }
            }

            throw new Exception("Item with name: " + name + " not found!");
        }
    }
}
