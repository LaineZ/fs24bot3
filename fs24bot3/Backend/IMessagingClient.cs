using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fs24bot3.Backend;
public interface IMessagingClient
{
    /// <summary>
    /// The client name e.g nickname in IRC
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The connected channels rooms
    /// </summanry>
    public List<string> Channels { get; }

    public async void SetupNick(string nickname) { }
    public async void JoinChannel(string name)
    {
        throw new NotImplementedException();
    }
    public async void PartChannel(string name)
    {
        throw new NotImplementedException();
    }
    public async Task SendMessage(string channel, string message) { }

    public void Process() { }
}