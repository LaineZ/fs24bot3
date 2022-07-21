namespace fs24bot3.Models;
public class APIExec
{
    public class Output
    {
        public string output { get; set; }
        public int statusCode { get; set; }
        public string memory { get; set; }
        public float? cpuTime { get; set; }
    }

    public class Input
    {
        public string clientId { get; set; }
        public string clientSecret { get; set; }
        public string script { get; set; }
        public string language { get; set; }
        public string versionIndex { get; set; }
    }
}
