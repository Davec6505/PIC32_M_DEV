using Microsoft.VisualBasic.ApplicationServices;
using System.Configuration;
using System.Diagnostics;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {

        string filePath = string.Empty;
        string jsonFilePath = string.Empty;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "PIC32Mn PROJ - " + Application.ProductVersion;

            filePath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\xc32\\32MZ2048EFH064\\configuration.data";

            // Fix the JSON file path - it should be relative to the project directory
            jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "dependancies", "pic32mz_device.json");
            jsonFilePath = Path.GetFullPath(jsonFilePath); // Resolve the relative path

            Debug.WriteLine($"JSON file path: {jsonFilePath}");
            Debug.WriteLine($"JSON file exists: {File.Exists(jsonFilePath)}");

            if (File.Exists(filePath))
            {
                // File exists, you can proceed with your logic
                extractConfigData(filePath);
            }
            else
            {
                // File does not exist, handle accordingly
                MessageBox.Show($"The file at path {filePath} does not exist.");
            }

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        #region data extraction from config files

        private void extractConfigData(string filePath)
        {
            var blocks = new List<SettingBlock>();
            SettingBlock currentBlock = null;

            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(':');
                if (parts.Length < 4) continue;

                var type = parts[0].Trim();

                if (type == "CSETTING")
                {
                    currentBlock = new SettingBlock
                    {
                        Address = parts[1].Trim(),
                        Label = parts[2].Trim(),
                        Description = parts[3].Trim()
                    };
                    blocks.Add(currentBlock);
                }
                else if (type == "CVALUE" && currentBlock != null)
                {
                    var value = parts[2].Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        currentBlock.Values.Add(new ValueEntry
                        {
                            Address = parts[1].Trim(),
                            Value = value,
                            Description = parts[3].Trim()
                        });
                    }
                }


            }

            // Example output
            foreach (var block in blocks)
            {
                Debug.WriteLine($"CSETTING: {block.Label} @ {block.Address} → {block.Description}");
                foreach (var val in block.Values)
                {
                    Debug.WriteLine($"  CVALUE: {val.Value} @ {val.Address} → {val.Description}");
                }
                Debug.WriteLine(Environment.NewLine);
            }

            if(File.Exists(jsonFilePath))
            {
                SaveConfigurationToJson(blocks);

            }
            else
            {
                MessageBox.Show($"No existing JSON found. A new file will be created at: {jsonFilePath}");
            }
        }

        private void SaveConfigurationToJson(List<SettingBlock> blocks)
        {
            try
            {
                // Load existing JSON or create new structure
                PIC32DeviceConfig deviceConfig;

                if (File.Exists(jsonFilePath))
                {
                    var existingJson = File.ReadAllText(jsonFilePath);
                    try
                    {
                        deviceConfig = JsonSerializer.Deserialize<PIC32DeviceConfig>(existingJson) ?? new PIC32DeviceConfig();
                    }
                    catch
                    {
                        // If deserialization fails, create new config but preserve exclude if it exists
                        deviceConfig = new PIC32DeviceConfig();
                        if (existingJson.Contains("exclude"))
                        {
                            var tempConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
                            if (tempConfig?.ContainsKey("exclude") == true)
                            {
                                deviceConfig.Exclude = JsonSerializer.Deserialize<string[]>(tempConfig["exclude"].ToString());
                            }
                        }
                    }
                }
                else
                {
                    deviceConfig = new PIC32DeviceConfig();
                }

                // Clear existing configuration settings and add new ones
                deviceConfig.ConfigurationSettings.Clear();

                foreach (var block in blocks)
                {
                    var setting = new ConfigurationSetting
                    {
                        Address = block.Address,
                        Label = block.Label,
                        Description = block.Description,
                        Values = block.Values.Select(v => new ConfigurationValue
                        {
                            Address = v.Address,
                            Value = v.Value,
                            Description = v.Description
                        }).ToList()
                    };

                    deviceConfig.ConfigurationSettings.Add(setting);
                }

                // Serialize with pretty formatting
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonString = JsonSerializer.Serialize(deviceConfig, options);

                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(jsonFilePath));

                File.WriteAllText(jsonFilePath, jsonString);

                Debug.WriteLine($"Configuration data saved to: {jsonFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving configuration to JSON: {ex.Message}");
            }
        }

        #endregion
    }


    public class SettingBlock
    {
        public string Address { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public List<ValueEntry> Values { get; set; } = new();
    }

    public class ValueEntry
    {
        public string Address { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }

    // JSON structure classes
    public class PIC32DeviceConfig
    {
        public string[] Exclude { get; set; } = new[]
        {
            "**/bin",
            "**/bower_components",
            "**/jspm_packages",
            "**/node_modules",
            "**/obj",
            "**/platforms"
        };

        public List<ConfigurationSetting> ConfigurationSettings { get; set; } = new();
    }

    public class ConfigurationSetting
    {
        public string Address { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public List<ConfigurationValue> Values { get; set; } = new();
    }

    public class ConfigurationValue
    {
        public string Address { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }

}