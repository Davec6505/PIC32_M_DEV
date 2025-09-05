using Microsoft.VisualBasic.ApplicationServices;
using System.Configuration;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {

        string filePath = string.Empty;
        string jsonFilePath = string.Empty;
        string outputPath = string.Empty;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            filePath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\edc\\PIC32MZ2048EFH064.PIC"; // Replace with your actual XML file path
            jsonFilePath = "C:\\Users\\Automation\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\pic32mz_device.json";
            outputPath = "C:\\Users\\Automation\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\PinMappings.txt"; // Output file
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Can't find XML for device.");
            }
            else
            {
                if (!File.Exists(outputPath))
                {
                    File.Create(outputPath).Close();
                }
                LoadPins(filePath, outputPath);
                MessageBox.Show("XML Converted sucessfully!");

                jasonMappings(filePath, jsonFilePath);
            }

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        #region data extraction from xml pic32mz
        private void LoadPins(string filePath, string outpath)
        {


            XNamespace edc = "http://crownking/edc"; // Replace with actual namespace URI
            XDocument doc = XDocument.Load(filePath);

            var pins = doc.Descendants(edc + "Pin")
                          .Select((pin, index) => new
                          {
                              Index = index + 1,
                              VirtualPins = pin.Elements(edc + "VirtualPin")
                                               .Select(vp => new
                                               {
                                                   Name = vp.Attribute(edc + "name")?.Value,
                                                   Group = vp.Attribute(edc + "ppsgroup")?.Value,
                                                   Val = vp.Attribute(edc + "ppsval")?.Value
                                               }).ToList()
                          });


            // Build remappable function map: ppsgroup → { direction → [functions] }
            var remappableMap = doc.Descendants(edc + "RemappablePin")
                .SelectMany(rp =>
                {
                    string direction = rp.Attribute(edc + "direction")?.Value ?? "unknown";
                    return rp.Elements(edc + "VirtualPin")
                             .Where(vp => vp.Attribute(edc + "ppsgroup") != null)
                             .Select(vp => new
                             {
                                 Group = vp.Attribute(edc + "ppsgroup")?.Value,
                                 Name = vp.Attribute(edc + "name")?.Value,
                                 Direction = direction
                             });
                })
                .GroupBy(x => x.Group)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.Direction)
                          .ToDictionary(
                              d => d.Key,
                              d => d.Select(x => x.Name).Distinct().ToList()
                          )
                );

            using (StreamWriter writer = new StreamWriter(outputPath))
            {
                int pinIndex = 1;

                foreach (var pin in doc.Descendants(edc + "Pin"))
                {
                    var virtualPins = pin.Elements(edc + "VirtualPin").ToList();

                    string pinName = virtualPins.FirstOrDefault()?.Attribute(edc + "name")?.Value;
                    string pmd = virtualPins.FirstOrDefault(vp => vp.Attribute(edc + "name")?.Value?.StartsWith("PMD") == true)
                                 ?.Attribute(edc + "name")?.Value;

                    var ppsPin = virtualPins.FirstOrDefault(vp =>
                        vp.Attribute(edc + "ppsgroup") != null &&
                        vp.Attribute(edc + "ppsval") != null);

                    string ppsName = ppsPin?.Attribute(edc + "name")?.Value;
                    string ppsGroup = ppsPin?.Attribute(edc + "ppsgroup")?.Value;
                    string ppsVal = ppsPin?.Attribute(edc + "ppsval")?.Value;

                    var staticFunctions = virtualPins
                        .Where(vp => vp.Attribute(edc + "ppsgroup") == null &&
                                     vp.Attribute(edc + "ppsval") == null &&
                                     !(vp.Attribute(edc + "name")?.Value?.StartsWith("PMD") ?? false) &&
                                     vp != virtualPins.First())
                        .Select(vp => vp.Attribute(edc + "name")?.Value)
                        .ToList();

                    // Merge remappable functions
                    Dictionary<string, List<string>> directionalFunctions = new();

                    if (ppsGroup != null && remappableMap.ContainsKey(ppsGroup))
                    {
                        foreach (var dir in remappableMap[ppsGroup])
                        {
                            if (!directionalFunctions.ContainsKey(dir.Key))
                                directionalFunctions[dir.Key] = new List<string>();

                            directionalFunctions[dir.Key].AddRange(dir.Value);
                        }
                    }

                    if (staticFunctions.Any())
                    {
                        if (!directionalFunctions.ContainsKey("in"))
                            directionalFunctions["in"] = new List<string>();

                        directionalFunctions["in"].AddRange(staticFunctions);
                    }

                    foreach (var key in directionalFunctions.Keys.ToList())
                    {
                        directionalFunctions[key] = directionalFunctions[key].Distinct().ToList();
                    }

                    // Write to file
                    writer.WriteLine($"PIN{pinIndex}:");
                    writer.WriteLine($"  Physical Pin: {pinName}");
                    if (!string.IsNullOrEmpty(pmd))
                        writer.WriteLine($"  PMD: {pmd}");

                    if (ppsPin != null)
                    {
                        writer.WriteLine($"  PPS Mapping:");
                        writer.WriteLine($"    Name: {ppsName}");
                        writer.WriteLine($"    Group: {ppsGroup}");
                        writer.WriteLine($"    Value: {ppsVal}");
                    }

                    writer.WriteLine($"  Pin Functions:");
                    foreach (var dir in directionalFunctions)
                    {
                        writer.WriteLine($"    Direction: {dir.Key}");
                        foreach (var func in dir.Value)
                        {
                            writer.WriteLine($"      - {func}");
                        }
                    }

                    writer.WriteLine(); // spacing
                    pinIndex++;
                }

                Console.WriteLine($"✅ Pin mappings written to {outputPath}");
            }


        }

        


        private void jasonMappings(string xmlPath, string jsonPath)
        {

            XNamespace edc = "http://crownking/edc";
            XDocument doc = XDocument.Load(xmlPath);


            // Build remappable function map: ppsgroup → { direction → [functions] }
            var remappableMap = doc.Descendants(edc + "RemappablePin")
                .SelectMany(rp =>
                {
                    string direction = rp.Attribute(edc + "direction")?.Value ?? "unknown";
                    return rp.Elements(edc + "VirtualPin")
                             .Where(vp => vp.Attribute(edc + "ppsgroup") != null)
                             .Select(vp => new
                             {
                                 Group = vp.Attribute(edc + "ppsgroup")?.Value,
                                 Name = vp.Attribute(edc + "name")?.Value,
                                 Direction = direction
                             });
                })
                .GroupBy(x => x.Group)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.Direction)
                          .ToDictionary(
                              d => d.Key,
                              d => d.Select(x => x.Name).Distinct().ToList()
                          )
                );

            // Build pin mappings with sequential PINx
            var pinDict = new Dictionary<string, object>();
            var pins = doc.Descendants(edc + "Pin").ToList();

            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                var virtualPins = pin.Elements(edc + "VirtualPin").ToList();

                string pinName = virtualPins.FirstOrDefault()?.Attribute(edc + "name")?.Value;
                string pmd = virtualPins.FirstOrDefault(vp => vp.Attribute(edc + "name")?.Value?.StartsWith("PMD") == true)
                             ?.Attribute(edc + "name")?.Value;

                var ppsPin = virtualPins
                    .FirstOrDefault(vp => vp.Attribute(edc + "ppsgroup") != null && vp.Attribute(edc + "ppsval") != null);

                var pps = ppsPin != null ? new
                {
                    name = ppsPin.Attribute(edc + "name")?.Value,
                    ppsgroup = int.Parse(ppsPin.Attribute(edc + "ppsgroup")?.Value ?? "0"),
                    ppsval = int.Parse(ppsPin.Attribute(edc + "ppsval")?.Value ?? "0")
                } : null;

                var staticFunctions = virtualPins
                    .Where(vp => vp.Attribute(edc + "ppsgroup") == null &&
                                 vp.Attribute(edc + "ppsval") == null &&
                                 !(vp.Attribute(edc + "name")?.Value?.StartsWith("PMD") ?? false) &&
                                 vp != virtualPins.First())
                    .Select(vp => vp.Attribute(edc + "name")?.Value)
                    .ToList();

                Dictionary<string, List<string>> directionalFunctions = new();

                if (pps != null && remappableMap.ContainsKey(pps.ppsgroup.ToString()))
                {
                    foreach (var dir in remappableMap[pps.ppsgroup.ToString()])
                    {
                        if (!directionalFunctions.ContainsKey(dir.Key))
                            directionalFunctions[dir.Key] = new List<string>();

                        directionalFunctions[dir.Key].AddRange(dir.Value);
                    }
                }

                if (staticFunctions.Any())
                {
                    if (!directionalFunctions.ContainsKey("in"))
                        directionalFunctions["in"] = new List<string>();

                    directionalFunctions["in"].AddRange(staticFunctions);
                }

                foreach (var key in directionalFunctions.Keys.ToList())
                {
                    directionalFunctions[key] = directionalFunctions[key].Distinct().ToList();
                }

                var pinObject = new
                {
                    PIN = pinName,
                    PMD = pmd,
                    PPS = pps,
                    PinFunctions = new { direction = directionalFunctions }
                };

                pinDict[$"PIN{i + 1}"] = pinObject;
            }

            var finalJson = new Dictionary<string, object> { ["pins"] = pinDict };
            string json = JsonSerializer.Serialize(finalJson, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, json);

            Console.WriteLine($"✅ Pin mapping written to {jsonPath}");
        }
        #endregion 

    }
}