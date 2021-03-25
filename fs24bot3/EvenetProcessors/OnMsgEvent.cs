using NetIRC;
using NetIRC.Messages;
using Serilog;
using System;

namespace fs24bot3.EventProcessors
{
    class OnMsgEvent
    {
        private IRCMessageEventArgs<PrivMsgMessage> Event;
        private Client Client;
        private SQLite.SQLiteConnection Connection;

        private Random Rand = new Random();
        public OnMsgEvent(Client client, IRCMessageEventArgs<PrivMsgMessage> ev, SQLite.SQLiteConnection connect)
        {
            Event = ev;
            Client = client;
            Connection = connect;
        }

        public void LevelInscrease()
        {
            UserOperations usr = new UserOperations(Event.IRCMessage.From, Connection);
            usr.CreateAccountIfNotExist();
            usr.SetLastMessage();
            bool newLevel = usr.IncreaseXp(Event.IRCMessage.Message.Length * new Random().Next(1, 3) + 1);
            if (newLevel)
            {
                var random = new Random();
                int index = random.Next(Shop.ShopItems.Count);
                usr.AddItemToInv(Shop.ShopItems[index].Slug, 1);
                Client.SendAsync(new PrivMsgMessage(Event.IRCMessage.To, Event.IRCMessage.From + ": У вас новый уровень! Вы получили за это: " + Shop.ShopItems[index].Name));
            }
        }

        public void DestroyWallRandomly()
        {
            if (Rand.Next(0, 10) == 1 && user.RemItemFromInv("wall", 1))
            {
                Log.Information("Breaking wall for {0}", users.Nick);
            }
        }
    }
}