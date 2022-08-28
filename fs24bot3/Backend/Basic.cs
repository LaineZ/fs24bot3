using System;
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

    public Basic()
    {
        BotContext = new Bot(this);
    }

    public async void SetupNick(string nickname) { }
    public async void JoinChannel(string name)
    {
        Log.Information("Joining channel: {0}", name);
    }
    public async void PartChannel(string name)
    {
        Log.Information("Parting channel: {0}", name);
    }

    public async Task SendMessage(string channel, string message)
    {
        Log.Information(message);
    }

    public async void Process()
    {
        Thread thread = new Thread(async () =>
        {
            BotContext.ProccessInfinite();
        });
        thread.Start();

        while (true)
        {
            Console.Write("fs24bot3: ");
            string value = Console.ReadLine().TrimEnd();
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