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
                    rootDict = new Dictionary<string, Dictionary<string, string>>();
                    if (rawDict != null)
                    {
                        foreach (var kvp in rawDict)
                        {
                            if (kvp.Key == "Device")
                            {
                                dev = kvp.Value.GetString();
                            }
                            else
                            {
                                var dict = kvp.Value.Deserialize<Dictionary<string, string>>();
                                if (dict != null)
                                    rootDict[kvp.Key] = dict;
                            }
                        }
                    }
                }
                catch
                {
                    // Try to migrate from flat format
                    var flatDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (flatDict != null)
                    {
                        rootDict = new Dictionary<string, Dictionary<string, string>>();
                        if (flatDict.ContainsKey("Device"))
                        {
                            dev = flatDict["Device"];
                            flatDict.Remove("Device");
                        }
                        rootDict[dev] = flatDict;
                        updated = true;
                    }
                }
            }
            else
            {
                rootDict = new Dictionary<string, Dictionary<string, string>>();
                updated = true;
            }

            // Get or create the config for this device
            if (!rootDict.TryGetValue(device, out var deviceConfig))
            {
                deviceConfig = new Dictionary<string, string>(defaults);
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

            // When saving, use a Dictionary<string, object> to include the Device string
            if (updated)
            {
                var saveDict = new Dictionary<string, object>();
                saveDict["Device"] = dev;
                foreach (var kvp in rootDict)
                    saveDict[kvp.Key] = kvp.Value;
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(saveDict, new JsonSerializerOptions { WriteIndented = true }));
            }

            return deviceConfig;
        }

        private static Dictionary<string, string> ParseDefaultsFromTtFile(string ttFilePath)
        {
            var result = new Dictionary<string, string>();
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
            var saveDict = new Dictionary<string, object>
            {
                ["Device"] = device,
                [device] = config
            };
            File.WriteAllText(settingsPath, JsonSerializer.Serialize(saveDict, new JsonSerializerOptions { WriteIndented = true }));
        }           
    }
}