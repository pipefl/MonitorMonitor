namespace mmcli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var profileManager = new ProfileManager();

        try
        {
            switch (args[0].ToLower())
            {
                case "-save":
                case "--save":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Profile name required.");
                        Console.WriteLine("Usage: mmcli -save <profile_name>");
                        return 1;
                    }
                    SaveCurrentConfiguration(profileManager, args[1]);
                    return 0;

                case "-load":
                case "--load":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Profile name required.");
                        Console.WriteLine("Usage: mmcli -load <profile_name>");
                        return 1;
                    }
                    return LoadConfiguration(profileManager, args[1]);

                case "-list":
                case "--list":
                    ListProfiles(profileManager);
                    return 0;

                case "-delete":
                case "--delete":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Error: Profile name required.");
                        Console.WriteLine("Usage: mmcli -delete <profile_name>");
                        return 1;
                    }
                    return DeleteProfile(profileManager, args[1]) ? 0 : 1;

                case "-show":
                case "--show":
                    ShowCurrentConfiguration();
                    return 0;

                case "-help":
                case "--help":
                case "-h":
                case "/?":
                    ShowHelp();
                    return 0;

                default:
                    Console.WriteLine($"Error: Unknown command '{args[0]}'");
                    ShowHelp();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Multi-Monitor Configuration CLI (mmcli)");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  mmcli -save <profile_name>    Save current monitor configuration");
        Console.WriteLine("  mmcli -load <profile_name>    Load and apply saved configuration");
        Console.WriteLine("  mmcli -list                   List all saved profiles");
        Console.WriteLine("  mmcli -delete <profile_name>  Delete a saved profile");
        Console.WriteLine("  mmcli -show                   Show current monitor configuration");
        Console.WriteLine("  mmcli -help                   Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  mmcli -save 1and3             Save current setup as '1and3'");
        Console.WriteLine("  mmcli -save just2             Save current setup as 'just2'");
        Console.WriteLine("  mmcli -load 1and3             Apply '1and3' configuration");
        Console.WriteLine();
        Console.WriteLine("Profiles are stored in: %LocalAppData%\\mmcli\\profiles");
    }

    static void SaveCurrentConfiguration(ProfileManager profileManager, string profileName)
    {
        Console.WriteLine("Reading current monitor configuration...");
        var monitors = MonitorConfiguration.GetCurrentConfiguration();
        
        if (monitors.Count == 0)
        {
            Console.WriteLine("Warning: No active monitors detected.");
            return;
        }

        Console.WriteLine($"Found {monitors.Count} active monitor(s):");
        foreach (var monitor in monitors)
        {
            Console.WriteLine($"  - {monitor.DeviceString} ({monitor.Width}x{monitor.Height} @ {monitor.PositionX},{monitor.PositionY}){(monitor.IsPrimary ? " [PRIMARY]" : "")}");
        }

        profileManager.SaveProfile(profileName, monitors);
    }

    static int LoadConfiguration(ProfileManager profileManager, string profileName)
    {
        Console.WriteLine($"Loading profile '{profileName}'...");
        var monitors = profileManager.LoadProfile(profileName);
        
        if (monitors == null)
        {
            return 1;
        }

        Console.WriteLine($"Profile contains {monitors.Count} monitor(s):");
        foreach (var monitor in monitors)
        {
            Console.WriteLine($"  - {monitor.DeviceString} ({monitor.Width}x{monitor.Height} @ {monitor.PositionX},{monitor.PositionY}){(monitor.IsPrimary ? " [PRIMARY]" : "")}");
        }

        Console.WriteLine();
        Console.WriteLine("Applying configuration...");
        
        bool success = MonitorConfiguration.ApplyConfiguration(monitors);
        
        if (success)
        {
            Console.WriteLine("Configuration applied successfully!");
            return 0;
        }
        else
        {
            Console.WriteLine("Configuration applied with warnings. Some monitors may not have been configured correctly.");
            return 1;
        }
    }

    static void ListProfiles(ProfileManager profileManager)
    {
        var profiles = profileManager.ListProfiles();
        
        if (profiles.Count == 0)
        {
            Console.WriteLine("No saved profiles found.");
            return;
        }

        Console.WriteLine($"Saved profiles ({profiles.Count}):");
        foreach (var profile in profiles)
        {
            Console.WriteLine($"  - {profile}");
        }
    }

    static bool DeleteProfile(ProfileManager profileManager, string profileName)
    {
        return profileManager.DeleteProfile(profileName);
    }

    static void ShowCurrentConfiguration()
    {
        Console.WriteLine("Current monitor configuration:");
        var monitors = MonitorConfiguration.GetCurrentConfiguration();
        
        if (monitors.Count == 0)
        {
            Console.WriteLine("No active monitors detected.");
            return;
        }

        Console.WriteLine();
        foreach (var monitor in monitors)
        {
            Console.WriteLine($"Device:     {monitor.DeviceName}");
            Console.WriteLine($"Name:       {monitor.DeviceString}");
            Console.WriteLine($"Resolution: {monitor.Width}x{monitor.Height}");
            Console.WriteLine($"Position:   ({monitor.PositionX}, {monitor.PositionY})");
            Console.WriteLine($"Frequency:  {monitor.Frequency}Hz");
            Console.WriteLine($"Bits/Pixel: {monitor.BitsPerPixel}");
            Console.WriteLine($"Primary:    {(monitor.IsPrimary ? "Yes" : "No")}");
            Console.WriteLine();
        }
    }
}