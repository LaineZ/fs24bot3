namespace fs24bot3.Models;

public class ResultGeneric
{
    public string Title { get; }
    public string Url { get; }
    public string Description { get; }

    public ResultGeneric(string title, string url, string description) 
    {
        Title = title;
        Url = url;
        Description = description;
    }
}
