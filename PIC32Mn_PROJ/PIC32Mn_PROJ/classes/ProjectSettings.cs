using System.Text.Json.Serialization;

namespace PIC32Mn_PROJ.classes
{
    public class ProjectSettings
    {
        [JsonExtensionData]
        public Dictionary<string, object?> Settings { get; set; } = new();

        [JsonIgnore]
        public string? Device
        {
            get => Settings.TryGetValue("Device", out var value) ? value?.ToString() : null;
            set => Settings["Device"] = value;
        }

        // Indexer for dynamic access
        public object? this[string key]
        {
            get => Settings.TryGetValue(key, out var value) ? value : null;
            set => Settings[key] = value;
        }
    }
}