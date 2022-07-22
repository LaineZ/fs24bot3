using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fs24bot3.Core;
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

    public async void SetupNick(string nickname) {}
    public async void JoinChannel(string name) {}
    public async void PartChannel(string name) {}
    public async void SendMessage(string channel, string message) {}
}