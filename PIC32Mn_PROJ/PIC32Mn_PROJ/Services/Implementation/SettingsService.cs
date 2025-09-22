using System;
using System.Collections.Generic;
using System.IO;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class SettingsService : ISettingsService
    {
        public string? ProjectPath
        {
            get => AppSettings.Default.ProjectPath;
            set => AppSettings.Default.ProjectPath = value ?? string.Empty;
        }

        public string? MirrorProjectPath
        {
            get => AppSettings.Default.mirror_ProjectPath;
            set => AppSettings.Default.mirror_ProjectPath = value ?? string.Empty;
        }

        public void Save() => AppSettings.Default.Save();

        // Lightweight custom key/value storage for additional settings (e.g., Hotkeys)
        private static string StoreDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PIC32Mn_PROJ");
        private static string StorePath => Path.Combine(StoreDir, "user.settings");

        // Get a custom string value by key (or null if none)
        public string? GetString(string key)
        {
            try
            {
                if (!File.Exists(StorePath)) return null;
                foreach (var line in File.ReadAllLines(StorePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = line.Substring(0, idx).Trim();
                    if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;
                    return line[(idx + 1)..];
                }
            }
            catch { }
            return null;
        }

        // Set a custom string value by key and persist immediately
        public void SetString(string key, string value)
        {
            try
            {
                Directory.CreateDirectory(StoreDir);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(StorePath))
                {
                    foreach (var line in File.ReadAllLines(StorePath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                        var idx = line.IndexOf('=');
                        if (idx <= 0) continue;
                        var k = line.Substring(0, idx).Trim();
                        var v = line[(idx + 1)..];
                        dict[k] = v;
                    }
                }
                dict[key] = value ?? string.Empty;
                using var sw = new StreamWriter(StorePath, false);
                foreach (var kv in dict)
                {
                    sw.WriteLine($"{kv.Key}={kv.Value}");
                }
            }
            catch { }
        }
    }
}
