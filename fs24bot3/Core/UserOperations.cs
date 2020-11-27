using fs24bot3.Models;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace fs24bot3
{
    class UserOperations
    {
        public string Username;
        public SQLiteConnection Connect;
        public CommandProcessor.CustomCommandContext Ctx;

        const int XP_MULTIPLER = 150;


        public UserOperations(string username, SQLiteConnection connection, CommandProcessor.CustomCommandContext ctx = null)
        {
            Username = username;
            Connect = connection;
            Ctx = ctx;
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
            var nick = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).First();
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(nick.LastMsg).ToLocalTime();
            return dtDateTime;
        }

        public void SetLevel(int level)
        {
            Connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, Username);
            Connect.Execute("UPDATE UserStats SET Need = Level * ? WHERE Nick = ?", XP_MULTIPLER, Username);
        }

        public void AddRemind(TimeSpan time, string title)
        {
            var remind = new SQL.Reminds()
            {
                Nick = Username,
                Message = title,
                RemindDate = (int)((DateTimeOffset)DateTime.Now.Add(time)).ToUnixTimeSeconds(),
            };

            Connect.Insert(remind);
        }

        public void AddItemToInv(string name, int count)
        {
            count = (int)Math.Floor((decimal)count);
            try
            {
                Connect.Execute("INSERT INTO Inventory VALUES(?, ?, ?)", Username, Shop.GetItem(name).Name, count);
            }
            catch (SQLiteException)
            {
                Connect.Execute("UPDATE Inventory SET Count = Count + ? WHERE Item = ? AND Nick = ?", count, Shop.GetItem(name).Name, Username);
            }
        }


        /// <summary>
        /// Removes item from inventory
        /// </summary>
        /// <param name="name">Item slug</param>
        /// <param name="count">Count</param>
        /// <returns>Success of removing</returns>
        public bool RemItemFromInv(string name, int count)
        {
            count = (int)Math.Floor((decimal)count);

            string itemname = Shop.GetItem(name).Name;
            var item = Connect.Table<SQL.Inventory>().SingleOrDefault(v => v.Nick.Equals(Username) && v.Item.Equals(itemname));

            if (item != null && item.ItemCount >= count && count > 0)
            {
                Connect.Execute("UPDATE Inventory SET Count = Count - ? WHERE Item = ? AND Nick = ?", count, itemname, Username);
                // clening up items with 0
                Connect.Execute("DELETE FROM Inventory WHERE Count = 0");
                if (Ctx != null)
                {
                    Ctx.SendMessage(Ctx.Channel, $" {Shop.GetItem(name).Name} -{count} За использование данной команды");
                }
                return true;
            }

            if (Ctx != null)
            {
                Ctx.SendMessage(Ctx.Channel, $"Недостаточно {Shop.GetItem(name).Name} x{count}");
            }
            return false;
        }


        public int CountItem(string itemname)
        {
            // full qualified item name
            var itemFullName = Shop.GetItem(itemname).Name;
            var item = Connect.Table<SQL.Inventory>().SingleOrDefault(v => v.Nick.Equals(Username) && v.Item.Equals(itemFullName));
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
                Log.Warning(Username + ": Cannot query inventory!");
                return new List<SQL.Inventory>();
            }
        }

        public SQL.UserStats GetUserInfo()
        {
            var query = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).First();
            if (query != null)
            {
                return query;
            }
            else
            {
                throw new Core.Exceptions.UserNotFoundException();
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
                    throw new Core.Exceptions.UserNotFoundException();
                }

                foreach (var nick in userNick)
                {
                    tags.Add(nick);
                }
                return tags;
            }
            else
            {
                throw new Core.Exceptions.UserNotFoundException();
            }
        }


        /// <summary>
        /// Gets user fishing rod
        /// </summary>
        /// <returns>SQL.UserFishingRods - if rod found, null if not found </returns>
        public SQL.UserFishingRods GetRod()
        {
            var query = Connect.Table<SQL.UserFishingRods>().Where(v => v.Username.Equals(Username)).FirstOrDefault();
            return query;
        }

        public FishingError.RodErrors AddRod(string rodname)
        {
            var query = Connect.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).ToList();

            if (query.Any())
            {
                var userod = GetRod();

                if (userod == null)
                {
                    //Log.Verbose("INSERTING rod {0}", rodname);
                    Connect.Insert(new SQL.UserFishingRods { Username = Username, RodName = rodname, RodDurabillity = query[0].RodDurabillity });
                    return FishingError.RodErrors.RodOk;
                }
                else
                {
                    Log.Warning("Rod aready exsist {0}", rodname);
                    return FishingError.RodErrors.RodAreadyExists;
                }
            }
            Log.Warning("Cannot insert rod! {0}", rodname);
            return FishingError.RodErrors.RodNotFound;
        }

        public (FishingError.RodErrors, SQL.FishingNests) SetNest(string nest)
        {
            var query = Connect.Table<SQL.FishingNests>().Where(v => v.Name.Equals(nest)).FirstOrDefault();
            if (query != null)
            {
                if (GetRod() != null)
                {
                    Connect.Execute("UPDATE UserFishingRods SET Nest = ? WHERE Username = ?", nest, Username);
                    return (FishingError.RodErrors.RodOk, query);
                }
                else
                {
                    return (FishingError.RodErrors.RodNotFound, null);
                }
            }
            return (FishingError.RodErrors.RodUnknownError, null);
        }

        public (FishingError.RodErrors, SQL.UserFishingRods) DelRod()
        {
            var userod = GetRod();

            if (userod == null)
            {
                Log.Warning("Rod not found for {0}", Username);
                return (FishingError.RodErrors.RodNotFound, null);
            }
            else
            {
                Log.Verbose("Rod removed for {0}", Username);
                Connect.Execute("DELETE FROM UserFishingRods WHERE Username = ?", Username);
                return (FishingError.RodErrors.RodOk, userod);
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
                var tagInfo = new Core.TagsUtils(name, Connect);

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
