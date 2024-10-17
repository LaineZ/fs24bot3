using System.Linq;
using fs24bot3.Core;
using fs24bot3.Helpers;
using NetIRC;
using Serilog;
using SQLite;

namespace fs24bot3.Models;

public enum MessageKind
{
    Message,
    MessagePersonal,
    MessageFromBridge
}

public class MessageGeneric
{
    public string Body { get; }
    public Core.User Sender { get; }
    public string Target { get; }
    public MessageKind Kind { get; }

    public MessageGeneric(in ParsedIRCMessage message, in SQLiteConnection connection, string clientName)
    {
        Sender = new Core.User(message.Prefix.From, connection);
        Target = message.Parameters[0];
        Body = message.Trailing.TrimEnd();
        Kind = MessageKind.Message;

        if (message.Parameters[0] == clientName)
        {
            Target = message.Prefix.From;
            Kind = MessageKind.MessagePersonal;
        }

        if (Sender.Username == ConfigurationProvider.Config.Services.BridgeNickname)
        {
            // trim bridged user nickname like
            // <cheburator> //bpm140//: @ms привет
            var msg = Body.Split(":");
            Body = string.Join(":", msg.Skip(1)).TrimStart();
            Sender = new Core.User("@[" + MessageHelper.StripIRC(msg.First()) + "]", connection);
            Kind = MessageKind.MessageFromBridge;
            Log.Verbose("Message from the bridge: {0} from {1}", Body, Sender);
        }
    }

    public MessageGeneric(string body, string target, Core.User sender, MessageKind messageKind = MessageKind.Message)
    {
        Body = body;
        Target = target;
        Sender = sender;
        Kind = messageKind;
    }
}
