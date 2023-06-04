﻿using fs24bot3.Helpers;
using fs24bot3.Models;
using NLua;
using Serilog;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace fs24bot3.Core;
class LuaExecutor
{

    public const long MEMORY_LIMIT = 10_000_000; // 10 MB Memory limit (roughly)
    private Bot Context;
    private SQL.CustomUserCommands Command { get; }
    private IntPtr Pointer = IntPtr.Zero;
    private long CurrentMemoryUsage = 0;

    public LuaExecutor(Bot context, SQL.CustomUserCommands command)
    {
        Context = context;
        Command = command;
    }

    public void Execute(string senderNick, string channel, string message, string args)
    {
        var arr = Context.Connection.Table<SQL.UserStats>().ToList();
        var nick = MessageHelper.AntiHightlight(arr[new Random().Next(0, arr.Count - 1)].Nick);

        Lua lua = new Lua();
        lua.State.SetAllocFunction(((ud, ptr, osize, nsize) =>
        {
            CurrentMemoryUsage += (long)nsize;

            Log.Verbose("[LUA] MEMORY ALLOCATION: {0} bytes width: {1}", 
                CurrentMemoryUsage, (int)nsize.ToUInt32());

            if (CurrentMemoryUsage > MEMORY_LIMIT)
            {
                return IntPtr.Zero;
            }

            return ptr != IntPtr.Zero && nsize.ToUInt32() > 0 ? 
            Marshal.ReAllocHGlobal(ptr, unchecked((IntPtr)(long)(ulong)nsize)) : 
            Marshal.AllocHGlobal((int)nsize.ToUInt32());
        }), ref Pointer);
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
        LuaFunctions luaFunctions = new LuaFunctions(Context.Connection, 
                                        senderNick, Command.Command);
        lua["Cmd"] = luaFunctions;

        Thread thread = new Thread(async () =>
        {
            try
            {
                var res = (string)lua.DoString(Command.Output)[0];
                await Context.Client.SendMessage(channel, res);
            }
            catch (Exception e)
            {
                await Context.Client.SendMessage(channel, 
                    $"Ошибка Lua скрипта: {e.Message}");
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
                    lua.State.Close();
                }
            }
            catch (Exception)
            {
                Log.Information("Lua thread watcher has stopped working...");
            }
        });

        threadWatch.Start();
    }
}