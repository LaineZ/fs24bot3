using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using NLua;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace fs24bot3.Core
{
    class LuaExecutor
    {
        private Client Client { get; }
        private SQLite.SQLiteConnection Connect { get; }
        private List<ParsedIRCMessage> MessageBus { get; }
        private SQL.CustomUserCommands Command { get; }

        public LuaExecutor(Client client, SQLite.SQLiteConnection connect, List<ParsedIRCMessage> messageBus, SQL.CustomUserCommands command)
        {
            Client = client;
            Connect = connect;
            MessageBus = messageBus;
            Command = command;


        }

        public void Execute(string senderNick, string channel, string message, string args)
        {
            var arr = Connect.Table<SQL.UserStats>().ToList();
            var nick = MessageUtils.AntiHightlight(arr[new Random().Next(0, arr.Count - 1)].Nick);

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
            lua["pcall"] = null;
            lua["xpcall"] = null;
            lua["load"] = null;

            // just a bunch of globals
            lua["RANDOM_NICK"] = nick;
            lua["CMD_NAME"] = Command.Command;
            lua["CMD_OWNER"] = Command.Nick;
            lua["CMD_ARGS"] = args;
            LuaFunctions luaFunctions = new LuaFunctions(Connect, senderNick, Command.Command);
            lua["Cmd"] = luaFunctions;

            Thread thread = new Thread(async () =>
            {
                try
                {
                    var res = (string)lua.DoString(Command.Output)[0];
                    int count = 0;

                    if (res.Length < 20000)
                    {
                        foreach (string outputstr in res.Split("\n"))
                        {
                            if (!string.IsNullOrWhiteSpace(outputstr))
                            {
                                await Client.SendAsync(new PrivMsgMessage(channel, outputstr[..Math.Min(350, outputstr.Length)]));
                            }
                            count++;
                            if (count > 5)
                            {
                                string link = await new HttpTools().UploadToTrashbin(res, "addplain");
                                await Client.SendAsync(new PrivMsgMessage(channel, senderNick + ": Полный вывод здесь: " + link));
                                break;
                            }
                        }
                    }
                    else
                    {
                        string link = await new HttpTools().UploadToTrashbin(res, "addplain");
                        await Client.SendAsync(new PrivMsgMessage(channel, senderNick + ": Полный вывод здесь: " + link));
                    }
                }
                catch (Exception e)
                {
                    await Client.SendAsync(new PrivMsgMessage(channel, $"Ошибка в Lua: {e.Message}"));
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
                        long memoryUsed = currentProc.WorkingSet64 / 1024 / 1024;

                        if (memoryUsed > 150)
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
}