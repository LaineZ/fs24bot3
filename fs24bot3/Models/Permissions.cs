using System;
using Serilog;
using SQLite;

namespace fs24bot3.Models;

[Flags]
public enum PermissionsFlags
{
    None = 0,
    ExecuteCommands = 1,
    Bridge = 2,
    HandleProcessing = 4,
    HandleUrls = 8,
    Admin = 16
}

// ultimate table99999
public class Permissions
{
    [PrimaryKey] public string Username { get; set; }
    public PermissionsFlags Flags { get; set; }
    [Ignore] public bool ExecuteCommands { get; private set; }
    [Ignore] public bool Bridge { get; private set; }
    [Ignore] public bool HandleProcessing { get; private set; }
    [Ignore] public bool HandleUrls { get; private set; }
    [Ignore] public bool Admin { get; private set; }

    public Permissions()
    {
    }

    public Permissions(string username, PermissionsFlags flags)
    {
        Username = username;
        Flags = flags;
        
        ApplyValuesAsBitflags();
    }

    public Permissions(string username)
    {
        Username = username;
        Flags = PermissionsFlags.ExecuteCommands | PermissionsFlags.HandleProcessing |
                           PermissionsFlags.HandleUrls;
        
        ApplyValuesAsBitflags();
    }

    public void TooglePermission(PermissionsFlags flag)
    {
        Flags ^= flag;
        
        Log.Verbose("{0}", Flags);
    }

    public void ApplyValuesAsBitflags()
    {
        ExecuteCommands = (Flags & PermissionsFlags.ExecuteCommands) == PermissionsFlags.ExecuteCommands;
        Bridge = (Flags & PermissionsFlags.Bridge) == PermissionsFlags.Bridge;
        HandleProcessing = (Flags & PermissionsFlags.HandleProcessing) == PermissionsFlags.HandleProcessing;
        HandleUrls = (Flags & PermissionsFlags.HandleUrls) == PermissionsFlags.HandleUrls;
        Admin = (Flags & PermissionsFlags.Admin) == PermissionsFlags.Admin;
    }

    public void ConvertValuesToBitflags()
    {
        Flags = PermissionsFlags.None; 

        if (ExecuteCommands)
        {
            Flags |= PermissionsFlags.ExecuteCommands;
        }

        if (Bridge)
        {
            Flags |= PermissionsFlags.Bridge;
        }

        if (HandleProcessing)
        {
            Flags |= PermissionsFlags.HandleProcessing;
        }

        if (HandleUrls)
        {
            Flags |= PermissionsFlags.HandleUrls;
        }

        if (Admin)
        {
            Flags |= PermissionsFlags.Admin;
        }
    }
}