using fs24bot3.BotSystems;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
    public class User
    {
        public string Username { get; }

        private SQLiteConnection Connect;
        private CommandProcessor.CustomCommandContext Ctx;

        const int XP_MULTIPLER = 150;


        public User(string username, SQLiteConnection connection, CommandProcessor.CustomCommandContext ctx = null)
        {
            Username = username;
            Connect = connection;
            Ctx = ctx;
        }


        public void EnableSilentMode()
        {
            Ctx = null;
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
                    AdminPassword = "changeme",
                    Level = 1,
                    Xp = 0,
                    Need = 300,
                    LastMsg = (int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(),
                };

                Connect.Insert(user);
            }
        }

        public string GetUserPrefix()
        {
            var userInfo = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).FirstOrDefault();
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.Prefix) && !string.IsNullOrWhiteSpace(userInfo.Prefix))
            {
                return userInfo.Prefix;
            }
            return ConfigurationProvider.Config.Prefix;
        }

        public void AddWarning(string message)
        {
            Connect.Execute("UPDATE UserStats SET WarningAcknown = 0 WHERE Nick = ?", Username);

            var warning = new SQL.Warnings()
            {
                Nick = Username,
                Message = message,
            };

            Connect.Insert(warning);
        }

        public List<SQL.Warnings> GetWarnings()
        {
            SetAcknown();
            Connect.Execute("DELETE FROM Warnings WHERE Nick = ?", Username);
            return Connect.Table<SQL.Warnings>().Where(v => v.Nick.Equals(Username)).ToList();
        }

        public void SetAcknown()
        {
            Connect.Execute("UPDATE UserStats SET WarningAcknown = 1 WHERE Nick = ?", Username);
        }

        public bool GetAcknown()
        {
            return GetUserInfo().WarningAcknown == 0 && Connect.Table<SQL.Warnings>().Where(v => v.Nick.Equals(Username)).Any();
        }

        public void SetUserPrefix(string prefix = "#")
        {
            Connect.Execute("UPDATE UserStats SET Prefix = ? WHERE Nick = ?", prefix, Username);
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
            Connect.Execute("VACCUM");
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
            var nick = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).FirstOrDefault();
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(nick == null ? 0 : nick.LastMsg).ToLocalTime();
            return dtDateTime;
        }

        public bool UserIsIgnored()
        {
            return Connect.Table<SQL.Ignore>().Where(v => v.Username.Equals(Username)).Any();
        }

        public void SetLevel(int level)
        {
            Connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, Username);
            Connect.Execute("UPDATE UserStats SET Need = Level * ? WHERE Nick = ?", XP_MULTIPLER, Username);
        }

        public int GetFishLevel()
        {
            var q = Connect.Table<SQL.Fishing>().Where(v => v.Nick == Username).FirstOrDefault();
            if (q != null) { return q.Level; }
            return 1;
        }


        public string GetFishNest()
        {
            var q = Connect.Table<SQL.Fishing>().Where(v => v.Nick == Username).FirstOrDefault();
            if (q != null)
            {
                return q.NestName;
            }
            else
            {
                return null;
            }
        }

        public Dictionary<string, ItemInventory.IItem> AddRandomRarityItem(Shop shop, ItemInventory.ItemRarity rarity = ItemInventory.ItemRarity.Uncommon, int mincount = 1, int maxcount = 1, int iterations = 1)
        {
            var rng = new Random();
            var dict = new Dictionary<string, ItemInventory.IItem>();

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
            var query = Connect.Table<SQL.FishingNests>().Where(v => v.Name.Equals(nest)).FirstOrDefault();
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

        public void AddRemind(TimeSpan time, string title)
        {
            var remind = new SQL.Reminds()
            {
                Nick = Username,
                Message = title,
                RemindDate = (uint)((DateTimeOffset)DateTime.Now.Add(time)).ToUnixTimeSeconds(),
            };

            Connect.Insert(remind);
        }

        public List<SQL.Reminds> GetReminds()
        {
            return Connect.Table<SQL.Reminds>().Where(x => x.Nick == Username).ToList();
        }

        public void AddItemToInv(Shop shop, string name, int count)
        {
            if (!shop.Items.ContainsKey(name))
            {
                throw new Exceptions.ItemNotFoundException();
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
                throw new Exceptions.ItemNotFoundException();
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
            var item = Connect.Table<SQL.Inventory>().SingleOrDefault(v => v.Nick.Equals(Username) && v.Item.Equals(itemname));
            return item == null ? 0 : item.ItemCount;
        }

        public List<SQL.Inventory> GetInventory()
        {
            var query = Connect.Table<SQL.Inventory>().Where(v => v.Nick.Equals(Username)).ToList();
            if (query.Any())
            {
                return query;
            }
            else
            {
                //Log.Warning(Username + ": Cannot query inventory!");
                return new List<SQL.Inventory>();
            }
        }

        public SQL.UserStats GetUserInfo()
        {
            var query = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).FirstOrDefault();
            if (query != null)
            {
                return query;
            }
            else
            {
                throw new Exceptions.UserNotFoundException();
            }
        }

        public List<SQL.Tag> GetUserTags()
        {
            List<SQL.Tag> tags = new List<SQL.Tag>();
            var query = Connect.Table<SQL.Tags>().Where(v => v.Username.Equals(Username)).ToList();
            if (query.Any())
            {
                var userNick = JsonConvert.DeserializeObject<List<SQL.Tag>>(query[0].JsonTag);

                if (userNick == null)
                {
                    throw new Exceptions.UserNotFoundException();
                }

                foreach (var nick in userNick)
                {
                    tags.Add(nick);
                }
                return tags;
            }
            else
            {
                throw new Exceptions.UserNotFoundException();
            }
        }

        public bool AddTag(string name, int count)
        {
            var userinfo = GetUserInfo();

            if (userinfo == null)
            {
                Log.Error("User {0} not found!", Username);
                return false;
            }

            var query = Connect.Table<SQL.Tags>().Where(v => v.Username.Equals(Username)).ToList();

            if (query.Any())
            {
                var userTags = JsonConvert.DeserializeObject<List<SQL.Tag>>(query[0].JsonTag);

                bool append = false;

                foreach (var items in userTags)
                {
                    if (items.TagName == name)
                    {
                        items.TagCount += count;
                        //Log.Verbose("Appending {0} count: {1}", items.TagName, count);
                        append = true;
                        break;
                    }
                }
                if (!append)
                {
                    //Log.Verbose("creaing {0} count: {1}", name, count);
                    userTags.Add(new SQL.Tag() { TagName = name, TagCount = count });
                }

                Connect.Execute("UPDATE Tags SET JsonTag = ? WHERE Username = ?", JsonConvert.SerializeObject(userTags).ToString(), Username);
                return true;
            }
            else
            {

                Log.Verbose("Trying to find tag with name: {0}", name);
                var tagInfo = new TagsUtils(name, Connect);

                if (tagInfo.GetTagByName() == null)
                {
                    return false;
                }

                Log.Verbose("creaing {0} count: {1}", name, count);

                var tagList = new List<SQL.Tag>
                {
                    tagInfo.GetTagByName()
                };

                var user = new SQL.Tags()
                {
                    Username = Username,
                    JsonTag = JsonConvert.SerializeObject(tagList).ToString()
                };

                Connect.Insert(user);
                return true;
            }
        }
    }
}
