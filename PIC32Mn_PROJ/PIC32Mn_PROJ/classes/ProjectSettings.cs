using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace PIC32Mn_PROJ.classes
{
    public class ProjectSettings
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtensionData { get; set; }

        [JsonIgnore]
        public string? Device
        {
            get => this["Device"] as string;
            set => this["Device"] = value;
        }

        [JsonIgnore]
        public Dictionary<string, object>? General
        {
            get
            {
                if (this["General"] is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    return je.Deserialize<Dictionary<string, object>>();
                return this["General"] as Dictionary<string, object>;
            }
            set => this["General"] = value;
        }

        [JsonIgnore]
        public Dictionary<string, Dictionary<string, object>>? Features
        {
            get
            {
                if (this["Features"] is JsonElement je && je.ValueKind == JsonValueKind.Object)
                    return je.Deserialize<Dictionary<string, Dictionary<string, object>>>();
                return this["Features"] as Dictionary<string, Dictionary<string, object>>;
            }
            set => this["Features"] = value;
        }

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
                    if (value.ValueKind == JsonValueKind.Object)
                        return value;
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

