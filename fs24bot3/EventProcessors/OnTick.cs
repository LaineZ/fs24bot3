using fs24bot3.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.EventProcessors
{
    public class OnTick
    {
        private MultiUser MultiUser;
        private User User;
        private Random Rand = new Random();

        public OnTick(string username, SQLite.SQLiteConnection connection)
        {
            MultiUser = new MultiUser(connection);
            User = new User(username, connection);
        }

        public async void UpdateUserPaydays()
        {
            DateTime start = DateTime.Now;
            int checkPayday = Rand.Next(0, 10);

            if (checkPayday == 8 && MultiUser.GetItemAvg() < Shop.MaxCap)
            {
                var subst = DateTime.Now.Subtract(User.GetLastMessage()).TotalHours;

                if (subst > 10)
                {
                    Log.Information("Tax fine for user: {0}", User.Username);
                    await User.RemItemFromInv("money", User.GetUserInfo().Level * Rand.Next(1, 2));
                }
                else
                {
                    User.AddItemToInv("money", User.GetUserInfo().Level);
                }
            }

            Shop.PaydaysCount++;
            DateTime elapsed = DateTime.Now;
            Shop.TickSpeed = elapsed.Subtract(start);
        }
    }
}
