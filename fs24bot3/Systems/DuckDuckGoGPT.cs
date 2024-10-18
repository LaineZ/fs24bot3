using System.Collections.Generic;
using fs24bot3.Core;
using fs24bot3.Helpers;

namespace fs24bot3.Systems;

public class DuckDuckGoGPT
{
    public readonly Dictionary<User, DuckDuckGoGPTHelper> Contexts = new();
    public DuckDuckGoGPTHelper GlobalContext = new();
}