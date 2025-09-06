using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace PIC32Mn_PROJ.classes
{
    public class ConfigLoader
    {
        public static void LoadConfig(string configPath, string outputPath, string targetModule)
        {
            var doc = XDocument.Load(configPath);

            // Find the specific module by name
            var fuseModule = doc.Descendants("module")
                .FirstOrDefault(m => m.Attribute("name")?.Value == targetModule);

            if (fuseModule == null)
            {
                Console.WriteLine($"Module '{targetModule}' not found.");
                return;
            }

            // Build value-group lookup scoped to this module
            var valueGroups = fuseModule.Elements("value-group")
                .Where(vg => vg.Attribute("name") != null)
                .ToDictionary(
                    vg => vg.Attribute("name")!.Value,
                    vg => vg.Elements("value")
                            .Select(v => v.Attribute("name")?.Value)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .Distinct()
                            .ToList()
                );

            var registerGroup = fuseModule.Element("register-group");
            if (registerGroup == null)
            {
                Console.WriteLine($"No register-group found in module '{targetModule}'.");
                return;
            }

            var fuseConfig = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (var register in registerGroup.Elements("register").Where(r => r.Attribute("name") != null))
            {
                var registerName = register.Attribute("name")!.Value;
                var fuseMap = new Dictionary<string, List<string>>();

                foreach (var bitfield in register.Elements("bitfield").Where(bf => bf.Attribute("name") != null))
                {
                    var fuseName = bitfield.Attribute("name")!.Value;
                    var valuesGroupName = bitfield.Attribute("values")?.Value;

                    if (!string.IsNullOrEmpty(valuesGroupName) && valueGroups.TryGetValue(valuesGroupName, out var options))
                    {
                        fuseMap[fuseName] = options;
                    }
                    else
                    {
                        fuseMap[fuseName] = new List<string>(); // Empty array if no values
                    }
                }

                if (fuseMap.Count > 0)
                {
                    fuseConfig[registerName] = fuseMap;
                }
            }

            var output = new Dictionary<string, object>
            {
                [targetModule] = fuseConfig
            };

            var outputJson = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, outputJson);

            Console.WriteLine($"Saved nested config for module '{targetModule}' to {outputPath}");
        }

    }
}
