using Newtonsoft.Json;
using System.Collections.Generic;

namespace fs24bot3.Models;

public class ChatBotResponse
{
    [JsonProperty("replies")] public List<string> Replies { get; set; }
}

public class ChatBotRequest
{
    [JsonProperty("prompt")]
    public string Prompt { get; set; }

    [JsonProperty("length")]
    public int Length { get; set; }

    public ChatBotRequest(string message)
    {
        Prompt = message;
        Length = message.Length;
    }
}