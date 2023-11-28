using fs24bot3.Systems;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fs24bot3.ItemTraits;
using static fs24bot3.Models.Exceptions;

namespace fs24bot3.Core;
public class User
{
    public string Username { get; }

    private SQLiteConnection Connect;
    private CommandProcessor.CustomCommandContext Ctx;

    const int XP_MULTIPLER = 150;

    public User(string username, in SQLiteConnection connection, CommandProcessor.CustomCommandContext ctx = null)
    {
        Username = username;
        Connect = connection;
        Ctx = ctx;
    }


    /// <summary>
    /// Disables Chat-context related messages. (just sets to null Ctx variable, lol)
    /// </summary>
    public void EnableSilentMode()
    {
        Ctx = null;
    }

    public void SetContext(CommandProcessor.CustomCommandContext ctx)
    {
        Ctx = ctx;
    }

    public TimeZoneInfo GetTimeZone()
    {
        if (GetUserInfo().Timezone == null)
        {
            return TimeZoneInfo.Local;
        }
        else
        {
            return TimeZoneInfo.FindSystemTimeZoneById(GetUserInfo().Timezone);
        }
    }

    public void CreateAccountIfNotExist()
    {
        int query = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).Count();

        if (query <= 0)
        {
            Log.Warning("User {0} not found in database", Username);

            var user = new SQL.UserStats()
            {
                Nick = Username,
                Admin = 0,
                Level = 1,
                Xp = 0,
                Need = 1000,
                LastMsg = (int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(),
            };

            Connect.Insert(user);
        }
    }

    public void AddWarning(string message, Bot botContext)
    {
        var warning = new SQL.Warnings()
        {
            Nick = Username,
            Message = message,
        };
        botContext.AcknownUsers.Remove(Username);
        Connect.Insert(warning);
    }

    public List<SQL.Warnings> GetWarnings()
    {
        var warns = Connect.Table<SQL.Warnings>().Where(v => v.Nick.Equals(Username)).ToList();
        return warns;
    }

    public void DeleteWarnings()
    {
        Connect.Execute("DELETE FROM Warnings WHERE Nick = ?", Username);
    }

    public void SetTimeZone(string timezone)
    {
        Connect.Execute("UPDATE UserStats SET TimeZone = ? WHERE Nick = ?", timezone, Username);
    }

    public void RemoveUserAccount()
    {
        Connect.Execute("DELETE FROM Reminds WHERE Nick = ?", Username);
        Connect.Execute("DELETE FROM Warnings WHERE Nick = ?", Username);
        Connect.Execute("DELETE FROM UserStats WHERE Nick = ?", Username);
        Connect.Execute("DELETE FROM Inventory WHERE Nick = ?", Username);
        Connect.Execute("VACUUM");
    }

    /// <summary>
    /// Increases user XP
    /// </summary>
    /// <param name="count">Returns true if user gets new level</param>
    /// <returns></returns>
    public bool IncreaseXp(int count)
    {
        var nick = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).First();
        Connect.Execute("UPDATE UserStats SET Xp = Xp + ? WHERE Nick = ?", count, nick.Nick);

        Log.Verbose("{4}: Setting level: {0} {1} Current data: {2}/{3}", count, nick.Level, nick.Xp, nick.Need, nick.Nick);

        if (nick.Xp >= nick.Need)
        {
            Connect.Execute("UPDATE UserStats SET Level = Level + 1 WHERE Nick = ?", nick.Nick);
            Connect.Execute("UPDATE UserStats SET Xp = 0 WHERE Nick = ?", nick.Nick);
            Connect.Execute("UPDATE UserStats SET Need = Level * ? WHERE Nick = ?", XP_MULTIPLER, nick.Nick);
            return true;
        }

        return false;
    }

    public void SetLastMessage()
    {
        Connect.Execute("UPDATE UserStats SET LastMsg = ? WHERE Nick = ?", (int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(), Username);
    }

    public DateTime GetLastMessage()
    {
        var nick = GetUserInfo();
        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(nick == null ? 0 : nick.LastMsg).ToLocalTime();
        return dtDateTime;
    }

    public bool UserIsIgnored()
    {
        Log.Verbose("Ignored: {0}", Connect.Table<SQL.Ignore>().Any(v => v.Username == Username));
        return Connect.Table<SQL.Ignore>().Any(v => v.Username.Equals(Username));
    }

    public void SetLevel(int level)
    {
        Connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, Username);
        Connect.Execute("UPDATE UserStats SET Need = Level * ? WHERE Nick = ?", XP_MULTIPLER, Username);
    }

    public int GetFishLevel()
    {
        var q = Connect.Table<SQL.Fishing>().FirstOrDefault(v => v.Nick == Username);
        if (q != null) { return q.Level; }
        return 1;
    }


    public string GetFishNest()
    {
        var q = Connect.Table<SQL.Fishing>().FirstOrDefault(v => v.Nick == Username);
        if (q != null)
        {
            return q.NestName;
        }
        else
        {
            return null;
        }
    }

    public Dictionary<string, IItem> AddRandomRarityItem(Shop shop, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Uncommon, int mincount = 1, int maxcount = 1, int iterations = 1)
    {
        var rng = new Random();
        var dict = new Dictionary<string, IItem>();

        for (int i = 0; i < iterations; i++)
        {
            var item = shop.Items.Where(x => x.Value.Rarity >= rarity).Random();
            dict.Add(item.Key, item.Value);
            AddItemToInv(shop, item.Key, rng.Next(mincount, maxcount));
        }

        return dict;
    }

    public void IncreaseFishLevel()
    {
        Connect.Execute("UPDATE Fishing SET Level = Level + 1 WHERE Nick = ?", Username);
    }

    public SQL.FishingNests SetNest(string nest)
    {
        var query = Connect.Table<SQL.FishingNests>().FirstOrDefault(v => v.Name.Equals(nest));
        if (query != null)
        {
            Connect.InsertOrReplace(new SQL.Fishing
            {
                Level = GetFishLevel(),
                NestName = nest,
                Nick = Username,
            });
        }

        return query;
    }

    public void AddRemind(TimeSpan time, string title, string channel)
    {
        var remind = new SQL.Reminds()
        {
            Nick = Username,
            Message = title,
            RemindDate = ((DateTimeOffset)DateTime.Now.Add(time)).ToUnixTimeSeconds(),
            Channel = channel,
        };

        Connect.Insert(remind);
    }


    public void AddRemindAbs(DateTime time, string title)
    {
        var remind = new SQL.Reminds()
        {
            Nick = Username,
            Message = title,
            RemindDate = (uint)((DateTimeOffset)time).ToUnixTimeSeconds(),
        };

        Connect.Insert(remind);
    }

    public List<SQL.Reminds> GetReminds()
    {
        return Connect.Table<SQL.Reminds>().Where(x => x.Nick == Username).OrderBy(x => x.RemindDate).ToList();
    }

    public bool DeleteRemind(uint id)
    {
        var affected = Connect.Execute("DELETE FROM Reminds WHERE Nick = ? AND Id = ?", Username, id);
        return affected > 0;
    }

    public void AddItemToInv(Shop shop, string name, int count)
    {
        if (!shop.Items.ContainsKey(name))
        {
            throw new ItemNotFoundException();
        }
        count = (int)Math.Floor((decimal)count);
        try
        {
            Connect.Execute("INSERT INTO Inventory VALUES(?, ?, ?)", Username, name, count);
        }
        catch (SQLiteException)
        {
            Connect.Execute("UPDATE Inventory SET Count = Count + ? WHERE Item = ? AND Nick = ?", count, name, Username);
        }
    }

    public void AddTag(in SQL.Tag tag)
    {
        Connect.Execute("INSERT INTO Tags VALUES(?, ?)", Username, tag.Name);
    }

    public void RemoveTag(in SQL.Tag tag)
    {
        Connect.Execute("DELETE FROM Tags WHERE TagName = ? AND Nick = ?", tag.Name, Username);
    }
    /// <summary>
    /// Removes item from inventory
    /// </summary>
    /// <param name="name">Item key</param>
    /// <param name="count">Count</param>
    /// <returns>Success of removing</returns>
    public async Task<bool> RemItemFromInv(Shop shop, string name, int count)
    {
        if (!shop.Items.ContainsKey(name))
        {
            throw new ItemNotFoundException();
        }

        count = (int)Math.Floor((decimal)count);
        var item = Connect.Table<SQL.Inventory>().SingleOrDefault(v => v.Nick.Equals(Username) && v.Item.Equals(name));

        if (item != null && item.ItemCount >= count && count > 0)
        {
            Connect.Execute("UPDATE Inventory SET Count = Count - ? WHERE Item = ? AND Nick = ?", count, name, Username);
            // clening up items with 0
            Connect.Execute("DELETE FROM Inventory WHERE Count = 0");
            if (Ctx != null)
            {
                await Ctx.SendMessage(Ctx.Channel, $" {shop.Items[name].Name} -{count} За использование данной команды");
            }
            return true;
        }

        if (Ctx != null)
        {
            await Ctx.SendMessage(Ctx.Channel, $"Недостаточно {shop.Items[name].Name} x{count}");
        }
        return false;
    }


    public int CountItem(string itemname)
    {
        var item = Connect.Table<SQL.Inventory>()
            .SingleOrDefault(v => v.Nick.Equals(Username) && v.Item.Equals(itemname));
        return item?.ItemCount ?? 0;
    }

    public void SetCity(string city)
    {
        Connect.Execute("UPDATE UserStats SET City = ? WHERE Nick = ?", city, Username);
    }

    public string GetCity(string def)
    {
        try
        {
            var city = GetUserInfo().City;

            if (def != "" && city != def)
            {
                SetCity(def);
                return def;
            }

            return city ?? def;
        }
        catch (UserNotFoundException)
        {
            return def;
        }
    }

    public List<SQL.Inventory> GetInventory()
    {
        var query = Connect.Table<SQL.Inventory>().Where(v => v.Nick.Equals(Username)).ToList();
        if (query.Any()) { return query; }
        else { return new List<SQL.Inventory>(); }
    }

    public List<SQL.Tags> GetTags()
    {
        var query = Connect.Table<SQL.Tags>().Where(v => v.Nick.Equals(Username)).ToList();
        if (query.Any()) { return query; }
        else { return new List<SQL.Tags>(); }
    }

    public SQL.UserStats GetUserInfo()
    {
        var query = Connect.Query<SQL.UserStats>("SELECT * FROM UserStats WHERE Nick = ? LIMIT 1", Username)
            .FirstOrDefault();
        if (query != null)
        {
            return query;
        }
        else
        {
            throw new UserNotFoundException();
        }
    }
}
