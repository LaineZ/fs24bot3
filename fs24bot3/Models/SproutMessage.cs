using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace fs24bot3.Models;

public enum Kind
{
    Message,
    ServerMessage
}
public class SproutMessage
{
    [JsonProperty("author")]
    public string Nick { get; set; }
    [JsonProperty("body")]
    public string Message { get; set; }
    [JsonProperty("time")]
    public DateTime Date { get; set; }
    [DefaultValue(MessageKind.Message)]
    public MessageKind Kind { get; set; }
}
