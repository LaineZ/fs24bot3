using System.Collections.Generic;
using Newtonsoft.Json;

namespace fs24bot3.Models;

public class DuckDuckGoGPT
{
    public class MessageHistoryJSON
    {
        [JsonProperty("content")] public string Content;
        [JsonProperty("role")] public string Role;

        public MessageHistoryJSON(string content, string role = "user")
        {
            Content = content;
            Role = role;
        }
    }

    public class GPTShitOutputJson
    {
        [JsonProperty("message")] public string Message;
    }

    public class ChatContextJSON
    {
        [JsonProperty("model")] public string Model = "gpt-4o-mini";
        [JsonProperty("messages")] public List<MessageHistoryJSON> Messages = new();
    }
}