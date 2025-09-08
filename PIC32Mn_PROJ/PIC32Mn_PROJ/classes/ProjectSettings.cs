using System.Text.Json;
using System.Text.Json.Serialization;

namespace PIC32Mn_PROJ.classes
{
    public class ProjectSettings
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        public object? this[string key]
        {
            get
            {
                if (ExtensionData != null && ExtensionData.TryGetValue(key, out var value))
                {
                    if (value.ValueKind == JsonValueKind.String)
                        return value.GetString();
                    if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int intValue))
                        return intValue;
                    if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                        return value.GetBoolean();
                    return value.ToString();
                }
                return null;
            }
            set
            {
                if (ExtensionData == null)
                    ExtensionData = new Dictionary<string, JsonElement>();

                if (value is JsonElement je)
                {
                    ExtensionData[key] = je;
                }
                else
                {
                    // Serialize the value to JSON, then parse as JsonElement
                    string json = JsonSerializer.Serialize(value);
                    ExtensionData[key] = JsonDocument.Parse(json).RootElement.Clone();
                }
            }
        }
    }
}