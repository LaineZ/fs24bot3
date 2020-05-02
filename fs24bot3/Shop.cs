﻿using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SQLite;

namespace fs24bot3
{
    public static class Shop
    {
        public static List<Models.ItemInventory.Shop> ShopItems = new List<Models.ItemInventory.Shop>();
        public static int PaydaysCount;

        /// <summary>
        /// Speed of the finiacial operations defalut = 2000 ms
        /// </summary>
        public static int Tickrate = 5000;
        /// <summary>
        /// Tickrate speed - using for @performance command
        /// </summary>
        public static TimeSpan TickSpeed;
        private static Random rand;

        public static void Init(SQLiteConnection connect)
        {
            Log.Information("loading shop...");
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💰 Деньги", Price = 0, Sellable = false, Slug = "money" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍺 Пиво", Price = 200, Sellable = true, Slug = "beer" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [МОЛДАВСКОЕ]", Price = 200, Sellable = true, Slug = "wine" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ФРАНЦУНСКОЕ]", Price = 200, Sellable = true, Slug = "winef" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ГРУЗИНСКОЕ]", Price = 200, Sellable = true, Slug = "wineg" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔧 Гаечный ключ", Price = 3000, Sellable = true, Slug = "wrench" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🛠 Гаечный ключ и молоток", Price = 5000, Sellable = true, Slug = "wrenchadv" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔊 Мониторные колонки", Price = 320, Sellable = true, Slug = "speaker" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎛 PIONEER DJ", Price = 320, Sellable = true, Slug = "dj" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎹 Native Instruments Komplete Kontrol S88", Price = 600, Sellable = true, Slug = "midikey" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🧱 Укрепление", Price = 15000, Sellable = true, Slug = "wall" });

            foreach (var item in ShopItems)
            {
                var sqlItem = new Models.SQL.Item()
                {
                    Name = item.Name
                };
                Log.Verbose("Inserting: {0}", item.Name);
                try
                {
                    connect.Insert(sqlItem);
                }
                catch (SQLiteException)
                {
                    Log.Verbose("Item aready addeded: {0}", item.Name);
                }
                catch (Exception)
                {
                    Log.Verbose("пиздец я хз чё вообще произошло");
                }
            }
            Log.Information("done");
            rand = new Random();
        }

        internal static double GetMoneyAvg(SQLiteConnection connection)
        {
            List<int> money = new List<int>();   

            var query = connection.Table<Models.SQL.UserStats>();
            foreach (var users in query)
            {
                try
                {
                    UserOperations user = new UserOperations(users.Nick, connection);
                    var itemToCount = user.GetInventory().Find(item => item.Item.Equals(Shop.getItem("money").Name));
                    money.Add(itemToCount.ItemCount);
                }
                catch (NullReferenceException)
                {
                    Log.Warning("User {0} have null money that's werid", users.Nick);
                }
            }
            return money.Average();
        }

        public static void Update(SQLiteConnection connect)
        {
            DateTime start = DateTime.Now;
            foreach (var shopItem in ShopItems)
            {
                int check = rand.Next(0, 10);
                if (check == 5)
                {
                    if (shopItem.Price >= rand.Next(1000, 15000))
                    {
                        Log.Verbose("Descreaseing price for {0}", shopItem.Name);
                        shopItem.Price -= 5;
                    }
                    else
                    {
                        //Log.Verbose("Incresing price for {0}", shopItem.Name);
                        shopItem.Price += rand.Next(1, 5);
                    }
                }
            }
            int checkPayday = rand.Next(0, 100);
            if (checkPayday == 8 && GetMoneyAvg(connect) > 500000)
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
            DateTime elapsed = DateTime.Now;
            TickSpeed = elapsed.Subtract(start);
        }

        public static Models.ItemInventory.Shop getItem(string name)
        {
            foreach (var item in ShopItems)
            {
                //Log.Verbose("items: {0} {1} need: {2}", item.Name, item.Slug, name);
                if (item.Name == name || item.Slug == name)
                {
                    return item;
                }
            }

          throw new Exception("Item with name: " + name + " not found!");
        }
    }
}
