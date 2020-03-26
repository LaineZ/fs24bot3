using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrcClientCore;
using Serilog;

namespace fs24bot3.Core
{
    public static class CustomCommandProcessor
    {
        public static bool ProcessCmd(Irc socket, Message message, SQLite.SQLiteConnection connect)
        {
            if (message.Text.StartsWith("@"))
            {
                var argsArray = message.Text.Split(" ").ToList();
                string cmdname = argsArray[0];
                Log.Verbose("Issused command: {0}", cmdname);
                var query = connect.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(cmdname));
                foreach (var cmd in query)
                {
                    Log.Verbose("Command found: {0}", cmd.Command);
                    argsArray.RemoveAt(0);
                    string argsString = string.Join(" ", argsArray);

                    string[] outputs = cmd.Output.Split("||");

                    Random random = new Random();
                    int index = random.Next(outputs.Length);

                    StringBuilder argsFinal = new StringBuilder(outputs[index]);
                    argsFinal.Replace("#USERINPUT", argsString);
                    argsFinal.Replace("#USERNAME", message.User);
                    socket.SendMessage(message.Channel, argsFinal.ToString());
                    return true;
                }
            }

            return false;
        }
    }
}
