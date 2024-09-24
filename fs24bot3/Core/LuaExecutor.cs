using fs24bot3.Helpers;
using fs24bot3.Models;
using KeraLua;
using Serilog;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Core;
class LuaExecutor
{

    public const long MEMORY_LIMIT = 10_000_000; // 10 MB Memory limit (roughly)

    private Bot Context;
    private SQL.CustomUserCommands Command { get; }

    public LuaExecutor(Bot context, SQL.CustomUserCommands command)
    {
        Context = context;
        Command = command;
    }


    public static NLua.Lua CreateLuaState(int timeout = 2000)
    {
        IntPtr pointer = IntPtr.Zero;
        long currentMemoryUsage = 0;

        NLua.Lua lua = new NLua.Lua();

        lua.State.SetAllocFunction(((ud, ptr, osize, nsize) =>
        {
            currentMemoryUsage += (long)nsize;

            Log.Verbose("[LUA] MEMORY ALLOCATION: {0} bytes width: {1}",
                currentMemoryUsage, (int)nsize.ToUInt32());

            if (currentMemoryUsage > LuaExecutor.MEMORY_LIMIT)
            {
                return IntPtr.Zero;
            }

            return ptr != IntPtr.Zero && nsize.ToUInt32() > 0 ?
            Marshal.ReAllocHGlobal(ptr, unchecked((IntPtr)(long)(ulong)nsize)) :
            Marshal.AllocHGlobal((int)nsize.ToUInt32());
        }), ref pointer);

        lua.State.Encoding = Encoding.UTF8;
        var start = DateTime.Now;
        
        lua.State.SetHook(((_, _) =>
        {
            var current = DateTime.Now;
            if ((current - start).TotalMilliseconds > timeout)
            {
                lua.State.Error("execution timeout");
            }
        }), LuaHookMask.Count, 1);

        return lua;
    }

    public async Task Execute(string senderNick, string channel, string message, string args)
    {
        var arr = Context.Connection.Table<SQL.UserStats>().ToList();
        var nick = MessageHelper.AntiHightlight(arr[new Random().Next(0, arr.Count - 1)].Nick);
        var lua = CreateLuaState();
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
        lua["loadfile"] = null;
        lua["dofile"] = null;

        // just a bunch of globals
        lua["RANDOM_NICK"] = nick;
        lua["CMD_NAME"] = Command.Command;
        lua["CMD_OWNER"] = Command.Nick;
        lua["CMD_ARGS"] = args;
        LuaFunctions luaFunctions = new LuaFunctions(Context.Connection, 
                                        senderNick, Command.Command);
        lua["Cmd"] = luaFunctions;

        try
        {
            var res = lua.DoString(Command.Output)[0].ToString();
            await Context.Client.SendMessage(channel, res);
        }
        catch (Exception e)
        {
            await Context.Client.SendMessage(channel,
                $"Ошибка Lua скрипта: {e.Message}");
        }
        finally
        {
            lua.Close();
            lua.Dispose();
        }
    }
}