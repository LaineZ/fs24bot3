﻿using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace fs24bot3
{
    public static class Shop
    {
        public static List<Models.ItemInventory.Shop> ShopItems = new List<Models.ItemInventory.Shop>();
        public static int PaydaysCount;
        private static Random rand;

        public static void Init()
        {
            Log.Information("loading shop...");
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💰 Деньги", Price = 0, Sellable = false, Slug = "money" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍺 Пиво", Price = 200, Sellable = true, Slug = "beer" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [МОЛДАВСКОЕ]", Price = 200, Sellable = true, Slug = "wine" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔧 Гаечный ключ", Price = 300, Sellable = true, Slug = "wrench" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🛠 Гаечный ключ и молоток", Price = 400, Sellable = true, Slug = "wrenchadv" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔊 Мониторные колонки", Price = 320, Sellable = true, Slug = "speaker" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎛 PIONEER DJ", Price = 320, Sellable = true, Slug = "dj" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎹 Native Instruments Komplete Kontrol S88", Price = 600, Sellable = true, Slug = "midikey" });
            Log.Information("done");
            rand = new Random();
        }

        public static void Update(SQLite.SQLiteConnection connect)
        {
            foreach (var shopItem in ShopItems)
            {
                int check = rand.Next(0, 10);
                if (check == 5)
                {
                    if (shopItem.Price >= GetMoneyAvg(connect))
                    {
                        Log.Verbose("Descreaseing price for {0}", shopItem.Name);
                        shopItem.Price -= 5;
                    }
                    else
                    {
                        Log.Verbose("Incresing price for {0}", shopItem.Name);
                        shopItem.Price += 1;
                    }
                }
            }
            int checkPayday = rand.Next(0, 100);
            if (checkPayday == 8)
            {
                Log.Information("Giving payday!");
                var query = connect.Table<Models.SQL.UserStats>();
                foreach (var users in query)
                {
                    UserOperations user = new UserOperations(users.Nick, connect);
                    user.AddItemToInv("money", user.GetUserInfo().Level);
                }
                PaydaysCount++;
            }
        }

        public static double GetMoneyAvg(SQLite.SQLiteConnection connect)
        {
            var money = new List<int>();

            var query = connect.Table<Models.SQL.UserStats>();
            foreach (var users in query)
            {
                UserOperations user = new UserOperations(users.Nick, connect);
                var userinfo = user.GetUserInfo();
                var userInv = JsonConvert.DeserializeObject<Models.ItemInventory.Inventory>(userinfo.JsonInv);
                int itemToCount = userInv.Items.FindIndex(item => item.Name.Equals(Shop.getItem("money").Name));
                if (itemToCount >= 0)
                {
                    money.Add(userInv.Items[itemToCount].Count);
                }
            }
            return money.Average();
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
