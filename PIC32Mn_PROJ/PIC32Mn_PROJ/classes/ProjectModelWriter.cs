using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PIC32Mn_PROJ.classes
{
    // Reusable helper to build and persist a device-rooted JSON model used by generators (GPIO, timers, I2C, ...)
    public static class ProjectModelWriter
    {
        public static void SaveProjectJson(string projectDir, string device, Dictionary<string, string> config, List<GpioOverride> overrides)
        {
            if (string.IsNullOrWhiteSpace(projectDir) || string.IsNullOrWhiteSpace(device)) return;

            var model = BuildProjectDeviceNode(config ?? new(), overrides ?? new());

            // Root object: { "<device>": { config: {...}, gpio: {...} } }
            var root = new Dictionary<string, ProjectDeviceNode>(StringComparer.OrdinalIgnoreCase)
            {
                [device] = model
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var outPath = Path.Combine(projectDir, "Project.json");
            var json = JsonSerializer.Serialize(root, options);
            File.WriteAllText(outPath, json);
        }

        public static ProjectDeviceNode? LoadProjectJson(string projectDir, string device)
        {
            try
            {
                var path = Path.Combine(projectDir ?? string.Empty, "Project.json");
                if (!File.Exists(path)) return null;
                using var fs = File.OpenRead(path);
                var root = JsonSerializer.Deserialize<Dictionary<string, ProjectDeviceNode>>(fs);
                if (root == null) return null;
                if (root.TryGetValue(device ?? string.Empty, out var node)) return node;
                // if device key not found, return first entry
                foreach (var kv in root)
                    return kv.Value;
            }
            catch { }
            return null;
        }

        public static ProjectDeviceNode BuildProjectDeviceNode(Dictionary<string, string> config, List<GpioOverride> overrides)
        {
            return new ProjectDeviceNode
            {
                config = new Dictionary<string, string>(config, StringComparer.OrdinalIgnoreCase),
                gpio = BuildGpioModel(overrides ?? new())
            };
        }

        // Compute GPIO masks, channels and pins in one place so it can be reused by other emitters
        public static GpioModel BuildGpioModel(List<GpioOverride> overrides)
        {
            var valid = (overrides ?? new())
                .Where(o => o != null && !string.IsNullOrEmpty(o.PortChannel) && o.PortPin >= 0 && o.Enabled)
                .ToList();

            var model = new GpioModel
            {
                generate = valid.Count > 0,
                channels = new List<string>(),
                masks = new Dictionary<string, ChannelMasks>(StringComparer.OrdinalIgnoreCase),
                pins = new List<GpioPin>()
            };

            // Group by channel
            var groups = valid
                .GroupBy(o => o.PortChannel)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var g in groups)
            {
                model.channels.Add(g.Key);

                int tris = 0;
                int ansel = 0;
                int lat = 0; // default 0 latch

                foreach (var o in g)
                {
                    if (o.PortPin >= 0 && o.PortPin <= 15)
                    {
                        int bit = 1 << o.PortPin;
                        ansel |= bit;           // digital enable
                        if (o.Output) tris |= bit; // clear TRIS for outputs
                    }

                    model.pins.Add(new GpioPin
                    {
                        name = o.PinName ?? string.Empty,
                        channel = g.Key,
                        pin = o.PortPin,
                        enabled = o.Enabled,
                        output = o.Output,
                        function = string.IsNullOrWhiteSpace(o.Function) ? "GPIO" : o.Function
                    });
                }

                model.masks[g.Key] = new ChannelMasks
                {
                    TRIS = tris.ToString("x"),
                    ANSEL = ansel.ToString("x"),
                    LAT = lat.ToString("x"),
                    CN_USED = g.Any(o => !o.Output)
                };
            }

            return model;
        }
    }

    // JSON DTOs (lowercase to match desired JSON keys)
    public class ProjectDeviceNode
    {
        public Dictionary<string, string> config { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public GpioModel gpio { get; set; } = new();
    }

    public class GpioModel
    {
        public bool generate { get; set; }
        public List<string> channels { get; set; } = new();
        public Dictionary<string, ChannelMasks> masks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<GpioPin> pins { get; set; } = new();
    }

    public class ChannelMasks
    {
        public string TRIS { get; set; } = "0";   // hex string (without 0x)
        public string ANSEL { get; set; } = "0";  // hex string (without 0x)
        public string LAT { get; set; } = "0";    // hex string (without 0x)
        public bool CN_USED { get; set; }
    }

    public class GpioPin
    {
        public string name { get; set; } = string.Empty;      // alias or UI text (e.g., RE5)
        public string channel { get; set; } = string.Empty;   // e.g., "E"
        public int pin { get; set; }
        public bool enabled { get; set; }
        public bool output { get; set; }
        public string function { get; set; } = "GPIO";        // UI function or GPIO
    }
}
