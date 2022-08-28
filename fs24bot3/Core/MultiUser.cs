using fs24bot3.Models;
using SQLite;
using System.Linq;

namespace fs24bot3.Core;
class MultiUser
{
    public SQLiteConnection Connect;

    public MultiUser(in SQLiteConnection connection)
    {
        Connect = connection;
    }

    public double GetItemAvg(string itemname = "money")
    {
        var query = Connect.Table<SQL.Inventory>().Where(x => x.Item == itemname).Average(x => x.ItemCount);
        return query;
    }
}
