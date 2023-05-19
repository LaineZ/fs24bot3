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
        public Dictionary<string, IItem> Items { get; private set; }
        private readonly Random Rand = new Random();

        public int Sells { get; private set; }
        public int Buys { get; private set; }
        private Bot BotCtx { get; }
        public int MaxCap = 250000;
        public int PaydaysCount = 0;

        public string ShopID { get; }

        public Shop(in Bot botCtx)
        {
            Items = new Dictionary<string, IItem>();
            ShopID = "shop";

            Items.Add("money", new BasicItem("💰 Деньги", 0, ItemInventory.ItemRarity.Common, false));
            Items.Add("beer", new Drink("🍺 Пиво", 1, 1000, ItemInventory.ItemRarity.Uncommon));
            Items.Add("wine", new Drink("🍷 Вино [МОЛДАВСКОЕ]", 3, 1500, ItemInventory.ItemRarity.Rare));
            Items.Add("winef", new Drink("🍷 Вино [ФРАНЦУЗСКОЕ]", 2, 1500, ItemInventory.ItemRarity.Rare));
            Items.Add("wineg", new Drink("🍷 Вино [ГРУЗИНСКОЕ]", 4, 2980, ItemInventory.ItemRarity.Rare));
            Items.Add("wrench", new Wrenchable("🔧 Гаечный ключ", 4, 30000, ItemInventory.ItemRarity.Rare));
            Items.Add("wrenchadv", new Wrenchable("🛠 Гаечный ключ и молоток", 8, 50000, ItemInventory.ItemRarity.Epic));
            Items.Add("hammer", new Wrenchable("🔨 Молоток", 5, 35000, ItemInventory.ItemRarity.Rare));
            Items.Add("speaker", new BasicItem("🔊 Мониторные колонки", 3200));
            Items.Add("dj", new BasicItem("🎛 PIONEER DJ", 3200));
            Items.Add("midikey", new BasicItem("🎹 Native Instruments Komplete Kontrol S88", 6000, ItemInventory.ItemRarity.Rare));
            Items.Add("wall", new BasicItem("🧱 Укрепление", 150000, ItemInventory.ItemRarity.Legendary));
            Items.Add("pistol", new Bomb("🔫 Пистолет", 5500, 50000));
            Items.Add("bomb", new Bomb("💣 Бомба", 9500, 90000, ItemInventory.ItemRarity.Unbeliveable));
            Items.Add("worm", new BasicItem("🐍 Червь", 500));
            Items.Add("fish", new BasicItem("🐟 Рыба", 1000, ItemInventory.ItemRarity.Uncommon));
            Items.Add("tfish", new BasicItem("🐠 Тропическая рыба", 15700, ItemInventory.ItemRarity.Rare));
            Items.Add("weirdfishes", new BasicItem("🍥 СТРАННАЯ РЫБА", 100000, ItemInventory.ItemRarity.Unbeliveable));
            Items.Add("ffish", new BasicItem("🐡 Рыба-фугу", 3700, ItemInventory.ItemRarity.Rare));
            Items.Add("veriplace", new BasicItem("🎏 Верхоплавки", 2700));
            Items.Add("pike", new BasicItem("🦈 Щука", 10000, ItemInventory.ItemRarity.Uncommon));
            Items.Add("som", new BasicItem("🐬 Сом", 12000, ItemInventory.ItemRarity.Rare));
            Items.Add("line", new BasicItem("🪢 Леска", 1000));
            Items.Add("rod", new FishingRod("🎣 Удочка", 20000, ItemInventory.ItemRarity.Uncommon));

            BotCtx = botCtx;

            foreach (var item in Items)
            {
                if (BotCtx.Connection.Table<SQL.Item>().All(x => x.Name != item.Key))
                {
                    BotCtx.Connection.Insert(new SQL.Item { ShopID = ShopID, Name = item.Key });
                    Log.Verbose("Inserted: {0}", item.Value.Name);
                }
            }

            Log.Information("Shop loading is done!");
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

                Math.Clamp(Items[itemname].Price, 100, Int32.MaxValue);

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