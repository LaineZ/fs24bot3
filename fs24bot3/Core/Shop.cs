using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public static int Tickrate = 15000;
        /// <summary>
        /// Tickrate speed - using for @performance command
        /// </summary>
        public static TimeSpan TickSpeed;
        private static Random Rand;
        public static int MaxCap = 150000;
        // TOOD: Refactor
        public static string SongameString = String.Empty;
        public static int SongameTries = 5;

        public static void Init(SQLiteConnection connect)
        {
            Log.Information("Loading shop...");
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💰 Деньги", Price = 0, Sellable = false, Slug = "money" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍺 Пиво", Price = 100, Sellable = true, Slug = "beer" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [МОЛДАВСКОЕ]", Price = 150, Sellable = true, Slug = "wine" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ФРАНЦУЗСКОЕ]", Price = 150, Sellable = true, Slug = "winef" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍷 Вино [ГРУЗИНСКОЕ]", Price = 150, Sellable = true, Slug = "wineg" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔧 Гаечный ключ", Price = 3000, Sellable = true, Slug = "wrench", });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🛠 Гаечный ключ и молоток", Price = 5000, Sellable = true, Slug = "wrenchadv" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔨 Молоток", Price = 3500, Sellable = true, Slug = "hammer" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔊 Мониторные колонки", Price = 320, Sellable = true, Slug = "speaker" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎛 PIONEER DJ", Price = 320, Sellable = true, Slug = "dj" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎹 Native Instruments Komplete Kontrol S88", Price = 600, Sellable = true, Slug = "midikey" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🧱 Укрепление", Price = 15000, Sellable = true, Slug = "wall" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🔫 Пистолет", Price = 5500, Sellable = true, Slug = "pistol" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💣 Бомба", Price = 9500, Sellable = true, Slug = "bomb" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💣 Насос", Price = 500, Sellable = true, Slug = "pump" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐍 Червь", Price = 50, Sellable = true, Slug = "worm" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐟 Рыба", Price = 390, Sellable = true, Slug = "fish" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐠 Тропическая рыба", Price = 1570, Sellable = true, Slug = "tfish" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🍥 СТРАННАЯ РЫБА", Price = 10000, Sellable = true, Slug = "weirdfishes" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐡 Рыба-фугу", Price = 370, Sellable = true, Slug = "ffish" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🎏 Верхоплавки", Price = 270, Sellable = true, Slug = "veriplace" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🦈 Щука", Price = 1000, Sellable = true, Slug = "pike" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "🐬 Сом", Price = 1200, Sellable = true, Slug = "som" });
            ShopItems.Add(new Models.ItemInventory.Shop() { Name = "💧 Вода", Price = 1, Sellable = true, Slug = "water" });

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
                    connect.Insert(new Models.SQL.FishingNests() { Level = Rand.Next(1, 3), FishCount = Rand.Next(1, 20), FishingLineRequired = Rand.Next(1, 10), Name = Core.MessageUtils.GenerateName(Rand.Next(2, 4)) });
                }
            }
            catch (SQLiteException)
            {
                Log.Verbose("Fishing rod aready added!");
            }

            Log.Information("Done loading preparing shop!");
        }

        public static void UpdateShop()
        {
            foreach (var shopItem in Shop.ShopItems)
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
