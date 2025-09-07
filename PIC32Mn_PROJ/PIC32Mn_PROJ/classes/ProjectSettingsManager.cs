using System.IO;
using System.Text.Json;

namespace PIC32Mn_PROJ.classes
{
    public static class ProjectSettingsManager
    {
        private const string SettingsFileName = "ProjectSettings.json";

        // Pass the project directory path to these methods
        public static ProjectSettings Load(string projectDirectory)
        {
            var settingsPath = Path.Combine(projectDirectory, SettingsFileName);
            if (!File.Exists(settingsPath))
                return new ProjectSettings();

            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<ProjectSettings>(json) ?? new ProjectSettings();
        }

        public static void Save(string projectDirectory, ProjectSettings settings)
        {
            var settingsPath = Path.Combine(projectDirectory, SettingsFileName);
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