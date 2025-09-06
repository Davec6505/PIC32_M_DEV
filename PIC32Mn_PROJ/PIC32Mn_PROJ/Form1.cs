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

        string rootPath = string.Empty;
        string packsPath = string.Empty;
        string picPath = string.Empty;

        string adtfPath = string.Empty;   //path to .atdf
        string configOutput = string.Empty; //path to .json
        string adchsOutput = string.Empty; //path to adchs .json                                          
        string gpioPath = string.Empty; //path to pin .data
        string outputPath = string.Empty;   //path to pin .json
        string opath = string.Empty;

        public string device { get; set; }

        public Form1()
        {
            InitializeComponent();

            // path of Form1 C:\Users\davec\GIT\PIC32_M_DEV\PIC32Mn_PROJ\PIC32Mn_PROJ\XML\
            rootPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\";
            //packsPath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\";
            packsPath = $"{rootPath}XML\\";
            
            //paths specific to this application, will have to sort this out
            picPath         = $"{packsPath}edc\\PIC32MZ2048EFH064.PIC"; // Replace with your actual XML file path
            adtfPath        = $"{packsPath}atdf\\PIC32MZ2048EFH064.atdf";

            configOutput    = $"{rootPath}dependancies\\CONFIGValues.json";
            adchsOutput     = $"{rootPath}dependancies\\ADCHSValues.json";

            gpioPath        = $"{rootPath}dependancies\\gpio\\pic32mz_device.json";
            outputPath      = $"{rootPath}dependancies\\gpio\\PinMappings.txt"; // Output file
            opath           = $"{rootPath}dependancies\\modules\\";

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mods = new Modules(adtfPath, opath);
            pins = new pins(picPath, gpioPath);

            // extract pins from xml and save to json file.
            pins.LoadPins();

            // Now you can use the 'mods' object to access module data
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

            //condition GPIO tab with json file
            load_pinForm(gpioPath);

        }

        #region  menu items
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void deviceToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        #endregion menu


        #region  gpio tab controls

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

                BuildPinRow(pinName, pinData);// directionFunctions);
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

            var enableCheck = new CheckBox { Text = "En", Width = 50, Checked = false, Anchor = AnchorStyles.Left };
            var pinNameBox = new TextBox { Width = 100, Text = pinData.GetProperty("PIN").GetString() };
            var directionToggle = new CheckBox { Text = "Out", Width = 60, Checked = false, Anchor = AnchorStyles.Left };
            var functionCombo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };

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

        #endregion gpio tab controls

        #region config tab controls



        #endregion config tab controls


    }
}