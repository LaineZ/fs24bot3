﻿using fs24bot3.Systems;
using fs24bot3.Core;
using Serilog;
using System;

namespace fs24bot3.EventProcessors;
public class OnTick
{
    private MultiUser MultiUser;
    private User User;
    private Random Rand = new Random();

    public OnTick(string username, in SQLite.SQLiteConnection connection)
    {
        MultiUser = new MultiUser(in connection);
        User = new User(username, in connection);
    }

    public async void UpdateUserPaydays(Shop shop)
    {
        int checkPayday = Rand.Next(0, 20);
        if (checkPayday == 8 && MultiUser.GetItemAvg() < shop.MaxCap)
        {
            var subst = DateTime.Now.Subtract(User.GetLastMessage()).TotalHours;

            if (subst > 10)
            {
                Log.Information("Tax fine for user: {0}", User.Username);
                await User.RemItemFromInv(shop, "money", User.GetUserInfo().Level * Rand.Next(1, 2));
            }
            else
            {
                User.AddItemToInv(shop, "money", User.GetUserInfo().Level / Rand.Next(1, 4));
            }

            shop.PaydaysCount++;
        }
    }

    public void RemoveLevelOneAccs()
    {
        var subst = DateTime.Now.Subtract(User.GetLastMessage()).TotalDays;

        if (subst >= 30 && User.GetUserInfo().Level == 1 && User.CountItem("money") < 2000)
        {
            Log.Warning("Removing user account: {0}", User.Username);
            User.RemoveUserAccount();
        }
    }
}
