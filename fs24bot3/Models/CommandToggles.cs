namespace fs24bot3.Models;

public class CommandToggles
{
    public enum CommandEdit
    {
        Add,
        Delete,
    }

    public enum Switch
    {            
        Enable,
        Disable
    }

    public enum Goal {
        Get,
        Delete
    }

    public enum ColorFormats
    {
        Hex,
        RGB255,
        RGB1,
    }
}
