using System.Data.SQLite;
using System.IO;

namespace fs24bot3
{
    public class SqlTools
    {
        string Database;

        public SqlTools(string database)
        {
            Database = database;
        }

        public void directQuery(string query)
        {
            using (SQLiteConnection Connect = new SQLiteConnection(@"Data Source=" + Database + "; Version=3;"))
            {
                SQLiteCommand Command = new SQLiteCommand(query, Connect);
                Connect.Open(); // открыть соединение
                Command.ExecuteNonQuery(); // выполнить запрос
                Connect.Close(); // закрыть соединение
            }
        }

        public void init()
        {
            if (!File.Exists("fsdb.sqlite"))
            {
                SqlTools sql = new SqlTools("fsdb.sqlite");

                sql.directQuery("CREATE TABLE IF NOT EXISTS nicks (id INTEGER PRIMARY KEY, username TEXT, isadmin INTEGER, xp INTEGER, level INTEGER, need INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS perks (id INTEGER PRIMARY KEY, username TEXT, perkname TEXT, level INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS vars (id INTEGER PRIMARY KEY, var_name TEXT, var TEXT, var_type TEXT);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS inv (id INTEGER PRIMARY KEY, username TEXT, itemname TEXT, itemcount INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS shop (id INTEGER PRIMARY KEY, itemname TEXT, itemprice INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS runtimecmds (command TEXT PRIMARY KEY, nick TEXT, output TEXT);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS buffs (buff TEXT PRIMARY KEY, nick TEXT, time INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS farm (id INTEGER PRIMARY KEY, nick TEXT, salary INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS tags (tag TEXT PRIMARY KEY, color INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS tags_usr (id INTEGER PRIMARY KEY, nick TEXT, tagid TEXT, count INTEGER, timestamp INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS votes (id INTEGER PRIMARY KEY, title TEXT, variant TEXT, voted INTEGER, voted_users TEXT, status TEXT, tags TEXT);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS inf (money INTEGER, tax INTEGER, date INTEGER);");
                sql.directQuery("CREATE TABLE IF NOT EXISTS dmlyrics (artist TEXT, track TEXT, lyric TEXT, nick TEXT);");
            }
        }
    }
}
