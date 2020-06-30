using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetIRC.Messages;
using Serilog;
using System.Threading.Tasks;
using fs24bot3.Models;
using NLua;
using System.Threading;

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
                    Random random = new Random();
                    var arr = connect.Table<SQL.UserStats>().ToList();
                    var nick = MessageUtils.AntiHightlight(arr[random.Next(0, arr.Count - 1)].Nick);
                    argsArray.RemoveAt(0); // removing command name

                    if (cmd.IsLua == 0)
                    {
                        //Log.Verbose("Command found: {0}", cmd.Command);
                        string argsString = string.Join(" ", argsArray);

                        string[] outputs = cmd.Output.Split("||");
                        int index = 0;

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

                        StringBuilder argsFinal = new StringBuilder(outputs[index]);
                        argsFinal.Replace("#USERINPUT", argsString);
                        argsFinal.Replace("#USERNAME", message.From);
                        argsFinal.Replace("#RNDNICK", nick);
                        argsFinal.Replace("#RNG", random.Next(int.MinValue, int.MaxValue).ToString());

                        await client.SendAsync(new PrivMsgMessage(message.To, argsFinal.ToString()));
                    }
                    else
                    {
                        Thread thread = new Thread(async () =>
                        {
                            try
                            {
                                using (Lua lua = new Lua())
                                {
                                    lua.State.Encoding = Encoding.UTF8;

                                    // block danger functions
                                    lua["os"] = null;
                                    lua["io"] = null;
                                    lua["debug"] = null;
                                    lua["require"] = null;
                                    lua["print"] = null;
                                    // just a bunch of globals
                                    lua["RANDOM_NICK"] = nick;
                                    lua["CMD_NAME"] = cmd.Command;
                                    lua["CMD_OWNER"] = cmd.Nick;
                                    lua["CMD_ARGS"] = string.Join(" ", argsArray);


                                    var res = (string)lua.DoString(cmd.Output)[0];

                                    await client.SendAsync(new PrivMsgMessage(message.To, res));
                                }
                            }
                            catch (Exception e)
                            {
                                await client.SendAsync(new PrivMsgMessage(message.To, $"Ошибка в Lua: {e.Message}"));
                            }
                        });
                        thread.Start();

                        // watch thread
                        new Thread(async () =>
                        {
                            Thread.Sleep(10000);
                            if (thread.IsAlive)
                            {
                                await client.SendAsync(new PrivMsgMessage(message.To, $"{cmd.Command}: Слишком долгое время выполнения..."));
                                try
                                {
                                    thread.Abort();
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }).Start();
                    }
                }
                return true;
            }

            return false;
        }
    }
}
