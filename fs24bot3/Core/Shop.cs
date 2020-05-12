using Newtonsoft.Json;
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
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [МОЛДАВСКОЕ]", Price = 200, Sellable = true, Slug = "wine", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ФРАНЦУНСКОЕ]", Price = 200, Sellable = true, Slug = "winef", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ГРУЗИНСКОЕ]", Price = 200, Sellable = true, Slug = "wineg", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔧 Гаечный ключ", Price = 3000, Sellable = true, Slug = "wrench", Wrenchable = true, WrDamage = 0 });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🛠 Гаечный ключ и молоток", Price = 5000, Sellable = true, Slug = "wrenchadv", Wrenchable = true, WrDamage = 5 });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔊 Мониторные колонки", Price = 320, Sellable = true, Slug = "speaker", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎛 PIONEER DJ", Price = 320, Sellable = true, Slug = "dj", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎹 Native Instruments Komplete Kontrol S88", Price = 600, Sellable = true, Slug = "midikey", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🧱 Укрепление", Price = 15000, Sellable = true, Slug = "wall", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔫 Пистолет", Price = 55000, Sellable = true, Slug = "pistol", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💣 Бомба", Price = 95000, Sellable = true, Slug = "bomb", Wrenchable = false });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💣 Бомба", Price = 95000, Sellable = true, Slug = "bomb", Wrenchable = false });
            // 🎣 
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
                    Log.Verbose("Item aready added: {0}", item.Name);
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
                    var itemToCount = user.GetInventory().Find(item => item.Item.Equals(Shop.GetItem("money").Name));
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
                    if (shopItem.Price >= rand.Next(1000, 100500))
                    {
                        Log.Verbose("Descreaseing price for {0}", shopItem.Name);
                        shopItem.Price -= rand.Next(1, 5);
                    }
                    else
                    {
                        //Log.Verbose("Incresing price for {0}", shopItem.Name);
                        shopItem.Price += rand.Next(1, 5);
                    }
                }
            }
            int checkPayday = rand.Next(0, 100);
            if (checkPayday == 8 && GetMoneyAvg(connect) < 450000)
            {
                Log.Information("Giving payday!");
                var query = connect.Table<Models.SQL.UserStats>();
                foreach (var users in query)
                {
                    UserOperations user = new UserOperations(users.Nick, connect);
                    user.AddItemToInv("money", user.GetUserInfo().Level);
                    if (rand.Next(0, 5) == 1 && user.RemItemFromInv("wall", 1)) 
                    {
                        Log.Information("Breaking wall for {0}", users.Nick);
                    }
                }
                PaydaysCount++;

            }
            DateTime elapsed = DateTime.Now;
            TickSpeed = elapsed.Subtract(start);
        }

        public static Models.ItemInventory.Shop GetItem(string name)
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
