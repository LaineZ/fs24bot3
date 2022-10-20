using Newtonsoft.Json;

namespace fs24bot3.Models;

public class ChatBotResponse
{
    [JsonProperty("ok")] public bool Ok { get; set; }

    [JsonProperty("text")] public string Text { get; set; }

    [JsonProperty("uid")] public string Uid { get; set; }
}

public class ChatBotRequest
{
    [JsonProperty("bot")]
    public string Bot { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("uid")]
    public string Uid { get; set; }

    public ChatBotRequest(string message)
    {
        Bot = "main";
        Text = message;
        Uid = null;
    }
}