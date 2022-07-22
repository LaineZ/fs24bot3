using fs24bot3.Models;
using Serilog;
using System.Linq;

namespace fs24bot3.Core;
public class CustomCommandProcessor
{
    private Bot Context;
    private CustomExecutor CustomExecutor { get; }
    public CustomCommandProcessor(Bot context)
    {
        Context = context;
        CustomExecutor = new CustomExecutor(Context);
        Log.Information("Custom command processor enabled!");
    }

    public bool ProcessCmd(string prefix, in MessageGeneric message)
    {
        if (message.Body.StartsWith(prefix))
        {
            var argsArray = message.Body.Split(" ").ToList();
            // remove command prefix
            string cmdname = argsArray[0][1..];
            //Log.Verbose("Issused command: {0}", cmdname);
            var cmd = Context.Connection.Table<SQL.CustomUserCommands>().SingleOrDefault(x => x.Command == cmdname);

            if (cmd != null)
            {
                argsArray.RemoveAt(0); // removing command name

                if (cmd.IsLua == 0)
                {
                    CustomExecutor.Execute(cmd, message.Sender.Username, message.Target, string.Join(" ", argsArray));
                }
                else
                {
                    new LuaExecutor(Context, cmd).Execute(message.Sender.Username, message.Target, message.Body, string.Join(" ", argsArray));
                }
                return true;
            }
        }
        return false;
    }
}
