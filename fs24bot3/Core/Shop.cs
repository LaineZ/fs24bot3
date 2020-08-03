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
        public static int Sells;
        public static int Buys;

        /// <summary>
        /// Speed of the finiacial operations defalut = 5000 ms
        /// </summary>
        public static int Tickrate = 5000;
        /// <summary>
        /// Tickrate speed - using for @performance command
        /// </summary>
        public static TimeSpan TickSpeed;
        private static Random Rand;
        public static int MaxCap = 150000;

        public static void Init(SQLiteConnection connect)
        {
            Log.Information("loading shop...");
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💰 Деньги", Price = 0, Sellable = false, Slug = "money" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍺 Пиво", Price = 100, Sellable = true, Slug = "beer" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [МОЛДАВСКОЕ]", Price = 150, Sellable = true, Slug = "wine" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ФРАНЦУНСКОЕ]", Price = 150, Sellable = true, Slug = "winef" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ГРУЗИНСКОЕ]", Price = 150, Sellable = true, Slug = "wineg" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔧 Гаечный ключ", Price = 3000, Sellable = true, Slug = "wrench", Type = Models.ItemInventory.ItemType.WrenchWeapon, Damage = 0 });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🛠 Гаечный ключ и молоток", Price = 5000, Sellable = true, Slug = "wrenchadv", Type = Models.ItemInventory.ItemType.WrenchWeapon, Damage = 5 });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔊 Мониторные колонки", Price = 320, Sellable = true, Slug = "speaker" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎛 PIONEER DJ", Price = 320, Sellable = true, Slug = "dj" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎹 Native Instruments Komplete Kontrol S88", Price = 600, Sellable = true, Slug = "midikey" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🧱 Укрепление", Price = 15000, Sellable = true, Slug = "wall" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔫 Пистолет", Price = 5500, Sellable = true, Slug = "pistol", Type = Models.ItemInventory.ItemType.WallDestroyer, Damage = 5 });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💣 Бомба", Price = 9500, Sellable = true, Slug = "bomb", Type = Models.ItemInventory.ItemType.WallDestroyer, Damage = 10 });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐍 Червь", Price = 50, Sellable = true, Slug = "worm" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐟 Рыба", Price = 390, Sellable = true, Slug = "fish" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐠 Тропическая рыба", Price = 1570, Sellable = true, Slug = "tfish" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍥 СТРАННАЯ РЫБА", Price = 10000, Sellable = true, Slug = "weirdfishes" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐡 Рыба-фугу", Price = 370, Sellable = true, Slug = "ffish" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎏 Верхоплавки", Price = 270, Sellable = true, Slug = "veriplace" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🦈 Щука", Price = 1000, Sellable = true, Slug = "pike" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐬 Сом", Price = 1200, Sellable = true, Slug = "som" });
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
                catch (Exception e)
                {
                    Log.Warning("Unrecoverable error while adding item: {0} Reason: {1}", item.Name, e.Message);
                }
            }
            Rand = new Random();
            // 🎣 add 2 new fishing rod
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    connect.Insert(new Models.SQL.FishingRods() { RodName = Core.MessageUtils.GenerateName(Rand.Next(2, 5)), Price = Rand.Next(1000, 5000), FishingLine = Rand.Next(1, 15), HookSize = Rand.Next(1, 5), RodDurabillity = Rand.Next(10, 100) });
                }
                // add 2 new spots
                for (int i = 0; i < 2; i++)
                {
                    connect.Insert(new Models.SQL.FishingNests() { Level = Rand.Next(1, 3), FishCount = Rand.Next(1, 20), FishingLineRequired = Rand.Next(1, 10), Name = Core.MessageUtils.GenerateName(Rand.Next(2, 4)) });
                }
            }
            catch (SQLiteException)
            {
                Log.Verbose("Fishing rod aready added!");
            }

            Log.Information("Done loading preparing shop!");
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
                    Log.Verbose("User {0} have null money", users.Nick);
                }
            }
            return money.Average();
        }

        public static void Update(SQLiteConnection connect)
        {
            DateTime start = DateTime.Now;
            foreach (var shopItem in ShopItems)
            {
                int check = Rand.Next(0, 10);
                if (check == 5)
                {
                    if (shopItem.Price >= Rand.Next(5800, 100500))
                    {
                        Log.Verbose("Descreaseing price for {0}", shopItem.Name);
                        shopItem.Price -= Rand.Next(1, 5);
                    }
                    else
                    {
                        //Log.Verbose("Incresing price for {0}", shopItem.Name);
                        shopItem.Price += Rand.Next(1, 2);
                    }
                }
            }
            int checkPayday = Rand.Next(0, 100);
            if (checkPayday == 8 && GetMoneyAvg(connect) < MaxCap)
            {
                Log.Information("Giving payday!");
                var query = connect.Table<Models.SQL.UserStats>();
                foreach (var users in query)
                {
                    UserOperations user = new UserOperations(users.Nick, connect);
                    user.AddItemToInv("money", user.GetUserInfo().Level);
                    if (Rand.Next(0, 5) == 1 && user.RemItemFromInv("wall", 1))
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
