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
using System.Drawing;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text; // added
using System.Collections.Generic; // added
using System.Linq; // added

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

        // Form fields
        // Add these fields to the Form1 class
        private ContextMenuStrip treeContextMenu;
        private ToolStripMenuItem deleteMenuItem;
        private TreeNode? contextNode;


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
            ApplyGpioOverridesFromProjectJson();
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

            // Add these lines to enable right-click delete on the dynamic tree
            SetupTreeViewContextMenu();
            treeView_Project.NodeMouseClick += treeView_Project_NodeMouseClick;
            treeView_Project.KeyDown += treeView_Project_KeyDown;
        
            ApplyCHighlightingColors(avalonEditor);
        }

        private void ApplyGpioOverridesFromProjectJson()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(projectDirPath) || string.IsNullOrWhiteSpace(device)) return;
                var node = ProjectModelWriter.LoadProjectJson(projectDirPath, device);
                if (node?.gpio?.pins == null || node.gpio.pins.Count == 0) return;

                // Map rows by pin name (textbox value like RE5)
                var rows = new Dictionary<string, Panel>(StringComparer.OrdinalIgnoreCase);
                foreach (Control row in flowPanelPins.Controls)
                {
                    if (row is Panel p)
                    {
                        var nameBox = p.Controls.OfType<TextBox>().FirstOrDefault();
                        if (nameBox != null && !string.IsNullOrWhiteSpace(nameBox.Text))
                        {
                            rows[nameBox.Text] = p;
                        }
                    }
                }

                foreach (var pin in node.gpio.pins)
                {
                    if (!rows.TryGetValue(pin.name ?? string.Empty, out var p)) continue;

                    var en = p.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Text == "En");
                    var dir = p.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Text == "Out");
                    var func = p.Controls.OfType<ComboBox>().FirstOrDefault();

                    if (en != null) en.Checked = pin.enabled;
                    if (dir != null) dir.Checked = pin.output;

                    // Ensure function list reflects the direction before selecting
                    if (dir != null && func != null)
                    {
                        // Trigger the CheckedChanged handler to repopulate
                        dir.Checked = pin.output;
                        if (!string.IsNullOrEmpty(pin.function))
                        {
                            // If function exists in current list, select it
                            var idx = func.FindStringExact(pin.function);
                            if (idx >= 0) func.SelectedIndex = idx;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplyGpioOverridesFromProjectJson error: {ex.Message}");
            }
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

                        // Reload GPIO overrides into UI
                        ApplyGpioOverridesFromProjectJson();
                        return;
                    }
                    else
                    {
                        // Create ProjectSettings.json in the project directory if it doesn't exist
                        var settings = new ProjectSettings();
                        settings["Device"] = "";
                        ProjectSettingsManager.Save(newProjectPath: projectDirPath, settings: settings);
                        // Prompt user to select device for newly created projectsettings.json
                        GetDevice();
                    }
                    this.Text = $"{projectDirPath} - {ProjectSettingsManager.LoadKey(projectDirPath, "Device")}";
                }
            }
        }


        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Try to recover current project/device if fields are empty
            if (string.IsNullOrEmpty(projectDirPath))
            {
                var savedPath = AppSettings.Default["projectPath"]?.ToString();
                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                {
                    projectDirPath = savedPath;
                }
            }
            if (string.IsNullOrEmpty(device) && !string.IsNullOrEmpty(projectDirPath))
            {
                try
                {
                    ProjectSettingsManager.SettingsFileName_ = "ProjectSettings.json";
                    device = ProjectSettingsManager.GetDevice(projectDirPath);
                }
                catch { /* ignore */ }
            }

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

                // NEW: collect GPIO overrides and save + emit gpio.vars.properties for .tt
                var gpioOverrides = CollectGpioOverrides();
                SaveGpioOverridesToProject(projectDirPath, gpioOverrides);
                WriteGpioVarsProperties(gpioOverrides);

                // NEW: write consolidated device-rooted JSON for all generators
                try
                {
                    ProjectModelWriter.SaveProjectJson(projectDirPath, device, config, gpioOverrides);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ProjectModelWriter error: {ex.Message}");
                }

                // --- Reload config and re-apply to controls ---
                string ttFilePath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
                var newConfig = ProjectConfigProvider.LoadConfig(projectDirPath, ttFilePath, device);
                ApplyConfigDefaultsToControls(newConfig);
                SyncGraphicComboBoxItems();

                System.Windows.Forms.MessageBox.Show("Project settings saved.", "Save", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                RefreshAvalonEditor();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("No project or device selected.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
           System.Windows.Forms.Application.Exit();
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
                        System.Windows.Forms.MessageBox.Show("Project name cannot be empty.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                        return;
                    }

                    string newProjectPath = Path.Combine(parentPath, projectName);
                    if (Directory.Exists(newProjectPath))
                    {
                        System.Windows.Forms.MessageBox.Show("A project with this name already exists.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
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

                }
            }
        }

        // Make the Generate click handler await the async generator and add guards
        private async void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(projectDirPath))
            {
                System.Windows.Forms.MessageBox.Show("No project loaded.", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate template paths before running
            var cTtPath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
            var hTtPath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.h.tt");
            var cclkPath = Path.Combine(rootPath, "dependancies", "templates", "plib_clk.c.tt");
            var hclkPath = Path.Combine(rootPath, "dependancies", "templates", "plib_clk.h.tt");
            var gpioHPath = Path.Combine(rootPath, "dependancies", "templates", "plib_gpio.h.tt");
            var gpioCPath = Path.Combine(rootPath, "dependancies", "templates", "plib_gpio.c.tt");

            // Check config templates
            if (!File.Exists(cTtPath))
            {
                System.Windows.Forms.MessageBox.Show($"Missing template:\n{cTtPath}", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(hTtPath))
            {
                System.Windows.Forms.MessageBox.Show($"Missing template:\n{hTtPath}", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // Check clock templates
            if (!File.Exists(hclkPath))
            {
                System.Windows.Forms.MessageBox.Show($"Missing template:\n{hclkPath}", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!File.Exists(cclkPath))
            {
                System.Windows.Forms.MessageBox.Show($"Missing template:\n{cclkPath}", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Optional: ensure gpio templates exist
            if (!File.Exists(gpioHPath) || !File.Exists(gpioCPath))
            {
                System.Windows.Forms.MessageBox.Show("GPIO templates (.tt) not found. Skipping GPIO generation.", "Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Disable while generating
            var senderItem = sender as ToolStripItem;
            if (senderItem?.Owner is MenuStrip ms && ms.FindForm() is Form f)
                f.Cursor = Cursors.WaitCursor;
            try
            {
                await project_generate_fromttfiles();
                // Refresh the tree so the new files appear immediately
                PopulateTreeViewWithFoldersAndFiles(projectDirPath);
            }
            finally
            {
                if (senderItem?.Owner is MenuStrip ms2 && ms2.FindForm() is Form f2)
                    f2.Cursor = Cursors.Default;
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

        // Add to Form1 class
        private void SetupTreeViewContextMenu()
        {
            treeContextMenu = new ContextMenuStrip();
            deleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedNode());
            treeContextMenu.Items.Add(deleteMenuItem);
        }

        private void treeView_Project_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            treeView_Project.SelectedNode = e.Node;
            contextNode = e.Node;

            // Prevent deleting the root node
            deleteMenuItem.Enabled = e.Node.Parent != null;
            treeContextMenu.Show(treeView_Project, e.Location);
        }

        private void treeView_Project_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedNode();
                e.Handled = true;
            }
        }

        private void DeleteSelectedNode()
        {
            var node = contextNode ?? treeView_Project.SelectedNode;
            contextNode = null;
            if (node == null) return;

            // Disallow deleting the root (top-level) node
            if (node.Parent == null)
            {
                System.Windows.Forms.MessageBox.Show("Cannot delete the project root folder.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (node.Tag is FileInfo fi)
                {
                    if (!IsUnderProjectRoot(fi.FullName))
                    {
                        System.Windows.Forms.MessageBox.Show("Blocked: outside project root.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var confirm = System.Windows.Forms.MessageBox.Show($"Delete file:\n{fi.FullName}?", "Confirm Delete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;

                    // If file is open in editor, clear it
                    if (!string.IsNullOrEmpty(currentViewFilePath) &&
                        string.Equals(currentViewFilePath, fi.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        avalonEditor.Text = string.Empty;
                        currentViewFilePath = string.Empty;
                    }

                    if (File.Exists(fi.FullName))
                    {
                        File.SetAttributes(fi.FullName, FileAttributes.Normal);
                        File.Delete(fi.FullName);
                    }

                    node.Remove();
                }
                else if (node.Tag is DirectoryInfo di)
                {
                    if (!IsUnderProjectRoot(di.FullName))
                    {
                        System.Windows.Forms.MessageBox.Show("Blocked: outside project root.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var confirm = System.Windows.Forms.MessageBox.Show($"Delete folder and all contents:\n{di.FullName}?",
                        "Confirm Delete Folder", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;

                    if (Directory.Exists(di.FullName))
                        Directory.Delete(di.FullName, recursive: true);

                    node.Remove();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Delete failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsUnderProjectRoot(string path)
        {
            if (string.IsNullOrEmpty(projectDirPath)) return false;
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(projectDirPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
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
            pinNumLable.Location = new System.Drawing.Point(xposOffset, 5);
            enableCheck.Location = new System.Drawing.Point(xposOffset + 50, 5);
            pinNameBox.Location = new System.Drawing.Point(xposOffset + 100, 5);
            directionToggle.Location = new System.Drawing.Point(xposOffset + 230, 5);
            functionCombo.Location = new System.Drawing.Point(xposOffset + 290, 5);

            rowPanel.Controls.Add(pinNumLable);
            rowPanel.Controls.Add(pinNameBox);
            rowPanel.Controls.Add(enableCheck);
            rowPanel.Controls.Add(directionToggle);
            rowPanel.Controls.Add(functionCombo);

            flowPanelPins.Controls.Add(rowPanel);
        }

        // NEW: collect GPIO changes from UI
        private List<GpioOverride> CollectGpioOverrides()
        {
            var list = new List<GpioOverride>();
            foreach (Control row in flowPanelPins.Controls)
            {
                if (row is not Panel p) continue;
                string pinKey = p.Tag as string ?? "";

                var lbl = p.Controls.OfType<Label>().FirstOrDefault();
                var en = p.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Text == "En");
                var nameBox = p.Controls.OfType<TextBox>().FirstOrDefault();
                var dir = p.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Text == "Out");
                var func = p.Controls.OfType<ComboBox>().FirstOrDefault();

                if (nameBox == null) continue;
                // Only track rows where user checked Enable
                if (en != null && en.Checked)
                {
                    list.Add(new GpioOverride
                    {
                        PinKey = pinKey,
                        PinName = nameBox.Text ?? string.Empty,
                        Enabled = true,
                        Output = dir?.Checked == true,
                        Function = func?.SelectedItem?.ToString()
                    });
                }
            }
            return list;
        }

        // NEW: save overrides into ProjectSettings.json under key GPIOOverrides
        private void SaveGpioOverridesToProject(string projectDir, List<GpioOverride> overrides)
        {
            var settings = ProjectSettingsManager.Load(projectDir);
            settings["GPIOOverrides"] = overrides;
            ProjectSettingsManager.Save(projectDir, settings);
        }

        // NEW: Emit gpio.vars.properties used by plib_gpio .tt templates
        private void WriteGpioVarsProperties(List<GpioOverride> overrides)
        {
            string propsDir = Path.Combine(rootPath, "dependancies", "templates");
            Directory.CreateDirectory(propsDir);
            string propsPath = Path.Combine(propsDir, "gpio.vars.properties");

            // Consider only rows that have valid channel/pin and were enabled via checkbox
            var validOverrides = overrides
                .Where(o => !string.IsNullOrEmpty(o.PortChannel) && o.PortPin >= 0 && o.Enabled)
                .ToList();

            var sb = new StringBuilder();

            // Gate generation of GPIO files based on at least one enabled pin
            bool generateGpio = validOverrides.Count > 0;
            sb.AppendLine($"GENERATE_GPIO={(generateGpio ? "true" : "false")}");

            // Always include to keep interrupts include present in generated C if desired
            sb.AppendLine("CoreSysIntFile=true");

            // Map channels present and pins count
            var channels = validOverrides
                .GroupBy(o => o.PortChannel)
                .OrderBy(g => g.Key)
                .ToList();

            int chTotal = channels.Count == 0 ? 0 : channels.Max(g => (g.Key[0] - 'A')) + 1;
            int pinTotal = validOverrides.Count;

            sb.AppendLine($"GPIO_CHANNEL_TOTAL={chTotal}");
            sb.AppendLine($"GPIO_PIN_TOTAL={pinTotal}");

            // Per-channel aggregate masks (TRIS to clear for outputs, ANSEL to clear for digital, LAT initial)
            var trisByCh = new Dictionary<string, int>();
            var anselByCh = new Dictionary<string, int>();
            var latByCh = new Dictionary<string, int>();

            foreach (var g in channels)
            {
                int idx = g.Key[0] - 'A';
                sb.AppendLine($"GPIO_CHANNEL_{idx}_NAME={g.Key}");

                // Initialize masks
                trisByCh[g.Key] = 0;
                anselByCh[g.Key] = 0;
                latByCh[g.Key] = 0; // default 0

                foreach (var o in g)
                {
                    if (o.PortPin >= 0 && o.PortPin <= 15)
                    {
                        int bit = 1 << o.PortPin;
                        // Any configured pin: set digital mode (ANSELxCLR)
                        anselByCh[g.Key] |= bit;
                        // If output requested, clear TRIS bit (TRISxCLR)
                        if (o.Output)
                            trisByCh[g.Key] |= bit;
                    }
                }

                // Mark CN used if any override is input
                bool cnUsed = g.Any(o => !o.Output);
                sb.AppendLine($"SYS_PORT_{g.Key}_CN_USED={(cnUsed ? "true" : "false")}");

                // Emit masks as hex strings (without 0x prefix, template adds it)
                string trisHex = trisByCh[g.Key].ToString("x");
                string anselHex = anselByCh[g.Key].ToString("x");
                string latHex = latByCh[g.Key].ToString("x");

                sb.AppendLine($"SYS_PORT_{g.Key}_TRIS={trisHex}");
                sb.AppendLine($"SYS_PORT_{g.Key}_ANSEL={anselHex}");
                sb.AppendLine($"SYS_PORT_{g.Key}_LAT={latHex}");
            }

            // BSP pin entries 1..N per overrides order using the PinName textbox value
            for (int i = 0; i < validOverrides.Count; i++)
            {
                var o = validOverrides[i];
                int idx = i + 1;
                sb.AppendLine($"BSP_PIN_{idx}_PORT_CHANNEL={o.PortChannel}");
                sb.AppendLine($"BSP_PIN_{idx}_PORT_PIN={o.PortPin}");
                sb.AppendLine($"BSP_PIN_{idx}_FUNCTION_TYPE={(string.IsNullOrEmpty(o.Function) ? "GPIO" : o.Function)}");
                sb.AppendLine($"BSP_PIN_{idx}_FUNCTION_NAME={o.PinName}");
                // CN default true for inputs when enabled
                string cn = (!o.Output).ToString();
                sb.AppendLine($"BSP_PIN_{idx}_CN={cn}");
            }

            // Leave PPS related values unset unless available; generator will skip
            // If you want to always unlock IOLOCK like sample, uncomment below:
            // sb.AppendLine("IOLOCK_ENABLE=true");

            File.WriteAllText(propsPath, sb.ToString());
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
                System.Windows.Forms.MessageBox.Show("Project settings file not found or device not selected.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        #endregion Form Helper methods


        #region Avilon Edit View Tab

        /// <summary>
        /// Displays the contents of the specified file in the AvalonEdit control with appropriate syntax highlighting.
        /// </summary>
        /// <param name="filePath"></param>
        // Replace the existing ApplyCHighlightingColors with this version (targets C/C++ instead of C#)
        private void ApplyCHighlightingColors(TextEditor editor)
        {
            // Use the built‑in "C++" definition (AvalonEdit ships C++ but not pure C)
            var def = HighlightingManager.Instance.GetDefinition("C++");
            if (def == null) return;

            void Set(string name, string? fg = null, bool? bold = null, bool? italic = null)
            {
                var colorEntry = def.NamedHighlightingColors.FirstOrDefault(x => x.Name == name);
                if (colorEntry == null) return;
                if (fg != null)
                {
                    var drawingColor = (System.Drawing.Color)System.ComponentModel.TypeDescriptor.GetConverter(typeof(System.Drawing.Color)).ConvertFromString(fg);
                    var mediaColor = System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
                    colorEntry.Foreground = new SimpleHighlightingBrush(mediaColor);
                }
                if (bold.HasValue) colorEntry.FontWeight = bold.Value ? FontWeights.Bold : FontWeights.Normal;
                if (italic.HasValue) colorEntry.FontStyle = italic.Value ? FontStyles.Italic : FontStyles.Normal;
            }

            // Adjust names that exist in the C++ definition
            Set("Keyword", "#FF0080FF", bold: true);
            Set("Type", "#FFFFA500");
            Set("Comment", "#FF5A995A", italic: true);
            Set("String", "#FFCE9178");
            Set("Number", "#FFB5CEA8");
            Set("Preprocessor", "#FF9B9B9B");

            editor.SyntaxHighlighting = def;
        }

        private IHighlightingDefinition LoadHighlightingFromFile(string path)
        {
            using var fs = File.OpenRead(path);
            using var reader = new XmlTextReader(fs);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        private void EnsureCustomCHighlightingRegistered()
        {
            const string name = "C-PIC";
            if (HighlightingManager.Instance.GetDefinition(name) != null)
                return;

            var xshdPath = Path.Combine(rootPath, "dependancies", "highlighting", "c-pic.xshd");
            if (!File.Exists(xshdPath))
                return;

            var def = LoadHighlightingFromFile(xshdPath);
            HighlightingManager.Instance.RegisterHighlighting(name, new[] { ".c", ".h" }, def);
        }

        private void DisplayFileInViewTab(string filePath)
        {
            if (!File.Exists(filePath) || avalonEditor == null)
                return;

            avalonEditor.Text = File.ReadAllText(filePath);
            currentViewFilePath = filePath;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string fileName = Path.GetFileName(filePath);

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

            switch (ext)
            {
                case ".c":
                case ".h":
                    EnsureCustomCHighlightingRegistered();
                    avalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");
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
                                if (divs.Length > 2)
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