using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetIRC.Messages;
using Serilog;
using System.Threading.Tasks;
using fs24bot3.Models;

namespace fs24bot3.Core
{
    public static class CustomCommandProcessor
    {
        public async static Task<bool> ProcessCmd(PrivMsgMessage message, NetIRC.Client client, SQLite.SQLiteConnection connect)
        {
            if (message.Message.StartsWith("@"))
            {
                var argsArray = message.Message.Split(" ").ToList();
                string cmdname = argsArray[0];
                //Log.Verbose("Issused command: {0}", cmdname);
                var cmd = connect.Table<SQL.CustomUserCommands>().SingleOrDefault(x => x.Command == cmdname);

                if (cmd != null)
                {
                    //Log.Verbose("Command found: {0}", cmd.Command);
                    argsArray.RemoveAt(0); // removing command name
                    string argsString = string.Join(" ", argsArray);

                    string[] outputs = cmd.Output.Split("||");
                    int index = 0;

                    Random random = new Random();

                    if (int.TryParse(argsString, out int result))
                    {
                        if (result >= outputs.Length || result < 0)
                        {
                            await client.SendAsync(new PrivMsgMessage(message.To, $"Учтите в следующий раз, здесь максимум: {outputs.Length}, поэтому показано рандомное сообщение"));
                            index = random.Next(outputs.Length);
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
                        index = random.Next(outputs.Length - 1);
                    }

                    var arr = connect.Table<SQL.UserStats>().ToList();
                    var nick = MessageUtils.AntiHightlight(arr[random.Next(0, arr.Count - 1)].Nick);
                    StringBuilder argsFinal = new StringBuilder(outputs[index]);
                    argsFinal.Replace("#USERINPUT", argsString);
                    argsFinal.Replace("#USERNAME", message.From);
                    argsFinal.Replace("#RNDNICK", nick);
                    argsFinal.Replace("#RNG", random.Next(int.MinValue, int.MaxValue).ToString());
                    await client.SendAsync(new PrivMsgMessage(message.To, argsFinal.ToString()));
                }
                return true;
            }

            return false;
        }
    }
}
