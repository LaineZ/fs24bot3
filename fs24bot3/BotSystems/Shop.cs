using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.ItemTraits;
using Serilog;

namespace fs24bot3.BotSystems
{
    public class Shop
    {
        public Dictionary<string, Models.ItemInventory.IItem> Items { get; private set; }
        private Random Rand = new Random();

        public int Sells { get; private set; }
        public int Buys { get; private set; }
        private Bot BotCtx { get; }
        public int MaxCap = 250000;
        public int PaydaysCount = 0;

        public string ShopID { get; }

        public Shop(Bot botCtx)
        {
            Items = new Dictionary<string, Models.ItemInventory.IItem>();
            ShopID = "shop";

            Items.Add("money", new Models.ItemInventory.BasicItem("💰 Деньги", 0, false));
            Items.Add("beer", new Drink("🍺 Пиво", 1, 100));
            Items.Add("wine", new Drink("🍷 Вино [МОЛДАВСКОЕ]", 3, 150));
            Items.Add("winef", new Drink("🍷 Вино [ФРАНЦУЗСКОЕ]", 2, 150));
            Items.Add("wineg", new Drink("🍷 Вино [ГРУЗИНСКОЕ]", 4, 298));
            Items.Add("wrench", new Wrenchable("🔧 Гаечный ключ", 4, 3000));
            Items.Add("wrenchadv", new Wrenchable("🛠 Гаечный ключ и молоток", 8, 5000));
            Items.Add("hammer", new Wrenchable("🔨 Молоток", 5, 3500));
            Items.Add("speaker", new Models.ItemInventory.BasicItem("🔊 Мониторные колонки", 320));
            Items.Add("dj", new Models.ItemInventory.BasicItem("🎛 PIONEER DJ", 320));
            Items.Add("midikey", new Models.ItemInventory.BasicItem("🎹 Native Instruments Komplete Kontrol S88", 600));
            Items.Add("wall", new Models.ItemInventory.BasicItem("🧱 Укрепление", 15000));
            Items.Add("pistol", new Bomb("🔫 Пистолет", 5500));
            Items.Add("bomb", new Bomb("💣 Бомба", 9500));
            Items.Add("worm", new Models.ItemInventory.BasicItem("🐍 Червь", 50));
            Items.Add("fish", new Models.ItemInventory.BasicItem("🐟 Рыба", 100));
            Items.Add("tfish", new Models.ItemInventory.BasicItem("🐠 Тропическая рыба", 1570));
            Items.Add("weirdfishes", new Models.ItemInventory.BasicItem("🍥 СТРАННАЯ РЫБА", 10000));
            Items.Add("ffish", new Models.ItemInventory.BasicItem("🐡 Рыба-фугу", 370));
            Items.Add("veriplace", new Models.ItemInventory.BasicItem("🎏 Верхоплавки", 270));
            Items.Add("pike", new Models.ItemInventory.BasicItem("🦈 Щука", 1000));
            Items.Add("som", new Models.ItemInventory.BasicItem("🐬 Сом", 1200));
            Items.Add("line", new Models.ItemInventory.BasicItem("🪢 Леска", 100));
            Items.Add("rod", new FishingRod("🎣 Удочка", 2000));

            BotCtx = botCtx;

            foreach (var item in Items)
            {
                if (!BotCtx.Connection.Table<Models.SQL.Item>().Where(x => x.Name == item.Key).Any())
                {
                    BotCtx.Connection.Insert(new Models.SQL.Item { ShopID = ShopID, Name = item.Key });
                    Log.Verbose("Inserted: {0}", item.Value.Name);
                }
            }

            Log.Information("Shop loading is done!");
        }

        public void UpdateShop()
        {
            foreach (var BasicItem in Items)
            {
                int check = Rand.Next(0, 10);
                if (check == 5)
                {
                    Log.Verbose("Descreaseing price for {0}", BasicItem.Value.Name);
                    BasicItem.Value.Price -= Rand.Next(1, 3);
                }
            }
        }
        
        public async Task<(bool, int)> Sell(User user, string itemname, int count = 1)
        {
            if (await user.RemItemFromInv(this, itemname, count) && Items[itemname].Sellable)
            {
                // tin
                int sellprice = (int)Math.Floor((decimal)(Items[itemname].Price * count) / 2);
                user.AddItemToInv(this, "money", sellprice);
                Sells++;
                return (true, sellprice);
            }
            else
            {
                return (false, 0);
            }
        }

        public async Task<(bool, int)> Buy(User user, string itemname, int count = 1)
        {
            int buyprice = Items[itemname].Price * count;
            bool sucessfully = await user.RemItemFromInv(this, "money", buyprice);

            if (sucessfully)
            {
                user.AddItemToInv(this, itemname, count);
                Items[itemname].Price += 5;
                Buys++;
            }
            return (sucessfully, buyprice);
        }
    }
}