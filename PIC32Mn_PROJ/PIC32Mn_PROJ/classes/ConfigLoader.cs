using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace PIC32Mn_PROJ.classes
{
    public class ConfigLoader
    {

        public static void Call_LoadConfig(string configPath, string outputPath, string targetModule)
        {
            if (targetModule.Equals("FUSECONFIG", StringComparison.OrdinalIgnoreCase))
            {
                ConfigLoader.Load_Configwith_Valuegroups_Details(configPath, outputPath, targetModule);
            }
            else
            {
                ConfigLoader.Load_ConfigWith_Bitfield_Details(configPath, outputPath, targetModule);
            }

        }

        private static void Load_Configwith_Valuegroups_Details(string configPath, string outputPath, string targetModule)
        {
            var doc = XDocument.Load(configPath);

            // Find the specific module by name
            var fuseModule = doc.Descendants("module")
                .FirstOrDefault(m =>
                    string.Equals(
                        m.Attribute("name")?.Value.Trim(),
                        targetModule.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (fuseModule == null)
            {
                Debug.WriteLine($"Module '{targetModule}' not found.");
                return;
            }

            Debug.WriteLine($"Module name: {fuseModule.Attribute("name")?.Value}, id: {fuseModule.Attribute("id")?.Value}");
            foreach (var el in fuseModule.Elements())
            {
                Debug.WriteLine($"Child element: {el.Name}");
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

            // Try to find register-group directly under the module
            XElement registerGroup = fuseModule.Element("register-group");

            // If not found, look for an <instance> and resolve the referenced register-group by name
            if (registerGroup == null)
            {
                var instance = fuseModule.Element("instance");
                string registerGroupName = instance?.Attribute("name")?.Value; // or use another attribute if needed

                if (!string.IsNullOrEmpty(registerGroupName))
                {
                    // Look for a register-group with this name anywhere in the document
                    registerGroup = doc.Descendants("register-group")
                        .FirstOrDefault(rg => (string)rg.Attribute("name") == registerGroupName);
                }
            }

            if (registerGroup == null)
            {
                Debug.WriteLine($"No register-group found in module '{targetModule}'.");
                return;
            }

            foreach (var reg in registerGroup.Descendants("register"))
            {
                Debug.WriteLine($"Found register: {reg.Attribute("name")?.Value}");
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
                        fuseMap[fuseName] = new List<string>();
                    }
                }

                // Only add the register if at least one bitfield has values
                if (fuseMap.Values.Any(list => list.Count > 0))
                {
                    fuseConfig[registerName] = fuseMap;
                }
                Debug.WriteLine($"Register: {registerName}, Bitfields: {string.Join(", ", fuseMap.Select(kvp => $"{kvp.Key} ({kvp.Value.Count})"))}");
            }

            var output = new Dictionary<string, object>
            {
                [targetModule] = fuseConfig
            };

            var outputJson = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, outputJson);

            Debug.WriteLine($"Saved nested config for module '{targetModule}' to {outputPath}");
        }
   
        private static void Load_ConfigWith_Bitfield_Details(string configPath, string outputPath, string targetModule)
        {
            var doc = XDocument.Load(configPath);

            // Find the <module> under <modules> (not <peripherals>)
            var fuseModule = doc.Descendants("modules")
                .Elements("module")
                .FirstOrDefault(m =>
                    string.Equals(
                        m.Attribute("name")?.Value.Trim(),
                        targetModule.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (fuseModule == null)
            {
                Debug.WriteLine($"Module '{targetModule}' not found.");
                return;
            }

            // Build value-group lookup for this module
            var valueGroups = fuseModule.Elements("value-group")
                .Where(vg => vg.Attribute("name") != null)
                .ToDictionary(
                    vg => vg.Attribute("name")!.Value,
                    vg => vg.Elements("value")
                .Select(v => new {
                    caption = v.Attribute("caption")?.Value,
                    name = v.Attribute("name")?.Value,
                    value = v.Attribute("value")?.Value
                })
                .ToList()
                );

            // Find the register-group whose name matches the module name
            var registerGroup = fuseModule.Elements("register-group")
                .FirstOrDefault(rg => string.Equals(
                    rg.Attribute("name")?.Value.Trim(),
                    targetModule.Trim(),
                    StringComparison.OrdinalIgnoreCase));

                    if (registerGroup == null)
                    {
                        Debug.WriteLine($"No register-group found in module '{targetModule}'.");
                        return;
                    }

            var fuseConfig = new Dictionary<string, List<object>>();

            foreach (var register in registerGroup.Elements("register").Where(r => r.Attribute("name") != null))
            {
                var registerName = register.Attribute("name")!.Value;
                var bitfieldList = new List<object>();

                foreach (var bitfield in register.Elements("bitfield").Where(bf => bf.Attribute("name") != null))
                {
                    var fuseName = bitfield.Attribute("name")!.Value;
                    var mask = bitfield.Attribute("mask")?.Value;
                    var caption = bitfield.Attribute("caption")?.Value;
                    var valuesGroupName = bitfield.Attribute("values")?.Value;

                    List<object> values = new();
                    if (!string.IsNullOrEmpty(valuesGroupName) && valueGroups.TryGetValue(valuesGroupName, out var options))
                        values = options.Cast<object>().ToList();

                    bitfieldList.Add(new
                    {
                        name = fuseName,
                        mask = mask,
                        caption = caption,
                        values = values
                    });
                }

                if (bitfieldList.Count > 0)
                {
                    fuseConfig[registerName] = bitfieldList;
                }
            }

            var output = new Dictionary<string, object>
            {
                [targetModule] = fuseConfig
            };

            var outputJson = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, outputJson);

            Debug.WriteLine($"Saved detailed config for module '{targetModule}' to {outputPath}");
        }
  
    
    
    
    
    }
}
