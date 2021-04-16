using fs24bot3.Core;
using NetIRC;
using NetIRC.Messages;
using Serilog;
using System;

namespace fs24bot3.EventProcessors
{
    public class OnMsgEvent
    {
        private readonly Client Client;
        private readonly SQLite.SQLiteConnection Connection;
        private readonly Core.User User;
        private readonly string Target;
        private readonly string Message;
        private readonly Random Rand = new Random();

        public OnMsgEvent(Client client, string nick, string target, string message, SQLite.SQLiteConnection connect)
        {
            Client = client;
            Connection = connect;
            Message = message;
            Target = target;
            User = new Core.User(nick, Connection);
        }

        public void LevelInscrease()
        {
            User.CreateAccountIfNotExist();
            User.SetLastMessage();
            bool newLevel = User.IncreaseXp(Message.Length * new Random().Next(1, 3) + 1);
            if (newLevel)
            {
                var random = new Random();
                int index = random.Next(Shop.ShopItems.Count);
                User.AddItemToInv(Shop.ShopItems[index].Slug, 1);
                Client.SendAsync(new PrivMsgMessage(Target, User.Username + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name));
            }
        }

        public async void DestroyWallRandomly()
        {
            if (Rand.Next(0, 10) == 1 && await User.RemItemFromInv("wall", 1))
            {
                Log.Information("Breaking wall for {0}", User.Username);
            }
        }

        public async void GiveWaterFromPumps()
        {
            if (User.GetInventory() != null)
            {
                foreach (var item in User.GetInventory())
                {
                    // if user have a PUMP!
                    if (item.Item == "pump")
                    {
                        Log.Information("Giving water!");
                        User.AddItemToInv("water", Rand.Next(1, 2));

                        if (Rand.Next(1, 3) == 3)
                        {
                            await User.RemItemFromInv("pump", 1);
                        }
                        break;
                    }
                }
            }
        }
    }
}