﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetIRC.Messages;
using NetIRC.Connection;
using Serilog;
using System.Threading.Tasks;

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
                var query = connect.Table<Models.SQL.CustomUserCommands>().Where(v => v.Command.Equals(cmdname));
                
                foreach (var cmd in query)
                {
                    //Log.Verbose("Command found: {0}", cmd.Command);
                    argsArray.RemoveAt(0); // removing command name
                    string argsString = string.Join(" ", argsArray);

                    string[] outputs = cmd.Output.Split("||");
                    uint index = 0;

                    if (uint.TryParse(argsString, out uint result))
                    {
                        if (result >= outputs.Length)
                        {
                            await client.SendAsync(new PrivMsgMessage (message.To, $"Учтите в следующий раз, здесь максимум: {outputs.Length - 1}, поэтому показано рандомное сообщение"));
                        }
                        else
                            index = result;
                        }
                    else
                    {
                        Random random = new Random();
                        index = (uint)random.Next(outputs.Length);
                    }

                    StringBuilder argsFinal = new StringBuilder(outputs[index]);
                    argsFinal.Replace("#USERINPUT", argsString);
                    argsFinal.Replace("#USERNAME", message.From);
                    await client.SendAsync(new PrivMsgMessage(message.To, argsFinal.ToString()));
                    return true;
                }
            }

            return false;
        }
    }
}
