using fs24bot3.Models;
using NetIRC;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Core
{
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

        public bool ProcessCmd(string prefix, string senderNick, string channel, string message)
        {
            if (message.StartsWith(prefix))
            {
                var argsArray = message.Split(" ").ToList();
                // remove command prefix
                string cmdname = argsArray[0][1..];
                //Log.Verbose("Issused command: {0}", cmdname);
                var cmd = Context.Connection.Table<SQL.CustomUserCommands>().SingleOrDefault(x => x.Command == cmdname);

                if (cmd != null)
                {
                    argsArray.RemoveAt(0); // removing command name

                    if (cmd.IsLua == 0)
                    {
                        CustomExecutor.Execute(cmd, senderNick, channel, string.Join(" ", argsArray));
                    }
                    else
                    {
                        new LuaExecutor(Context, cmd).Execute(senderNick, channel, message, string.Join(" ", argsArray));
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
