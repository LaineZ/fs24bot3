using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fs24bot3.Core;
using fs24bot3.Models;

namespace fs24bot3.Backend;
public interface IMessagingClient
{
    /// <summary>
    /// The client name e.g nickname in IRC
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The main bot context, must be instance of Bot class
    /// </summary>
    public Bot BotContext { get; }

    public Dictionary<string, string> Fmt { get; }
    public void SetupNick(string nickname) { }
    public void JoinChannel(string name)
    {
        throw new NotImplementedException();
    }
    public void PartChannel(string name)
    {
        throw new NotImplementedException();
    }

    public Task SendMessage(string channel, string message);
    public void Process() 
    { 
        BotContext.ProccessInfinite();
    }

    public Task<bool> EnsureAuthorization(User user)
    {
        return Task.FromResult(true);
    }
}