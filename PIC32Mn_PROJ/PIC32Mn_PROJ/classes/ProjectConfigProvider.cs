using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PIC32Mn_PROJ.classes
{
    public static class ProjectConfigProvider
    {
        public static Dictionary<string, string> LoadConfig(string? projectDirectory, string ttFilePath, string? device)
        {
            var defaults = ParseDefaultsFromTtFile(ttFilePath);

            if (string.IsNullOrEmpty(projectDirectory) || string.IsNullOrEmpty(device))
            {
                // No project assigned or device unknown, use defaults from .tt
                return defaults;
            }

            var settingsPath = Path.Combine(projectDirectory, ProjectSettingsManager.SettingsFileName_);
            Dictionary<string, Dictionary<string, string>> rootDict = null;
            string dev = device;
            bool updated = false;

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);

                try
                {
                    // Read as Dictionary<string, JsonElement>
                    var rawDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                    rootDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    if (rawDict != null)
                    {
                        foreach (var kvp in rawDict)
                        {
                            if (string.Equals(kvp.Key, "Device", StringComparison.OrdinalIgnoreCase))
                            {
                                if (kvp.Value.ValueKind == JsonValueKind.String)
                                    dev = kvp.Value.GetString();
                                continue;
                            }

                            // Only treat OBJECT nodes as device configs (string->string map). Ignore arrays like GPIOOverrides.
                            if (kvp.Value.ValueKind == JsonValueKind.Object)
                            {
                                var obj = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                foreach (var prop in kvp.Value.EnumerateObject())
                                {
                                    if (prop.Value.ValueKind == JsonValueKind.String)
                                    {
                                        obj[prop.Name] = prop.Value.GetString() ?? string.Empty;
                                    }
                                    else if (prop.Value.ValueKind == JsonValueKind.Number ||
                                             prop.Value.ValueKind == JsonValueKind.True ||
                                             prop.Value.ValueKind == JsonValueKind.False ||
                                             prop.Value.ValueKind == JsonValueKind.Null)
                                    {
                                        // Store as raw text to avoid exceptions; consumers expect strings
                                        obj[prop.Name] = prop.Value.GetRawText();
                                    }
                                    else
                                    {
                                        // Complex types are unexpected in device config; skip
                                    }
                                }
                                rootDict[kvp.Key] = obj;
                            }
                            // else: skip non-object entries (e.g., arrays like GPIOOverrides)
                        }
                    }
                }
                catch
                {
                    // Try to migrate from flat format (very old file layout). If it fails, ignore and fall back to defaults.
                    try
                    {
                        var flatDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        if (flatDict != null)
                        {
                            rootDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                            if (flatDict.ContainsKey("Device"))
                            {
                                dev = flatDict["Device"];
                                flatDict.Remove("Device");
                            }
                            rootDict[dev] = flatDict;
                            updated = true;
                        }
                    }
                    catch
                    {
                        // leave rootDict null; we'll recreate below
                    }
                }
            }
            else
            {
                rootDict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                updated = true;
            }

            rootDict ??= new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            // Get or create the config for this device
            if (!rootDict.TryGetValue(device, out var deviceConfig))
            {
                deviceConfig = new Dictionary<string, string>(defaults, StringComparer.OrdinalIgnoreCase);
                rootDict[device] = deviceConfig;
                updated = true;
            }
            else
            {
                foreach (var kvp in defaults)
                {
                    if (!deviceConfig.ContainsKey(kvp.Key))
                    {
                        deviceConfig[kvp.Key] = kvp.Value;
                        updated = true;
                    }
                }
                rootDict[device] = deviceConfig;
            }

            // When saving, use a Dictionary<string, object> to include the Device string and preserve other keys
            if (updated)
            {
                var saveDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                // Preserve any existing non-device keys from the current file
                if (File.Exists(settingsPath))
                {
                    try
                    {
                        var existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(settingsPath));
                        if (existing != null)
                        {
                            foreach (var kvp in existing)
                            {
                                if (string.Equals(kvp.Key, "Device", StringComparison.OrdinalIgnoreCase)) continue;
                                if (string.Equals(kvp.Key, device, StringComparison.OrdinalIgnoreCase)) continue;
                                // Keep as-is (JsonElement retains original structure)
                                saveDict[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                    catch { /* ignore */ }
                }

                saveDict["Device"] = dev;
                foreach (var kvp in rootDict)
                    saveDict[kvp.Key] = kvp.Value;

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(saveDict, new JsonSerializerOptions { WriteIndented = true }));
            }

            return deviceConfig;
        }

        private static Dictionary<string, string> ParseDefaultsFromTtFile(string ttFilePath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string content = File.ReadAllText(ttFilePath);
            var matches = Regex.Matches(content, @"\[\s*""(?<key>[^""]+)""\s*\]\s*=\s*""(?<value>[^""]*)""");
            foreach (Match match in matches)
            {
                string key = match.Groups["key"].Value;
                string value = match.Groups["value"].Value;
                result[key] = value;
            }
            return result;
        }

        public static void SaveDeviceConfig(string projectDir, string device, Dictionary<string, string> config)
        {
            string settingsPath = Path.Combine(projectDir, "ProjectSettings.json");

            var saveDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Load existing settings to preserve other keys (e.g., GPIOOverrides)
            if (File.Exists(settingsPath))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(settingsPath));
                    if (existing != null)
                    {
                        foreach (var kvp in existing)
                        {
                            if (string.Equals(kvp.Key, "Device", StringComparison.OrdinalIgnoreCase)) continue;
                            if (string.Equals(kvp.Key, device, StringComparison.OrdinalIgnoreCase)) continue;
                            saveDict[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch { /* ignore */ }
            }

            saveDict["Device"] = device;
            saveDict[device] = config;

            File.WriteAllText(settingsPath, JsonSerializer.Serialize(saveDict, new JsonSerializerOptions { WriteIndented = true }));
        }           
    }
}