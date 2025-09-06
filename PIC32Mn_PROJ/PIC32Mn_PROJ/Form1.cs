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
        Modules mods;
        pins pins;
        string picPath = string.Empty;

        string adtfPath = string.Empty;   //path to .atdf
        string configOutput = string.Empty; //path to .json
        string adchsOutput = string.Empty; //path to adchs .json                                          
        string gpioPath = string.Empty; //path to pin .data
        string outputPath = string.Empty;   //path to pin .json

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            picPath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\edc\\PIC32MZ2048EFH064.PIC"; // Replace with your actual XML file path
            adtfPath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\atdf\\PIC32MZ2048EFH064.atdf";

            configOutput = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\CONFIGValues.json";
            adchsOutput = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\ADCHSValues.json";

            gpioPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\pic32mz_device.json";
            outputPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\PinMappings.txt"; // Output file
            string opath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\dependancies\\modules\\";
            mods = new Modules(adtfPath,opath);
            pins = new pins(picPath, gpioPath);

            if (!File.Exists(picPath))
            {
                MessageBox.Show("Can't find XML for device.");
            }
            else
            {
                if (!File.Exists(outputPath))
                {
                    File.Create(outputPath).Close();
                }
                pins.LoadPinsfromXML_SavetoTxt(picPath, outputPath);
                //  MessageBox.Show("XML Converted sucessfully!");

                pins.LoadPinsfromXML_SavetoJson(picPath, gpioPath);

                load_pinForm(gpioPath);

                if (!File.Exists(configOutput))
                {
                    File.Create(configOutput).Close();
                }
            }
            //EXAMPLE OF MODULES
            //"FUSECONFIG"  | "ADCHS" | "CAN" | "CFG" | "CMP" | "CORE" | "CRU" | "RCON" | "DMA" | "DMT" | "ETH" | "GPIO" | "I2C" | "ICAP" |
            //"INT" | "JTAG" | "NVM" | "OCMP" | "PCACHE" | "PMP" | "RNG" | "RPIN" | "RPOUT" | "RTCC" | "SB" |  "SPI" | "SQI" | "TMR1" | "TMR" |
            //"UART" | "USB" | "USBCLKRST" | "WDT"

            // If 'mods' is a Dictionary<string, ModuleType>, entry is KeyValuePair<string, ModuleType>
            // If 'mods' is a collection of ModuleType, you need to adjust accordingly.
            // Try to cast entry to KeyValuePair<string, ModuleType>:
            foreach (var entry in mods)
            {
                if (entry is KeyValuePair<string, (string Path1, string Path2)> kvp)
                {
                    ConfigLoader.Call_LoadConfig(kvp.Value.Path1, kvp.Value.Path2, kvp.Key);
                }
                else
                {
                    Debug.WriteLine("module is null or of unexpected type.");
                }
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