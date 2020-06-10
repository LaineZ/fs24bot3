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


        public bool IncreaseXp(int count)
        {
            var start = Stopwatch.StartNew();
            var query = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username));

            start.Stop();
            Log.Information("{0} ms", start.ElapsedMilliseconds);

            foreach (var nick in query)
            {
                Connect.Execute("UPDATE UserStats SET Xp = Xp + ? WHERE Nick = ?", count, nick.Nick);

                Log.Verbose("{4}: Setting level: {0} {1} Current data: {2}/{3}", count, nick.Level, nick.Xp, nick.Need, nick.Nick);

                if (nick.Xp >= nick.Need)
                {
                    Connect.Execute("UPDATE UserStats SET Level = Level + 1 WHERE Nick = ?", nick.Nick);
                    Connect.Execute("UPDATE UserStats SET Xp = 0 WHERE Nick = ?", nick.Nick);
                    Connect.Execute("UPDATE UserStats SET Need = Level * ? WHERE Nick = ?", XP_MULTIPLER, nick.Nick);
                    return true;
                }
            }
            return false;
        }

        public void SetLevel(int level)
        {
            Connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, Username);
            Connect.Execute("UPDATE UserStats SET Need = Level * ? WHERE Nick = ?", XP_MULTIPLER, Username);
        }

        public bool AddItemToInv(string name, int count)
        {
            count = (int)Math.Floor((decimal)count);

            try
            {
                Connect.Execute("INSERT INTO Inventory VALUES(?, ?, ?)", Username, Shop.GetItem(name).Name, count);
                //Log.Verbose("Inserting items");
                return true;
            }
            catch (SQLiteException)
            {
                Connect.Execute("UPDATE Inventory SET Count = Count + ? WHERE Item = ? AND Nick = ?", count, Shop.GetItem(name).Name, Username);
                //Log.Verbose("Updating items {0}", name);
                return true;
            }
        }

        public bool RemItemFromInv(string name, int count)
        {
            count = (int)Math.Floor((decimal)count);

            string itemname = Shop.GetItem(name).Name;
            var query = Connect.Table<SQL.Inventory>().Where(v => v.Nick.Equals(Username) && v.Item.Equals(itemname)).ToList();
            
            if (query.Count > 0)
            {
                foreach (var item in query)
                {
                    if (item.Item == Shop.GetItem(name).Name && item.ItemCount >= count)
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
                }
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

            var query = Connect.Table<SQL.Inventory>().Where(v => v.Nick.Equals(Username) && v.Item.Equals(itemFullName)).ToList();
            if (query.Count > 0)
            {
                foreach (var item in query)
                {
                    if (item.Item == itemFullName)
                    {
                        Log.Verbose("Counting item {0} x{1}", itemFullName, item.ItemCount);
                        return item.ItemCount;
                    }
                }
            }
            Log.Verbose("Count item failed!");
            return 0;
        }

        public List<SQL.Inventory> GetInventory()
        {
            List<SQL.Inventory> inv = new List<SQL.Inventory>();
            var query = Connect.Table<SQL.Inventory>().Where(v => v.Nick.Equals(Username)).ToList();
            if (query.Count > 0)
            {
                foreach (var item in query)
                {
                    Log.Verbose("INV: Adding {0} with count {1}", item.Item, item.ItemCount);
                    inv.Add(item);
                }
                Log.Verbose("Inventory queried sucessfully!");
                return inv;
            }
            else
            {
                Log.Warning("Cannot query inventory!");
                return null;
            }
        }

        public SQL.UserStats GetUserInfo()
        {
            var query = Connect.Table<SQL.UserStats>().Where(v => v.Nick.Equals(Username)).ToList();
            if (query.Count > 0)
            {
                return query[0];
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
            if (query.Count > 0)
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
            var query = Connect.Table<SQL.UserFishingRods>().Where(v => v.Username.Equals(Username)).ToList();
            return query.Count > 0 ? query[0] : null;
        }

        public FishingError.RodErrors AddRod(string rodname)
        {
            var query = Connect.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).ToList();

            if (query.Any())
            {
                var userod = GetRod();

                if (userod == null)
                {
                    Log.Verbose("INSERTING rod {0}", rodname);
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

        public string[] GetGarbageSites()
        {
            var query = Connect.Table<SQL.UserSearchIgnores>().Where(v => v.Username.Equals(Username)).ToList();
            if (query.Any())
            {
                return query[0].Urls.Split("\n");
            }
            return null;
        }

        public bool SetGarbageSites(string siteName, bool isDelete = false)
        {
            string[] sites = GetGarbageSites();
            if (!isDelete)
            {
                if (sites == null)
                {
                    var site = new SQL.UserSearchIgnores()
                    {
                        Username = Username,
                        Urls = siteName,
                    };

                    Connect.Insert(site);
                    return true;
                }
                else
                {
                    sites[sites.Length + 1] = siteName;
                    Connect.Execute("UPDATE UserFishingRods SET Urls = ? WHERE Username = ?", string.Join("\n", sites), Username);
                    return true;
                }
            }
            return false;
        }

        public (FishingError.RodErrors, SQL.FishingNests) SetNest(string nest)
        {
            var query = Connect.Table<SQL.FishingNests>().Where(v => v.Name.Equals(nest)).ToList();
            if (query.Any())
            {
                if (GetRod() != null)
                {
                    Connect.Execute("UPDATE UserFishingRods SET Nest = ? WHERE Username = ?", nest, Username);
                    return (FishingError.RodErrors.RodOk, query[0]);
                }
                else
                {
                    return (FishingError.RodErrors.RodNotFound, null);
                }
            }
            else
            {
                return (FishingError.RodErrors.RodUnknownError, null);
            }
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

            if (query.Count > 0)
            {
                var userTags = JsonConvert.DeserializeObject<List<SQL.Tag>>(query[0].JsonTag);

                bool append = false;

                foreach (var items in userTags)
                {
                    if (items.TagName == name)
                    {
                        items.TagCount += count;
                        Log.Verbose("Appending {0} count: {1}", items.TagName, count);
                        append = true;
                        break;
                    }
                }
                if (!append)
                {
                    Log.Verbose("creaing {0} count: {1}", name, count);
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
