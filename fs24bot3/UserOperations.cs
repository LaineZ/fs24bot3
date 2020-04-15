using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Serilog;
using SQLite;
using System.Linq;

namespace fs24bot3
{
    class UserOperations
    {
        public string Username;
        public SQLiteConnection Connect;
        public CommandProcessor.CustomCommandContext Ctx;


        public UserOperations(string username, SQLiteConnection connection, CommandProcessor.CustomCommandContext ctx = null)
        {
            Username = username;
            Connect = connection;
            Ctx = ctx;
        }


        public bool IncreaseXp(int count)
        {
            var query = Connect.Table<Models.SQL.UserStats>().Where(v => v.Nick.Equals(Username));

            foreach (var nick in query)
            {
                Connect.Execute("UPDATE UserStats SET Xp = Xp + ? WHERE Nick = ?", count, nick.Nick);

                Log.Verbose("{4}: Setting level: {0} {1} Current data: {2}/{3}", count, nick.Level, nick.Xp, nick.Need, nick.Nick);

                if (nick.Xp >= nick.Need)
                {
                    Connect.Execute("UPDATE UserStats SET Level = Level + 1 WHERE Nick = ?", nick.Nick);
                    Connect.Execute("UPDATE UserStats SET Xp = 0 WHERE Nick = ?", nick.Nick);
                    Connect.Execute("UPDATE UserStats SET Need = Level * 120 WHERE Nick = ?", nick.Nick);
                    return true;
                }
            }
            return false;
        }

        public void SetLevel(int level)
        {
            Connect.Execute("UPDATE UserStats SET Level = ? WHERE Nick = ?", level, Username);
            Connect.Execute("UPDATE UserStats SET Need = Level * 120 WHERE Nick = ?", Username);
        }

        public bool AddItemToInv(string name, int count)
        {
            try
            {
                Connect.Execute("INSERT INTO Inventory VALUES(?, ?, ?)", Username, Shop.getItem(name).Name, count);
                Log.Verbose("Inserting items");
                return true;
            }
            catch (SQLiteException)
            {
                Connect.Execute("UPDATE Inventory SET Count = Count + ? WHERE Item = ? AND Nick = ?", count, Shop.getItem(name).Name, Username);
                Log.Verbose("Updating items {0}", name);
                return true;
            }
        }

        public bool RemItemFromInv(string name, int count)
        {
            string itemname = Shop.getItem(name).Name;
            var query = Connect.Table<Models.SQL.Inventory>().Where(v => v.Nick.Equals(Username) && v.Item.Equals(itemname)).ToList();
            if (query.Count > 0)
            {
                foreach (var item in query)
                {
                    if (item.Item == Shop.getItem(name).Name && item.ItemCount >= count)
                    {
                        Connect.Execute("UPDATE Inventory SET Count = Count - ? WHERE Item = ? AND Nick = ?", count, itemname, Username);
                        // clening up items with 0
                        Connect.Execute("DELETE FROM Inventory WHERE Count = 0");
                        if (Ctx != null)
                        {
                            Ctx.SendMessage(Ctx.Channel, $" {Shop.getItem(name).Name} -{count} За использование данной команды");
                        }
                        return true;
                    }
                }
            }
            if (Ctx != null)
            {
                Ctx.SendMessage(Ctx.Channel, $"Недостаточно {Shop.getItem(name).Name} x{count}");
            }
            return false;
        }

        public List<Models.SQL.Inventory> GetInventory()
        {
            List<Models.SQL.Inventory> inv = new List<Models.SQL.Inventory>();
            var query = Connect.Table<Models.SQL.Inventory>().Where(v => v.Nick.Equals(Username)).ToList();
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

        public Models.SQL.UserStats GetUserInfo()
        {
            var query = Connect.Table<Models.SQL.UserStats>().Where(v => v.Nick.Equals(Username)).ToList();
            if (query.Count > 0)
            {
                return query[0];
            }
            else
            {
                throw new Core.Exceptions.UserNotFoundException();
            }
        }

        public List<Models.SQL.Tag> GetUserTags()
        {
            List<Models.SQL.Tag> tags = new List<Models.SQL.Tag>();
            var query = Connect.Table<Models.SQL.Tags>().Where(v => v.Username.Equals(Username)).ToList();
            if (query.Count > 0)
            {
                var userNick = JsonConvert.DeserializeObject<List<Models.SQL.Tag>>(query[0].JsonTag);

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


        public bool AddTag(string name, int count)
        {

            var userinfo = GetUserInfo();

            if (userinfo == null)
            {
                Log.Error("User {0} not found!", Username);
                return false;
            }

            var query = Connect.Table<Models.SQL.Tags>().Where(v => v.Username.Equals(Username)).ToList();

            if (query.Count > 0)
            {
                var userTags = JsonConvert.DeserializeObject<List<Models.SQL.Tag>>(query[0].JsonTag);

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
                    userTags.Add(new Models.SQL.Tag() { TagName = name, TagCount = count });
                }

                Connect.Execute("UPDATE Tags SET JsonTag = ? WHERE Nick = ?", JsonConvert.SerializeObject(userTags).ToString(), Username);
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

                var tagList = new List<Models.SQL.Tag>();
                tagList.Add(tagInfo.GetTagByName());

                var user = new Models.SQL.Tags()
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
