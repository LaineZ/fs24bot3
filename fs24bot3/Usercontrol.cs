using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace fs24bot3
{
    class Usercontrol
    {
        SQLiteConnection Database;

        public Usercontrol(SQLiteConnection database)
        {
            Database = database;
        }

        private void prepareAndExec(SQLiteCommand cmd)
        {
            cmd.Prepare();
            cmd.ExecuteNonQuery();
        }

        public bool giveXP(string username, int xp)
        {
            var cmd = new SQLiteCommand(Database);
            cmd.CommandText = "UPDATE nicks SET xp = xp + @xp WHERE username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@xp", xp);
            prepareAndExec(cmd);
 
            cmd.CommandText = "SELECT xp, level, need FROM nicks WHERE username = @username LIMIT 1";
            cmd.Parameters.AddWithValue("@username", username);
            prepareAndExec(cmd);

            SQLiteDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if ((int)rdr["xp"] >= (int)rdr["need"])
                {
                    cmd.CommandText = "UPDATE nicks SET level = level + @level WHERE username = @username";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@level", (int)rdr["level"] + 1);
                    prepareAndExec(cmd);
                    return true;
                }
            }

            return false;
        }
    }
}
