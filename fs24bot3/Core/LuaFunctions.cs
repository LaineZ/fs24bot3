using fs24bot3.Helpers;
using fs24bot3.Models;
using SQLite;
using System;
using System.Linq;
using System.Text;

namespace fs24bot3.Core;

public class LuaFunctions
{
    private readonly SQLiteConnection Connection;
    private string Caller { get; set; }
    private string Command { get; set; }

    public LuaFunctions(SQLiteConnection connection, string caller, string commandname)
    {
        Connection = connection;
        Caller = caller;
        Command = commandname;
    }

    public string[] GetCommandOutput(string input, string command)
    {
        StringBuilder argsFinal = new StringBuilder(Connection.Table<SQL.CustomUserCommands>()
            .SingleOrDefault(x => x.Command == command && x.IsLua == 0).Output);

        Random random = new Random();
        var arr = Connection.Table<SQL.UserStats>().ToList();
        var nick = MessageHelper.AntiHightlight(arr[random.Next(0, arr.Count - 1)].Nick);

        argsFinal.Replace("#USERINPUT", input);
        argsFinal.Replace("#USERNAME", Caller);
        argsFinal.Replace("#RNDNICK", nick);
        argsFinal.Replace("#RNG", random.Next(int.MinValue, int.MaxValue).ToString());

        return argsFinal.ToString().Split("||");
    }

    public string GetLocalStorage()
    {
        var query = Connection.Table<SQL.ScriptStorage>().Where(v => v.Nick.Equals(Caller) && v.Command == Command);

        if (query.Any())
        {
            return query.First().Data.Trim();
        }

        return null;
    }

    public bool SetLocalStorage(string data)
    {
        var query = Connection.Table<SQL.ScriptStorage>().Where(v => v.Nick.Equals(Caller) && v.Command == Command);


        if (!query.Any())
        {
            Connection.Execute("INSERT INTO ScriptStorage (Command, Nick, Data) VALUES (?, ?, ?)", Command, Caller, data);
            return true;
        }

        if (Encoding.Unicode.GetByteCount(data) < 1024)
        {
            Connection.Execute("UPDATE ScriptStorage SET Data = ? WHERE Nick = ? AND Command = ?", data, Caller, Command);
            return true;
        }

        return false;
    }

    public void ClearLocalStorage()
    {
        SetLocalStorage("");
    }

    public bool AppendLocalStorage(string data)
    {
        string totalData = GetLocalStorage() + data;

        if (totalData == null)
        {
            return false;
        }

        if (Encoding.Unicode.GetByteCount(totalData) < 1024)
        {
            Connection.Execute("UPDATE ScriptStorage SET Data = ? WHERE Nick = ? AND Command = ?", totalData, Caller, Command);
            return true;
        }
        else
        {
            return false;
        }
    }
}