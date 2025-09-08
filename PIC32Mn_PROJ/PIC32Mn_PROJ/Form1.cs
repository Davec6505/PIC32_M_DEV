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

        #endregion Project properties

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
            mods = new Modules(adtfPath, opath);
            pins = new pins(picPath, gpioPath);
            // Check if the Application AppSettings has a value for projectPath
            var savedPath = AppSettings.Default["projectPath"].ToString();
            if (!string.IsNullOrEmpty(savedPath))
            {
                ProjectSettingsManager.SettingsFileName_ = "ProjectSettings.json";
                projectDirPath = savedPath;
                PopulateTreeViewWithFoldersAndFiles(projectDirPath);

                if (CheckProjectSettingsExists())
                {
                    // if ProjectSettings.json exists load it
                    var settings = ProjectSettingsManager.Load(projectDirPath);
                    device = Convert.ToString(settings["Device"]);// ?? string.Empty;
                    this.Text = $"{projectDirPath} - {device}";
                }
                else
                {
                    // Create ProjectSettings.json in the project directory if it doesn't exist
                    var settings = new ProjectSettings();
                    settings["Device"] = "";
                    ProjectSettingsManager.Save(projectDirPath, settings);
                    // Prompt user to select device for newly created projectsettings.json
                    GetDevice();
                    this.Text = $"{projectDirPath} - {ProjectSettingsManager.LoadKey(projectDirPath, "Device")}";
                }
                var settings_ = ProjectSettingsManager.Load(projectDirPath);
                if (settings_ != null)
                {
                    device = Convert.ToString(settings_["Device"]);// ?? string.Empty;
                                                                   // CreateProjectSettings();
                    this.Text = $"{projectDirPath} - {device}";
                }


            }

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

            TextEditor avalonEditor = new TextEditor();
            avalonEditor.ShowLineNumbers = true;
            avalonEditor.SyntaxHighlighting = null; // Set later based on file type
            avalonEditor.IsReadOnly = true;

            // Assuming you have a TabPage named tabPage_View and want to fill it:
            ElementHost elementHost = new ElementHost();
            elementHost.Dock = DockStyle.Fill;
            elementHost.Child = avalonEditor;
            tabPage_View.Controls.Add(elementHost);

            // Store reference for later use
            this.avalonEditor = avalonEditor;

            treeView_Project.AfterSelect += treeView_Project_AfterSelect;



        }


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
                        // if ProjectSettings.json exists load it
                        var settings_ = ProjectSettingsManager.Load(projectDirPath);
                        device = Convert.ToString(settings_["Device"]);
                        this.Text = $"{projectDirPath} - {device}";
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
                            File.Copy($"{rootPath}dependancies\\project_files\\startup.S" , Path.Combine(newProjectPath, "srcs\\startup", "startup.S"));
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

            var pinNumLable = new Label { Text = pinKey };
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
            int xposOffset = tabPage_Gpio.Left;
            pinNumLable.Location = new Point(xposOffset, 5);
            enableCheck.Location = new Point(xposOffset + 5, 5);
            pinNameBox.Location = new Point(xposOffset + 60, 5);
            directionToggle.Location = new Point(xposOffset + 170, 5);
            functionCombo.Location = new Point(xposOffset + 240, 5);

            rowPanel.Controls.Add(pinNameBox);
            rowPanel.Controls.Add(enableCheck);
            rowPanel.Controls.Add(pinNameBox);
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
                ProjectSettingsManager.SaveKey(projectDirPath, "Device", _device);
                device = _device;
                string dblchk = ProjectSettingsManager.LoadKey(projectDirPath, "Device") as string ?? string.Empty;
                this.Text = $"{projectDirPath} - {dblchk}";
            }
            else
            {
                MessageBox.Show("Project settings file not found or device not selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Form Helper methods


        #region Avilon Edit View Tab


        private void DisplayFileInViewTab(string filePath)
        {
            if (!File.Exists(filePath) || avalonEditor == null)
                return;

            avalonEditor.Text = File.ReadAllText(filePath);

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

        #endregion Avilon Edit View Tab

        #region treeview event handlers

        private void treeView_Project_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is FileInfo fileInfo)
            {
                DisplayFileInViewTab(fileInfo.FullName);
            }
        }

        private IHighlightingDefinition LoadCustomHighlighting(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new XmlTextReader(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        #endregion treeview event handlers
    }

}