using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.ItemTraits;
using fs24bot3.Models;
using Serilog;

namespace fs24bot3.Systems
{
    public class Shop
    {
        public Dictionary<string, ItemInventory.IItem> Items { get; private set; }
        private readonly Random Rand = new Random();

        public int Sells { get; private set; }
        public int Buys { get; private set; }
        private Bot BotCtx { get; }
        public int MaxCap = 250000;
        public int PaydaysCount = 0;

        public string ShopID { get; }

        public Shop(Bot botCtx)
        {
            Items = new Dictionary<string, ItemInventory.IItem>();
            ShopID = "shop";

            Items.Add("money", new ItemInventory.BasicItem("💰 Деньги", 0, ItemInventory.ItemRarity.Common, false));
            Items.Add("beer", new Drink("🍺 Пиво", 1, 1000, ItemInventory.ItemRarity.Uncommon));
            Items.Add("wine", new Drink("🍷 Вино [МОЛДАВСКОЕ]", 3, 1500, ItemInventory.ItemRarity.Rare));
            Items.Add("winef", new Drink("🍷 Вино [ФРАНЦУЗСКОЕ]", 2, 1500, ItemInventory.ItemRarity.Rare));
            Items.Add("wineg", new Drink("🍷 Вино [ГРУЗИНСКОЕ]", 4, 2980, ItemInventory.ItemRarity.Rare));
            Items.Add("wrench", new Wrenchable("🔧 Гаечный ключ", 4, 30000, ItemInventory.ItemRarity.Rare));
            Items.Add("wrenchadv", new Wrenchable("🛠 Гаечный ключ и молоток", 8, 50000, ItemInventory.ItemRarity.Epic));
            Items.Add("hammer", new Wrenchable("🔨 Молоток", 5, 35000, ItemInventory.ItemRarity.Rare));
            Items.Add("speaker", new ItemInventory.BasicItem("🔊 Мониторные колонки", 3200, ItemInventory.ItemRarity.Common));
            Items.Add("dj", new ItemInventory.BasicItem("🎛 PIONEER DJ", 3200, ItemInventory.ItemRarity.Common));
            Items.Add("midikey", new ItemInventory.BasicItem("🎹 Native Instruments Komplete Kontrol S88", 6000, ItemInventory.ItemRarity.Rare));
            Items.Add("wall", new ItemInventory.BasicItem("🧱 Укрепление", 150000, ItemInventory.ItemRarity.Legendary));
            Items.Add("pistol", new Bomb("🔫 Пистолет", 5500, 50000, ItemInventory.ItemRarity.Rare));
            Items.Add("bomb", new Bomb("💣 Бомба", 9500, 90000, ItemInventory.ItemRarity.Unbeliveable));
            Items.Add("worm", new ItemInventory.BasicItem("🐍 Червь", 500, ItemInventory.ItemRarity.Common));
            Items.Add("fish", new ItemInventory.BasicItem("🐟 Рыба", 1000, ItemInventory.ItemRarity.Uncommon));
            Items.Add("tfish", new ItemInventory.BasicItem("🐠 Тропическая рыба", 15700, ItemInventory.ItemRarity.Rare));
            Items.Add("weirdfishes", new ItemInventory.BasicItem("🍥 СТРАННАЯ РЫБА", 100000, ItemInventory.ItemRarity.Unbeliveable));
            Items.Add("ffish", new ItemInventory.BasicItem("🐡 Рыба-фугу", 3700, ItemInventory.ItemRarity.Rare));
            Items.Add("veriplace", new ItemInventory.BasicItem("🎏 Верхоплавки", 2700, ItemInventory.ItemRarity.Common));
            Items.Add("pike", new ItemInventory.BasicItem("🦈 Щука", 10000, ItemInventory.ItemRarity.Uncommon));
            Items.Add("som", new ItemInventory.BasicItem("🐬 Сом", 12000, ItemInventory.ItemRarity.Rare));
            Items.Add("line", new ItemInventory.BasicItem("🪢 Леска", 1000, ItemInventory.ItemRarity.Common));
            Items.Add("rod", new FishingRod("🎣 Удочка", 20000, ItemInventory.ItemRarity.Uncommon));

            BotCtx = botCtx;

            foreach (var item in Items)
            {
                if (!BotCtx.Connection.Table<SQL.Item>().Where(x => x.Name == item.Key).Any())
                {
                    BotCtx.Connection.Insert(new SQL.Item { ShopID = ShopID, Name = item.Key });
                    Log.Verbose("Inserted: {0}", item.Value.Name);
                }
            }

            Log.Information("Shop loading is done!");
        }

        public void UpdateShop()
        {
            foreach (var shopItem in Items)
            {
                int check = Rand.Next(0, 30);
                if (check == 5)
                {
                    if (shopItem.Value.Price >= Rand.Next(5800, 100500))
                    {
                        Log.Verbose("Decreasing price for {0}", shopItem.Value.Name);
                        shopItem.Value.Price -= Rand.Next(1, 3);
                    }
                }

            }
        }
                
        public async Task<(bool, int)> Sell(User user, string itemname, int count = 1)
        {
            if (!Items.ContainsKey(itemname))
            {
                throw new Exceptions.ItemNotFoundException();
            }

            if (Items[itemname].Sellable && await user.RemItemFromInv(this, itemname, count))
            {
                // tin
                int sellprice = (int)Math.Floor((decimal)(Items[itemname].Price * count) / 2);
                Sells++;
                user.AddItemToInv(this, "money", sellprice);
                Items[itemname].Price -= Rand.Next(1, sellprice);
                return (true, sellprice);
            }
            else
            {
                Log.Warning("Cannot sell {0}", itemname);
                return (false, 0);
            }
        }

        public async Task<(bool, int)> Buy(User user, string itemname, int count = 1)
        {
            if (!Items.ContainsKey(itemname))
            {
                throw new Exceptions.ItemNotFoundException();
            }

            int buyprice = Items[itemname].Price * count;
            bool sucessfully = await user.RemItemFromInv(this, "money", buyprice);

            if (sucessfully)
            {
                user.AddItemToInv(this, itemname, count);
                Items[itemname].Price += Rand.Next(1, 1000);
                Buys++;
            }
            return (sucessfully, buyprice);
        }
    }
}