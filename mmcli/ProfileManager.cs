using System.Text.Json;

namespace mmcli;

public class ProfileManager
{
    private readonly string _profileDirectory;

    public ProfileManager()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _profileDirectory = Path.Combine(appDataPath, "mmcli", "profiles");
        
        if (!Directory.Exists(_profileDirectory))
        {
            Directory.CreateDirectory(_profileDirectory);
        }
    }

    public void SaveProfile(string profileName, List<MonitorConfiguration.MonitorInfo> monitors)
    {
        var filePath = GetProfilePath(profileName);
        var json = JsonSerializer.Serialize(monitors, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Profile '{profileName}' saved successfully to: {filePath}");
    }

    public List<MonitorConfiguration.MonitorInfo>? LoadProfile(string profileName)
    {
        var filePath = GetProfilePath(profileName);
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: Profile '{profileName}' not found.");
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var monitors = JsonSerializer.Deserialize<List<MonitorConfiguration.MonitorInfo>>(json);
            return monitors;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading profile: {ex.Message}");
            return null;
        }
    }

    public List<string> ListProfiles()
    {
        if (!Directory.Exists(_profileDirectory))
        {
            return new List<string>();
        }

        var profiles = Directory.GetFiles(_profileDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();

        return profiles;
    }

    public bool DeleteProfile(string profileName)
    {
        var filePath = GetProfilePath(profileName);
        
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: Profile '{profileName}' not found.");
            return false;
        }

        try
        {
            File.Delete(filePath);
            Console.WriteLine($"Profile '{profileName}' deleted successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting profile: {ex.Message}");
            return false;
        }
    }

    private string GetProfilePath(string profileName)
    {
        return Path.Combine(_profileDirectory, $"{profileName}.json");
    }
}
