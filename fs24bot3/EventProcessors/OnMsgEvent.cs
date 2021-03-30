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
        private User User;
        private string Target;
        private string Message;
        private readonly Random Rand = new Random();

        public OnMsgEvent(Client client, string nick, string target, string message, SQLite.SQLiteConnection connect)
        {
            Client = client;
            Connection = connect;
            Message = message;
            Target = target;
            User = new User(nick, Connection);
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

        public void DestroyWallRandomly()
        {
            if (Rand.Next(0, 10) == 1 && User.RemItemFromInv("wall", 1))
            {
                Log.Information("Breaking wall for {0}", User.Username);
            }
        }

        public void GiveWaterFromPumps()
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
                            User.RemItemFromInv("pump", 1);
                        }
                        break;
                    }
                }
            }
        }
    }
}