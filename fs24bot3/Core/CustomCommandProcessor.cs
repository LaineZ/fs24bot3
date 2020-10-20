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
using System.Diagnostics;

namespace fs24bot3.Core
{
    public static class CustomCommandProcessor
    {
        public async static Task<bool> ProcessCmd(PrivMsgMessage message, NetIRC.Client client, SQLite.SQLiteConnection connect, List<PrivMsgMessage> messageBus)
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
                            if (result > outputs.Length || result < 0)
                            {
                                await client.SendAsync(new PrivMsgMessage(message.To, $"Учтите в следующий раз, здесь максимум: {outputs.Length - 1}, поэтому показано рандомное сообщение"));
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
                        Lua lua = new Lua();
                        lua.State.Encoding = Encoding.UTF8;

                        // block danger functions
                        lua["os.execute"] = null;
                        lua["os.exit"] = null;
                        lua["os.remove"] = null;
                        lua["os.getenv"] = null;
                        lua["os.rename"] = null;
                        lua["os.setlocale"] = null;
                        lua["os.tmpname"] = null;

                        lua["io"] = null;
                        lua["debug"] = null;
                        lua["require"] = null;
                        lua["print"] = null;

                        // just a bunch of globals
                        lua["RANDOM_NICK"] = nick;
                        lua["CMD_NAME"] = cmd.Command;
                        lua["CMD_OWNER"] = cmd.Nick;
                        lua["CMD_ARGS"] = string.Join(" ", argsArray);
                        LuaFunctions luaFunctions = new LuaFunctions(connect, message.From, messageBus);
                        lua["Cmd"] = luaFunctions;

                        Thread thread = new Thread(async () =>
                        {
                            try
                            {
                                var res = (string)lua.DoString(cmd.Output)[0];
                                int count = 0;

                                if (res.Length < 20000)
                                {
                                    foreach (string outputstr in res.Split("\n"))
                                    {
                                        if (!string.IsNullOrWhiteSpace(outputstr))
                                        {
                                            await client.SendAsync(new PrivMsgMessage(message.To, outputstr[..Math.Min(350, outputstr.Length)]));
                                        }
                                        count++;
                                        if (count > 5)
                                        {
                                            string link = await new HttpTools().UploadToTrashbin(res, "addplain");
                                            await client.SendAsync(new PrivMsgMessage(message.To, message.From + ": Полный вывод здесь: " + link));
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    string link = await new HttpTools().UploadToTrashbin(res, "addplain");
                                    await client.SendAsync(new PrivMsgMessage(message.To, message.From + ": Полный вывод здесь: " + link));
                                }
                            }
                            catch (Exception e)
                            {
                                await client.SendAsync(new PrivMsgMessage(message.To, $"Ошибка в Lua: {e.Message}"));
                                lua.Close();
                                lua.Dispose();
                            }
                        });
                        thread.Start();

                        // watch thread
                        Thread threadWatch = new Thread(() =>
                        {
                            try
                            {
                                Thread.Sleep(10000);
                                if (thread.IsAlive)
                                {
                                    lua.State.Error("too long run time (10 seconds)");
                                }
                            }
                            catch (Exception)
                            {
                                Log.Information("Lua thread watcher has stopped working...");
                            }
                        });
                        threadWatch.Start();

                        new Thread(() =>
                        {
                            while (thread.IsAlive)
                            {
                                try
                                {
                                    Thread.Sleep(10);
                                    Process currentProc = Process.GetCurrentProcess();
                                    long memoryUsed = currentProc.PrivateMemorySize64 / 1024 / 1024;

                                    if (memoryUsed > 350)
                                    {
                                        lua.State.Error("out of memory " + memoryUsed + " mb");
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    Log.Warning("Lua command has ended with out of memory!");
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
