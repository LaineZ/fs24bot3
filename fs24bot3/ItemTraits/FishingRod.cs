using System;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.ItemTraits
{
    public class FishingRod : Models.ItemInventory.IItem
    {
        public string Name { get; }
        public int Price { get; set; }
        public bool Sellable { get; set; }

        public FishingRod(string name, int price = 0)
        {
            Name = name;
            Price = price;
            Sellable = true;
        }

        public async Task<bool> OnUseMyself(Bot botCtx, string channel, Core.User user)
        {
            var rand = new Random();
            var nest = botCtx.Connection.Table<SQL.FishingNests>().
                        Where(v => v.Name.Equals(user.GetFishNest())).
                        FirstOrDefault();

            if (nest == null)
            {
                await botCtx.SendMessage(channel, $"{Models.IrcColors.Gray}Место рыбалки не установлено, используйте @nest");
                return false;
            }

            if (rand.Next(0, 2) == 1)
            {
                // TODO: Refactor
                string[] fish = new string[15];

                if (nest.Level == 1)
                {
                    fish = new string[] { "fish", "veriplace", "ffish" };
                }
                if (nest.Level == 2)
                {
                    fish = new string[] { "fish", "veriplace", "ffish", "pike", "som" };
                }
                if (nest.Level == 3)
                {
                    fish = new string[] { "fish", "veriplace", "ffish", "pike", "som", "weirdfishes", "worm", "wrench", "wrenchadv" };
                }

                string catched = fish[rand.Next(0, fish.Length)];
                user.AddItemToInv(botCtx.Shop, catched, 1);
                await botCtx.SendMessage(channel, $"Вы поймали {botCtx.Shop.Items[catched].Name}!");
            }
            else
            {
                await botCtx.SendMessage(channel, $"{IrcColors.Gray}Рыба сорвалась!");
            }


            return true;
        }
    }
}