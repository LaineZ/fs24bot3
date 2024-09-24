using Newtonsoft.Json;

namespace fs24bot3.Helpers;
public class JsonSerializerHelper
{
    public static readonly JsonSerializerSettings OPTIMIMAL_SETTINGS = new JsonSerializerSettings()
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };
}