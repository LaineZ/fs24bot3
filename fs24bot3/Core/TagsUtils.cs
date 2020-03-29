﻿using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Core
{

    public class TagsUtils
    {
        public string Name;
        public SQLite.SQLiteConnection Connection;

        public TagsUtils(string name, SQLite.SQLiteConnection connection)
        {
            Name = name;
            Connection = connection;

        }

        internal Models.SQL.Tag GetTagByName()
        {
            var query = Connection.Table<Models.SQL.Tag>().Where(v => v.TagName.Equals(Name));
            foreach (var tag in query)
            {
                return tag;
            }

            throw new Exception("Tag " + Name + " does not exsist!");
        }
    }
}