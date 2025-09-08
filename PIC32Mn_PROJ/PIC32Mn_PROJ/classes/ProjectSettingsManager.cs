using System.IO;
using System.Text.Json;

namespace PIC32Mn_PROJ.classes
{
    public static class ProjectSettingsManager
    {
       // private const string SettingsFileName = "ProjectSettings.json";

        public static string SettingsFileName_;


        // Pass the project directory path to these methods
        public static ProjectSettings Load(string projectDirectory)
        {
            var settingsPath = Path.Combine(projectDirectory, SettingsFileName_);
            if (!File.Exists(settingsPath))
                return new ProjectSettings();

            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<ProjectSettings>(json) ?? new ProjectSettings();
        }

        public static void Save(string projectDirectory, ProjectSettings settings)
        {
            var settingsPath = Path.Combine(projectDirectory, SettingsFileName_);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }

        public static void SaveKey(string projectDirectory, string key, object? value)
        {
            var settings = Load(projectDirectory);
            settings[key] = value;
            Save(projectDirectory, settings);
        }

        public static object? LoadKey(string projectDirectory, string key)
        {
            var settings = Load(projectDirectory);
            return settings[key];
        }
    }
}

/* Example usage:
 * // Load settings from a directory
var settings = ProjectSettingsManager.Load(projectDirectory);

// Read a value
string? device = settings["Device"] as string;

// Set or update a value
settings["Device"] = "PIC32MZ2048EFH064";
settings["BaudRate"] = 115200;
settings["Debug"] = true;
settings["Options"] = new { Level = 3, Mode = "Advanced" };

// Save settings back to disk
ProjectSettingsManager.Save(projectDirectory, settings);

// Read back a complex object
var optionsElement = settings["Options"] as JsonElement?;
if (optionsElement.HasValue)
{
    var options = optionsElement.Value.Deserialize<YourOptionsType>();
    // or use optionsElement.Value.GetProperty("Level").GetInt32();
}
*/

/* Example ProjectSettings.json content after saving:
{
  "Device": "PIC32MZ2048EFH064",
  "BaudRate": 115200,
  "Debug": true,
  "Options": {
    "Level": 3,
    "Mode": "Advanced"
  }
}
*/