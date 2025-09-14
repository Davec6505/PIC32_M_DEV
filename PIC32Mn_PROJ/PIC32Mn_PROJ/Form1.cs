using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

using Microsoft.WindowsAPICodePack.Dialogs;
using PIC32Mn_PROJ.classes;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
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

        // Application specific paths
        #region App specific paths    
        string rootPath = string.Empty;
        string packsPath = string.Empty;
        string picPath = string.Empty;

        string adtfPath = string.Empty;   //path to .atdf
        string configOutput = string.Empty; //path to .json
        string adchsOutput = string.Empty; //path to adchs .json                                          
        string gpioPath = string.Empty; //path to pin .data
        string outputPath = string.Empty;   //path to pin .json
        string opath = string.Empty;
        #endregion App specific paths

        // Project properties
        #region Project properties

        public string projectDirPath { get; set; }

        public string projectName { get; set; }
        public string projectVersion { get; set; }
        public string projectDir { get; set; }
        public string projectType { get; set; }

        public string device { get; set; }
        public bool saveNeeded { get; set; } = false;

        private TextEditor avalonEditor; // AvalonEdit instance for code viewing
        private string currentViewFilePath;
        // In Form1_Load, after initializing avalonEditor#
        #endregion Project properties

        #region Form Initialization
        public Form1()
        {
            InitializeComponent();

            // path of Form1 C:\Users\davec\GIT\PIC32_M_DEV\PIC32Mn_PROJ\PIC32Mn_PROJ\XML\
            rootPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\";
            //packsPath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\";
            packsPath = $"{rootPath}XML\\";

            //paths specific to this application, will have to sort this out
            picPath = $"{packsPath}edc\\PIC32MZ2048EFH064.PIC"; // Replace with your actual XML file path
            adtfPath = $"{packsPath}atdf\\PIC32MZ2048EFH064.atdf";

            configOutput = $"{rootPath}dependancies\\CONFIGValues.json";
            adchsOutput = $"{rootPath}dependancies\\ADCHSValues.json";

            gpioPath = $"{rootPath}dependancies\\gpio\\pic32mz_device.json";
            outputPath = $"{rootPath}dependancies\\gpio\\PinMappings.txt"; // Output file
            opath = $"{rootPath}dependancies\\modules\\";


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Project Initialization
            var savedPath = AppSettings.Default["projectPath"]?.ToString();


            if (!string.IsNullOrEmpty(savedPath))
            {
                ProjectSettingsManager.SettingsFileName_ = "ProjectSettings.json";
                projectDirPath = savedPath;
                PopulateTreeViewWithFoldersAndFiles(projectDirPath);
                PopulateFRCDIVComboBox();
                if (CheckProjectSettingsExists())
                {
                    device = ProjectSettingsManager.GetDevice(projectDirPath);
                    this.Text = $"{projectDirPath} - {device}";


                }
                else
                {
                    var settings = new ProjectSettings();
                    settings["Device"] = "";
                    ProjectSettingsManager.Save(projectDirPath, settings);
                    GetDevice();
                    device = ProjectSettingsManager.GetDevice(projectDirPath);
                    this.Text = $"{projectDirPath} - {device}";
                }
            }


            BuildConfigGroupBoxesFromJson("dependancies\\modules\\FUSECONFIG.json");
            Init_ClockDiagram_Combos();

            if (!string.IsNullOrEmpty(device))
            {
                //load the json file to controls.
                string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
                var config = ProjectConfigProvider.LoadConfig(projectDirPath, ttFilePath, device);
                ApplyConfigDefaultsToControls(config);
            }



            // --- All other initialization code should always run ---
            mods = new Modules(adtfPath, opath);
            pins = new pins(picPath, gpioPath);


            pins.LoadPins();

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

            load_pinForm(gpioPath);
            // AvalonEdit setup
            TextEditor avalonEditor = new TextEditor();
            avalonEditor.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            avalonEditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            avalonEditor.ShowLineNumbers = true;
            avalonEditor.SyntaxHighlighting = null;
            avalonEditor.IsReadOnly = false;

            ElementHost elementHost = new ElementHost();
            elementHost.BackColor = System.Drawing.Color.White;
            elementHost.Margin = new Padding(0);
            elementHost.Padding = new Padding(0);
            elementHost.Dock = DockStyle.Fill;
            elementHost.Child = avalonEditor;

            tabPage_View.Padding = new Padding(0);
            tabPage_View.Controls.Add(elementHost);

            this.avalonEditor = avalonEditor;
            avalonEditor.TextChanged += (s, e2) => { saveNeeded = true; };
            LoadDefaults();
            treeView_Project.AfterSelect += treeView_Project_AfterSelect;
            panel_ClockDiagram.Resize += Panel_ClockDiagram_Resize;

            assign_events_clockdiagram();
            tooltips_clockdiagram();
            this.Panel_ClockDiagram_Resize(panel_ClockDiagram, null);

        }



        #endregion Form Initialization

        // Project menustrip items
        #region  menu items
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select the project folder";
                dialog.InitialDirectory = rootPath;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrEmpty(dialog.FileName))
                {
                    projectDirPath = dialog.FileName;
                    PopulateTreeViewWithFoldersAndFiles(projectDirPath);
                    // Save the selected path to AppSettings
                    AppSettings.Default["projectPath"] = projectDirPath;
                    AppSettings.Default.Save();

                    if (CheckProjectSettingsExists())
                    {
                        // Load device and config from ProjectSettings.json
                        var deviceName = ProjectSettingsManager.GetDevice(projectDirPath);
                        device = deviceName;
                        this.Text = $"{projectDirPath} - {device}";

                        // Load config for the device
                        string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
                        var config = ProjectConfigProvider.LoadConfig(projectDirPath, ttFilePath, device);
                        ApplyConfigDefaultsToControls(config);
                        return;
                    }
                    else
                    {
                        // Create ProjectSettings.json in the project directory if it doesn't exist
                        var settings = new ProjectSettings();
                        settings["Device"] = "";
                        ProjectSettingsManager.Save(projectDirPath, settings);
                        // Prompt user to select device for newly created projectsettings.json
                        GetDevice();
                    }
                    this.Text = $"{projectDirPath} - {ProjectSettingsManager.LoadKey(projectDirPath, "Device")}";
                }
            }
        }


        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Save the file in the editor if needed
            if (!string.IsNullOrEmpty(currentViewFilePath) && avalonEditor != null)
            {
                File.WriteAllText(currentViewFilePath, avalonEditor.Text);
            }

            // Save project-level config from UI controls
            if (!string.IsNullOrEmpty(projectDirPath) && !string.IsNullOrEmpty(device))
            {
                var config = CollectCurrentConfigValues();
                ProjectConfigProvider.SaveDeviceConfig(projectDirPath, device, config);

                // --- Reload config and re-apply to controls ---
                string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
                var newConfig = ProjectConfigProvider.LoadConfig(projectDirPath, ttFilePath, device);
                ApplyConfigDefaultsToControls(newConfig);
                SyncGraphicComboBoxItems();

                MessageBox.Show("Project settings saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshAvalonEditor();
            }
            else
            {
                MessageBox.Show("No project or device selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void deviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetDevice();
        }

        private void createProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select the parent folder for your new project";
                dialog.InitialDirectory = rootPath;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string parentPath = dialog.FileName;
                    string projectName = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter a name for your new project:", "New Project", "MyProject");

                    if (string.IsNullOrWhiteSpace(projectName))
                    {
                        MessageBox.Show("Project name cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string newProjectPath = Path.Combine(parentPath, projectName);
                    if (Directory.Exists(newProjectPath))
                    {
                        MessageBox.Show("A project with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    Directory.CreateDirectory(newProjectPath);

                    var settings = new ProjectSettings();
                    settings["Device"] = "";
                    ProjectSettingsManager.Save(newProjectPath, settings);

                    projectDirPath = newProjectPath;

                    //create standard project subfolders
                    Directory.CreateDirectory(Path.Combine(newProjectPath, "srcs"));
                    Directory.CreateDirectory(Path.Combine(newProjectPath, "incs"));
                    Directory.CreateDirectory(Path.Combine(newProjectPath, "libs"));
                    Directory.CreateDirectory(Path.Combine(newProjectPath, "objs"));
                    Directory.CreateDirectory(Path.Combine(newProjectPath, "other"));

                    File.Copy($"{rootPath}dependancies\\makefiles\\Makefile_Root", Path.Combine(newProjectPath, "", "Makefile"));
                    if (Directory.Exists(Path.Combine(newProjectPath, "srcs")))
                    {
                        File.Copy($"{rootPath}dependancies\\project_files\\main.c", Path.Combine(newProjectPath, "srcs", "main.c"));
                        File.Copy($"{rootPath}dependancies\\makefiles\\Makefile_Srcs", Path.Combine(newProjectPath, "srcs", "Makefile"));

                        Directory.CreateDirectory(Path.Combine(newProjectPath, "srcs\\startup"));
                        if (Directory.Exists(Path.Combine(newProjectPath, "srcs", "startup")))
                        {
                            File.Copy($"{rootPath}dependancies\\project_files\\startup.S", Path.Combine(newProjectPath, "srcs\\startup", "startup.S"));
                        }
                    }


                    PopulateTreeViewWithFoldersAndFiles(projectDirPath);
                    AppSettings.Default["projectPath"] = projectDirPath;
                    AppSettings.Default.Save();

                    GetDevice();
                    // this.Text = $"{projectDirPath} - {settings["Device"]}";
                }
            }
        }

        #endregion menu

        #region  project tree view

        private void PopulateTreeViewWithFoldersAndFiles(string rootFolderPath)
        {
            treeView_Project.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(rootFolderPath);
            var rootNode = new TreeNode(rootDirectoryInfo.Name) { Tag = rootDirectoryInfo };
            treeView_Project.Nodes.Add(rootNode);
            AddDirectoriesAndFiles(rootDirectoryInfo, rootNode);
            rootNode.Expand();
        }

        private void AddDirectoriesAndFiles(DirectoryInfo directoryInfo, TreeNode parentNode)
        {
            // Add directories
            foreach (var directory in directoryInfo.GetDirectories())
            {
                var dirNode = new TreeNode(directory.Name) { Tag = directory };
                parentNode.Nodes.Add(dirNode);
                AddDirectoriesAndFiles(directory, dirNode);
            }

            // Add files
            foreach (var file in directoryInfo.GetFiles())
            {
                var fileNode = new TreeNode(file.Name) { Tag = file };
                parentNode.Nodes.Add(fileNode);
            }
        }
        #endregion project tree view

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

                BuildPinRow(pinEntry.Name, pinName, pinData);// directionFunctions);
            }

        }

        private void BuildPinRow(string pin_num, string pinKey, JsonElement pinData)
        {
            var rowPanel = new Panel
            {
                Width = 500,//flowPanelPins.Width - 25,
                Height = 35,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = pinKey // for later reference
            };

            var pinNumLable = new Label { Text = pin_num, Width = 50 };
            var enableCheck = new CheckBox { Text = "En", Width = 50, Checked = false, Anchor = AnchorStyles.Left };
            var pinNameBox = new TextBox { Width = 100, Text = pinData.GetProperty("PIN").GetString() };
            var directionToggle = new CheckBox { Text = "Out", Width = 60, Checked = false, Anchor = AnchorStyles.Left };
            var functionCombo = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right };

            var directionFunctions = new Dictionary<string, List<string>>();
            if (pinData.TryGetProperty("PinFunctions", out var pinFunctions) && pinFunctions.TryGetProperty("direction", out var directionElement))
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
            int xposOffset = tabPage_Gpio.Left;
            pinNumLable.Location = new Point(xposOffset, 5);
            enableCheck.Location = new Point(xposOffset + 50, 5);
            pinNameBox.Location = new Point(xposOffset + 100, 5);
            directionToggle.Location = new Point(xposOffset + 230, 5);
            functionCombo.Location = new Point(xposOffset + 290, 5);

            rowPanel.Controls.Add(pinNumLable);
            rowPanel.Controls.Add(pinNameBox);
            rowPanel.Controls.Add(enableCheck);
            rowPanel.Controls.Add(directionToggle);
            rowPanel.Controls.Add(functionCombo);

            flowPanelPins.Controls.Add(rowPanel);
        }

        #endregion gpio tab controls

        #region Form Helper methods

        /// <summary>
        /// Check to see if ProjectSettings.json exists in the project directory
        /// </summary>
        /// <returns name="exists">Returns true if ProjectSettings.json exists, false otherwise.</returns>
        public bool CheckProjectSettingsExists()
        {
            string settingsPath = Path.Combine(projectDirPath, "ProjectSettings.json");
            return File.Exists(settingsPath);
        }

        /// <summary>
        /// Displays a dialog for selecting a device and updates the project settings with the selected device.
        /// </summary>
        /// <remarks>This method opens a dialog box that allows the user to select a device. If a device
        /// is selected and  the project settings file exists, the selected device is saved to the project settings and
        /// the window  title is updated to reflect the selected device. If the project settings file is missing or no
        /// device  is selected, an error message is displayed.</remarks>
        private void GetDevice()
        {
            Device devForm = new Device(device);
            devForm.ShowDialog();
            string _device = devForm._Device;

            if (CheckProjectSettingsExists() && !string.IsNullOrEmpty(_device))
            {
                // Get config dictionary (from defaults, UI, or .tt)
                string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
                var config = ProjectConfigProvider.LoadConfig(projectDirPath, ttFilePath, _device);

                // Save device and config under device node
                ProjectConfigProvider.SaveDeviceConfig(projectDirPath, _device, config);

                // Use ProjectSettingsManager to get the device name
                device = ProjectSettingsManager.GetDevice(projectDirPath);
                this.Text = $"{projectDirPath} - {device}";

                // Refresh AvalonEdit text
                RefreshAvalonEditor();
            }
            else
            {
                MessageBox.Show("Project settings file not found or device not selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Form Helper methods


        #region Avilon Edit View Tab

        /// <summary>
        /// Displays the contents of the specified file in the AvalonEdit control with appropriate syntax highlighting.
        /// </summary>
        /// <param name="filePath"></param>
        private void DisplayFileInViewTab(string filePath)
        {
            if (!File.Exists(filePath) || avalonEditor == null)
                return;

            avalonEditor.Text = File.ReadAllText(filePath);
            currentViewFilePath = filePath; // Track the file being edited

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string fileName = Path.GetFileName(filePath);

            // Highlight Makefile by name (case-insensitive)
            if (fileName.Equals("Makefile", StringComparison.OrdinalIgnoreCase))
            {
                avalonEditor.SyntaxHighlighting = LoadCustomHighlighting("PIC32Mn_PROJ.dependancies.Highlighting.makefile.xshd");
                return;
            }

            if (fileName.Equals("startup", StringComparison.OrdinalIgnoreCase))
            {
                avalonEditor.SyntaxHighlighting = LoadCustomHighlighting("PIC32Mn_PROJ.dependancies.Highlighting.asm.xshd");
                return;
            }

            // Set syntax highlighting by extension
            switch (ext)
            {
                case ".c":
                case ".h":
                    avalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C++");
                    break;
                case ".json":
                    avalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");
                    break;
                case ".xml":
                    avalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    break;
                case ".s":
                case ".asm":
                    avalonEditor.SyntaxHighlighting = LoadCustomHighlighting("PIC32Mn_PROJ.dependancies.Highlighting.asm.xshd");
                    break;
                case ".mk":
                    avalonEditor.SyntaxHighlighting = LoadCustomHighlighting("PIC32Mn_PROJ.dependancies.Highlighting.makefile.xshd");
                    break;
                default:
                    avalonEditor.SyntaxHighlighting = null;
                    break;
            }
        }



        private IHighlightingDefinition LoadCustomHighlighting(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new XmlTextReader(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        private void RefreshAvalonEditor()
        {
            if (!string.IsNullOrEmpty(currentViewFilePath) && File.Exists(currentViewFilePath) && avalonEditor != null)
            {
                avalonEditor.Text = File.ReadAllText(currentViewFilePath);
            }
        }


        #endregion Avilon Edit View Tab

        #region treeview event handlers

        private void treeView_Project_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is FileInfo fileInfo)
            {
                DisplayFileInViewTab(fileInfo.FullName);
            }
        }

        #endregion treeview event handlers

        #region System tab controls

        #region Panel1 Config groupBox

        // Dictionary to map config key to all ComboBoxes representing it (Panel1 and graphic)
        private readonly Dictionary<string, List<Control>> configKeyToControls = new();

        // Call this in Form1_Load after loading the config JSON
        private void BuildConfigGroupBoxesFromJson(string fuseConfigPath)
        {
            string json = File.ReadAllText(fuseConfigPath);
            using var doc = JsonDocument.Parse(json);

            var fuseConfig = doc.RootElement.GetProperty("FUSECONFIG");

            int groupBoxTop = 10;
            foreach (var section in fuseConfig.EnumerateObject())
            {
                var groupBox = new GroupBox
                {
                    Text = section.Name,
                    Width = panelConfigSections.Width - 30,
                    Height = 60 + section.Value.EnumerateObject().Count() * 35,
                    Top = groupBoxTop,
                    Left = 10,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                int controlTop = 25;
                foreach (var item in section.Value.EnumerateObject())
                {
                    var label = new Label
                    {
                        Text = item.Name,
                        Left = 10,
                        Top = controlTop + 5,
                        Width = 100
                    };

                    Control inputControl;
                    var arr = item.Value.EnumerateArray().ToArray();
                    if (arr.Length == 0)
                    {

                        // Use TextBox for user input
                        var textBox = new TextBox
                        {
                            Text = (item.Name == "USERID") ? "0xffff" : "",
                            Left = 110,
                            Width = groupBox.Width - 140,
                            Top = controlTop,
                            Anchor = AnchorStyles.Left | AnchorStyles.Right,
                            Tag = item.Name
                        };
                        textBox.TextChanged += ConfigTextBox_TextChanged;

                        // Register in the dictionary for synchronization
                        if (!configKeyToControls.ContainsKey(item.Name))
                            configKeyToControls[item.Name] = new List<Control>();
                        configKeyToControls[item.Name].Add(textBox);

                        inputControl = textBox;
                    }
                    else
                    {
                        // Use ComboBox for selection
                        var combo = new ComboBox
                        {
                            Left = 110,
                            Width = groupBox.Width - 140,
                            Top = controlTop,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            Tag = item.Name,
                            Anchor = AnchorStyles.Left | AnchorStyles.Right
                        };
                        foreach (var val in arr)
                            combo.Items.Add(val.GetString());
                        combo.SelectedIndexChanged += ConfigComboBox_SelectedIndexChanged;

                        // Register in the dictionary for synchronization
                        RegisterComboBoxByTag(combo);

                        inputControl = combo;
                    }

                    groupBox.Controls.Add(label);
                    groupBox.Controls.Add(inputControl);

                    controlTop += 35;
                }

                panelConfigSections.Controls.Add(groupBox);
                groupBoxTop += groupBox.Height + 10;

                string? projectDir = AppSettings.Default["projectPath"]?.ToString();
                string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
                string? device = ProjectSettingsManager.LoadKey(projectDir ?? string.Empty, "Device") as string;
                var config = ProjectConfigProvider.LoadConfig(projectDir, ttFilePath, device);

                ApplyConfigDefaultsToControls(config);

            }

            SyncGraphicComboBoxItems();
        }

        private void Init_ClockDiagram_Combos()
        {
            foreach (Control ctrl in panel_ClockDiagram.Controls)
            {
                if (ctrl is ComboBox combo)
                    RegisterComboBoxByTag(combo);
            }
        }

        private void RegisterComboBoxByTag(ComboBox combo)
        {
            if (combo.Tag is string key && !string.IsNullOrEmpty(key))
            {
                if (!configKeyToControls.ContainsKey(key))
                    configKeyToControls[key] = new List<Control>();
                configKeyToControls[key].Add(combo);
                combo.SelectedIndexChanged += ConfigComboBox_SelectedIndexChanged;
            }
        }

        // Synchronize all controls with the same config key when a ComboBox changes
        private void ConfigComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var changedCombo = sender as ComboBox;
            var key = changedCombo.Tag as string;
            if (key == null) return;

            foreach (var control in configKeyToControls[key])
            {
                if (control != changedCombo)
                {
                    if (control is ComboBox combo)
                        combo.SelectedItem = changedCombo.SelectedItem;
                    else if (control is TextBox textBox)
                        textBox.Text = changedCombo.SelectedItem?.ToString() ?? "";
                }
            }
        }

        // Synchronize all controls with the same config key when a TextBox changes
        private void ConfigTextBox_TextChanged(object sender, EventArgs e)
        {
            var changedTextBox = sender as TextBox;
            var key = changedTextBox.Tag as string;
            if (key == null) return;

            foreach (var control in configKeyToControls[key])
            {
                if (control != changedTextBox)
                {
                    if (control is ComboBox combo)
                        combo.SelectedItem = changedTextBox.Text;
                    else if (control is TextBox textBox)
                        textBox.Text = changedTextBox.Text;
                }
            }
        }

        private void PopulateFRCDIVComboBox()
        {
            string cruJsonPath = Path.Combine(rootPath, "dependancies", "modules", "CRU.json");
            if (!File.Exists(cruJsonPath)) return;

            string jsonText = File.ReadAllText(cruJsonPath);
            using var doc = JsonDocument.Parse(jsonText);

            if (!doc.RootElement.TryGetProperty("CRU", out var cruObj)) return;
            if (!cruObj.TryGetProperty("OSCCON", out var oscconArr)) return;

            foreach (var field in oscconArr.EnumerateArray())
            {
                if (field.TryGetProperty("name", out var nameProp) && nameProp.GetString() == "FRCDIV")
                {
                    if (field.TryGetProperty("values", out var valuesArr))
                    {
                        comboBox_FRCDIV.Items.Clear();
                        foreach (var val in valuesArr.EnumerateArray())
                        {
                            //string caption = val.TryGetProperty("caption", out var cap) ? cap.GetString() : "";
                            string value = val.TryGetProperty("name", out var v) ? v.GetString() : "";
                            if (!string.IsNullOrEmpty(value))
                            {
                                var divs = value.Split('_');
                                if (divs.Length >2)
                                    comboBox_FRCDIV.Items.Add($"{divs[2]}_{divs[3]}");
                                else
                                    comboBox_FRCDIV.Items.Clear();
                            }
                           
                        }
                    }
                    break;
                }
            }

            comboBox_FRCDIV.Tag = "FRCDIV";
            RegisterComboBoxByTag(comboBox_FRCDIV);
        }

        #endregion Panel1 Config groupBox

        #region Panel2 Clock Diagram



        private void SyncGraphicComboBoxItems()
        {
            foreach (Control ctrl in panel_ClockDiagram.Controls)
            {
                if (ctrl is ComboBox graphicCombo && graphicCombo.Tag is string key && !string.IsNullOrEmpty(key))
                {
                    // Find the config ComboBox for this key (skip the graphic one itself)
                    if (configKeyToControls.TryGetValue(key, out var controls))
                    {
                        var configCombo = controls
                            .OfType<ComboBox>()
                            .FirstOrDefault(c => c != graphicCombo);

                        if (configCombo != null)
                        {
                            graphicCombo.Items.Clear();
                            foreach (var item in configCombo.Items)
                                graphicCombo.Items.Add(item);

                            // Optionally, sync the selected item as well
                            graphicCombo.SelectedItem = configCombo.SelectedItem;
                        }
                    }
                }
            }
        }

        #endregion Panel2 Clock Diagram




        #endregion System tab controls

        #region GetDefaults for config from tt file or project settings

        private void LoadDefaults()
        {
            // Example usage:
            string? projectDir = AppSettings.Default["projectPath"]?.ToString();
            string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
            string? device = ProjectSettingsManager.LoadKey(projectDir ?? string.Empty, "Device") as string;
            var config = ProjectConfigProvider.LoadConfig(projectDir, ttFilePath, device);
            // Use 'config' to populate your UI controls
        }

        private void ApplyConfigDefaultsToControls(Dictionary<string, string> config)
        {
            foreach (var kvp in config)
            {
                if (configKeyToControls.TryGetValue(kvp.Key, out var controls))
                {
                    foreach (var control in controls)
                    {
                        if (control is ComboBox combo)
                        {
                            if (combo.Items.Contains(kvp.Value))
                                combo.SelectedItem = kvp.Value;
                        }
                        else if (control is TextBox textBox)
                        {
                            textBox.Text = kvp.Value;
                        }
                    }
                }
            }
            // Optionally, set FRCDIV directly if not handled by the above
            if (config.TryGetValue("FRCDIV", out var frcdivValue) && comboBox_FRCDIV.Items.Contains(frcdivValue))
                comboBox_FRCDIV.SelectedItem = frcdivValue;
        }

        private Dictionary<string, string> CollectCurrentConfigValues()
        {
            var config = new Dictionary<string, string>();
            foreach (var kvp in configKeyToControls)
            {
                // Use the first control as the source of truth (all are synced)
                var control = kvp.Value.FirstOrDefault();
                if (control is ComboBox combo)
                {
                    if (combo.SelectedItem != null)
                        config[kvp.Key] = combo.SelectedItem.ToString();
                }
                else if (control is TextBox textBox)
                {
                    config[kvp.Key] = textBox.Text;
                }
            }

            if (!config.ContainsKey("FRCDIV") && comboBox_FRCDIV.SelectedItem != null)
                config["FRCDIV"] = comboBox_FRCDIV.SelectedItem.ToString();
            return config;
        }

        #endregion GetDefaults for config from tt file or project settings



    }

}