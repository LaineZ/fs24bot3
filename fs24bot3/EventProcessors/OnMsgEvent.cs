using NetIRC;
using NetIRC.Messages;
using Serilog;
using System;

namespace fs24bot3.EventProcessors
{
    public class OnMsgEvent
    {
        private readonly IRCMessageEventArgs<PrivMsgMessage> Event;
        private readonly Client Client;
        private readonly SQLite.SQLiteConnection Connection;
        private User User;
        private readonly Random Rand = new Random();

        public OnMsgEvent(Client client, IRCMessageEventArgs<PrivMsgMessage> ev, SQLite.SQLiteConnection connect)
        {
            Event = ev;
            Client = client;
            Connection = connect;
            User = new User(Event.IRCMessage.From, Connection);
        }

        public void LevelInscrease()
        {
            User.CreateAccountIfNotExist();
            User.SetLastMessage();
            bool newLevel = User.IncreaseXp(Event.IRCMessage.Message.Length * new Random().Next(1, 3) + 1);
            if (newLevel)
            {
                var random = new Random();
                int index = random.Next(Shop.ShopItems.Count);
                User.AddItemToInv(Shop.ShopItems[index].Slug, 1);
                Client.SendAsync(new PrivMsgMessage(Event.IRCMessage.To, Event.IRCMessage.From + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name));
            }
        }

        public void DestroyWallRandomly()
        {
            if (Rand.Next(0, 10) == 1 && User.RemItemFromInv("wall", 1))
            {
                Log.Information("Breaking wall for {0}", Event.IRCMessage.From);
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