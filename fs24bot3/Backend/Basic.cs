using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.Models;
using Serilog;

namespace fs24bot3.Backend;

public class Basic : IMessagingClient
{
    public string Name { get; }
    public Bot BotContext { get; }

    public Dictionary<string, string> Fmt { get; }

    public Basic()
    {
        BotContext = new Bot(this);
        Fmt = new Dictionary<string, string>();
        Name = "fs24bot3";
    }

    public void SetupNick(string nickname) { }
    public void JoinChannel(string name)
    {
        Log.Information("Joining channel: {0}", name);
    }
    public void PartChannel(string name)
    {
        Log.Information("Parting channel: {0}", name);
    }

    public async Task SendMessage(string channel, string message)
    {
        await Task.Run(() =>
        {
            Log.Information(message);
        });
    }

    public async void Process()
    {
        var thread = new Thread(() =>
        {
            BotContext.ProccessInfinite();
        });
        thread.Start();

        while (true)
        {
            Console.Write("fs24bot3: ");
            string value = Console.ReadLine()?.TrimEnd();
            if (!string.IsNullOrWhiteSpace(value))
            {
                var msg = new MessageGeneric(value, "testchannel", 
                    new User("test", BotContext.Connection));
                BotContext.MessageTrigger(msg);
                await BotContext.ExecuteCommand(msg, "");
            }
        }
    }
}