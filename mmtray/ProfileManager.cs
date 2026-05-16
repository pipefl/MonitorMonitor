using System.Text.Json;

namespace mmtray;

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

    public bool ProfileExists(string profileName)
        => File.Exists(GetProfilePath(profileName));

    public string SaveProfile(string profileName, List<MonitorConfiguration.MonitorInfo> monitors)
    {
        var filePath = GetProfilePath(profileName);
        var json = JsonSerializer.Serialize(monitors, ProfileJsonContext.Default.MonitorList);
        File.WriteAllText(filePath, json);
        return filePath;
    }

    public List<MonitorConfiguration.MonitorInfo>? LoadProfile(string profileName, out string? error)
    {
        error = null;
        var filePath = GetProfilePath(profileName);

        if (!File.Exists(filePath))
        {
            error = $"Profile '{profileName}' not found.";
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize(json, ProfileJsonContext.Default.MonitorList);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    public List<string> ListProfiles()
    {
        if (!Directory.Exists(_profileDirectory))
        {
            return new List<string>();
        }

        return Directory.GetFiles(_profileDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();
    }

    public bool DeleteProfile(string profileName, out string? error)
    {
        error = null;
        var filePath = GetProfilePath(profileName);

        if (!File.Exists(filePath))
        {
            error = $"Profile '{profileName}' not found.";
            return false;
        }

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private string GetProfilePath(string profileName)
    {
        return Path.Combine(_profileDirectory, $"{profileName}.json");
    }
}
