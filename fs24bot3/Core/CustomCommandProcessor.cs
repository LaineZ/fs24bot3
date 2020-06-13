using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetIRC.Messages;
using NetIRC.Connection;
using Serilog;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.Core
{
    public static class CustomCommandProcessor
    {
        private static List<string> Variants = new List<string>();
        private static string LastCmd;

        public async static Task<bool> ProcessCmd(PrivMsgMessage message, NetIRC.Client client, SQLite.SQLiteConnection connect)
        {
            if (message.Message.StartsWith("@"))
            {
                var argsArray = message.Message.Split(" ").ToList();
                string cmdname = argsArray[0];
                //Log.Verbose("Issused command: {0}", cmdname);
                var query = connect.Table<SQL.CustomUserCommands>().Where(v => v.Command.Equals(cmdname));

                if (LastCmd != cmdname)
                {
                    Variants.Clear();
                    LastCmd = cmdname;
                }

                foreach (var cmd in query)
                {
                    //Log.Verbose("Command found: {0}", cmd.Command);
                    argsArray.RemoveAt(0); // removing command name
                    string argsString = string.Join(" ", argsArray);

                    string[] outputs = cmd.Output.Split("||");
                    int index = 0;

                    if (!Variants.Any())
                    {
                        Variants.AddRange(outputs);
                    }

                    Random random = new Random();

                    if (int.TryParse(argsString, out int result))
                    {
                        if (result >= Variants.Count || result < 0)
                        {
                            await client.SendAsync(new PrivMsgMessage (message.To, $"Учтите в следующий раз, здесь максимум: {Variants.Count}, поэтому показано рандомное сообщение"));
                            index = random.Next(Variants.Count);
                        }
                        else
                            index = result;
                        }
                    else
                    {
                        if (argsString.Any())
                        {
                            Log.Verbose("Args string is not empty!");
                            random = new Random(argsString.GetHashCode());
                        }
                        else
                        {
                            Variants.RemoveAt(index);
                        }
                        index = random.Next(Variants.Count);
                    }

                    StringBuilder argsFinal = new StringBuilder(Variants[index]);

                    var arr = connect.Table<SQL.UserStats>().ToList();
                    var nick = MessageUtils.AntiHightlight(arr[random.Next(0, arr.Count - 1)].Nick);

                    argsFinal.Replace("#USERINPUT", argsString);
                    argsFinal.Replace("#USERNAME", message.From);
                    argsFinal.Replace("#RNDNICK", nick);
                    await client.SendAsync(new PrivMsgMessage(message.To, argsFinal.ToString()));
                    return true;
                }
            }

            return false;
        }
    }
}
