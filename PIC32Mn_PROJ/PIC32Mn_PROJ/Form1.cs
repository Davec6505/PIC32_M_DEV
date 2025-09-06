using Microsoft.VisualBasic.ApplicationServices;
using PIC32Mn_PROJ.classes;
using System.Configuration;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {
        ConfigLoader configLoader = new();
        string filePath = string.Empty;

        string configPath = string.Empty;   //path to .atdf
        string configOutput = string.Empty; //path to .json

        string jsonFilePath = string.Empty; //path to pin .data
        string outputPath = string.Empty;   //path to pin .json

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            filePath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\edc\\PIC32MZ2048EFH064.PIC"; // Replace with your actual XML file path
            configPath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\atdf\\PIC32MZ2048EFH064.atdf";
            configOutput = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\ConfigValues.json";

            jsonFilePath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\pic32mz_device.json";
            outputPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\PinMappings.txt"; // Output file

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
                //  MessageBox.Show("XML Converted sucessfully!");

                jsonMappings(filePath, jsonFilePath);

                load_pinForm(jsonFilePath);

                if (!File.Exists(configOutput))
                {
                    File.Create(configOutput).Close();
                }

                ConfigLoader.LoadConfig(configPath, configOutput, "FUSECONFIG");

            }
        }

        #region  menu
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion menu

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

        


        private void jsonMappings(string xmlPath, string jsonPath)
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


        #region config form load
        private void LoadConfig(string config, string output)
        {
            var doc = XDocument.Load(config);

            var modules = doc.Descendants("module");

            var registers = modules
                .SelectMany(m => m.Descendants("register"))
                .Where(reg => reg.Attribute("name") != null)
                .ToDictionary(
                    reg => reg.Attribute("name")!.Value,
                    reg => reg.Elements("bitfield")
                        .Where(bf => bf.Attribute("name") != null && bf.Attribute("values") != null)
                        .ToDictionary(
                            bf => bf.Attribute("name")!.Value,
                            bf => {
                                var valueGroupName = bf.Attribute("values")!.Value;
                                var valueGroup = modules
                                    .SelectMany(m => m.Elements("value-group"))
                                    .FirstOrDefault(vg => vg.Attribute("name")?.Value == valueGroupName);

                                if (valueGroup == null)
                                    return new Dictionary<string, string>();

                                return valueGroup.Elements("value")
                                    .Where(v => v.Attribute("name") != null && v.Attribute("value") != null)
                                    .GroupBy(v => v.Attribute("name")!.Value)
                                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                                    .ToDictionary(
                                        g => g.Key!,
                                        g => g.First().Attribute("value")?.Value ?? "0x0"
                                    );
                            }
                        )
                );

            var json = JsonSerializer.Serialize(registers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(output, json);

            Console.WriteLine($"Saved to {output}");
        }

        #endregion config form load

        #region  pins form load

        private void load_pinForm(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            JsonDocument doc = JsonDocument.Parse(json);

            JsonElement pinsElement = doc.RootElement.GetProperty("pins");

            foreach (JsonProperty pinEntry in pinsElement.EnumerateObject())
            {
                JsonElement pinData = pinEntry.Value;

                string pinName = pinData.GetProperty("PIN").GetString();
                string pmd = pinData.TryGetProperty("PMD", out var pmdProp) ? pmdProp.GetString() : null;

                var directionFunctions = new Dictionary<string, List<string>>();

                if (pinData.TryGetProperty("PinFunctions", out var pinFunctions) &&
                    pinFunctions.TryGetProperty("direction", out var directionElement))
                {
                    foreach (JsonProperty dir in directionElement.EnumerateObject())
                    {
                        directionFunctions[dir.Name] = dir.Value.EnumerateArray()
                            .Select(f => f.GetString())
                            .Where(f => !string.IsNullOrEmpty(f))
                            .ToList();
                    }
                }

                BuildPinRow(pinName,pinData);// directionFunctions);
            }

        }

        private void BuildPinRow(string pinKey, JsonElement pinData)
        {
            var rowPanel = new Panel
            {
                Width = 450,//flowPanelPins.Width - 25,
                Height = 35,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = pinKey // for later reference
            };

            var enableCheck = new CheckBox {Text = "En", Width = 50, Checked = false , Anchor = AnchorStyles.Left };
            var pinNameBox = new TextBox { Width = 100, Text = pinData.GetProperty("PIN").GetString() };
            var directionToggle = new CheckBox { Text = "Out", Width = 60, Checked = false , Anchor = AnchorStyles.Left };
            var functionCombo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList , Anchor = AnchorStyles.Left | AnchorStyles.Right };

            var directionFunctions = new Dictionary<string, List<string>>();
            if (pinData.TryGetProperty("PinFunctions", out var pinFunctions) &&
                pinFunctions.TryGetProperty("direction", out var directionElement))
            {
                foreach (JsonProperty dir in directionElement.EnumerateObject())
                {
                    directionFunctions[dir.Name] = dir.Value.EnumerateArray().Select(f => f.GetString()).Where(f => !string.IsNullOrEmpty(f)).ToList();
                }
            }

            // Initial population
            if (directionFunctions.TryGetValue("in", out var inFunctions))
                functionCombo.Items.AddRange(inFunctions.ToArray());

            directionToggle.CheckedChanged += (s, e) =>
            {
                string dir = directionToggle.Checked ? "out" : "in";
                functionCombo.Items.Clear();
                if (directionFunctions.TryGetValue(dir, out var functions))
                    functionCombo.Items.AddRange(functions.ToArray());
            };

            // Positioning
            enableCheck.Location = new Point(5, 5);
            pinNameBox.Location = new Point(60, 5);
            directionToggle.Location = new Point(170, 5);
            functionCombo.Location = new Point(240, 5);

            rowPanel.Controls.Add(enableCheck);
            rowPanel.Controls.Add(pinNameBox);
            rowPanel.Controls.Add(directionToggle);
            rowPanel.Controls.Add(functionCombo);

            flowPanelPins.Controls.Add(rowPanel);
        }
        #endregion pins form load

    }
}