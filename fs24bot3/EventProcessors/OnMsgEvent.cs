using fs24bot3.BotSystems;
using fs24bot3.Core;
using NetIRC;
using NetIRC.Messages;
using Serilog;
using System;
using System.Linq;

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

        public void LevelInscrease(Shop shop)
        {
            User.CreateAccountIfNotExist();
            User.SetLastMessage();
            bool newLevel = User.IncreaseXp(Message.Length * new Random().Next(1, 3) + 1);
            if (newLevel)
            {
                var random = new Random();
                int index = random.Next(shop.Items.Count);
                var item = shop.Items.ElementAt(index);
                User.AddItemToInv(shop, item.Key, 1);
                Client.SendAsync(new PrivMsgMessage(Target, User.Username + ": У вас новый уровень! Вы получили за это: " + item.Value.Name));
            }
        }

        public async void DestroyWallRandomly(Shop shop)
        {
            if (Rand.Next(0, 10) == 1 && await User.RemItemFromInv(shop, "wall", 1))
            {
                Log.Information("Breaking wall for {0}", User.Username);
            }
        }
    }
}