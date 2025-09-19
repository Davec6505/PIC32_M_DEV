using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

using Microsoft.WindowsAPICodePack.Dialogs;
// removed: using PIC32Mn_PROJ.classes;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Drawing;
using System.Windows; // WPF base types (FontWeights, etc.)
using System.Windows.Forms; // WinForms
using System.Windows.Forms.Integration; // ElementHost
using System.Xml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text; // added
using System.Collections.Generic; // added
using System.Linq;
using DataFormats = System.Windows.Forms.DataFormats; // added

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {

        // Removed deleted dependencies to simplify build
        // ConfigLoader configLoader = new();
        // Modules mods;
        // pins pins;

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
        private ContextMenuStrip treeContextMenu;
        private ToolStripMenuItem deleteMenuItem;
        private TreeNode? contextNode;
        private string projectDirPathRight { get; set; } // Added for right project directory

        private ContextMenuStrip rightTreeContextMenu;
        private ToolStripMenuItem rightDeleteMenuItem;
        private TreeNode? rightContextNode;

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
            var savedPath = AppSettings.Default.ProjectPath;

            if (!string.IsNullOrEmpty(savedPath))
            {
                projectDirPath = savedPath;
                PopulateTreeViewWithFoldersAndFiles(projectDirPath);
            }

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
            // Removed LoadDefaults(); since functionality was deleted
            treeView_Project.AfterSelect += treeView_Project_AfterSelect;

            // Add these lines to enable right-click delete on the dynamic tree
            SetupTreeViewContextMenu();
            treeView_Project.NodeMouseClick += treeView_Project_NodeMouseClick;
            treeView_Project.KeyDown += treeView_Project_KeyDown;

            // Hook both trees to the same selection preview (optional, previews in View tab)
            treeView_Left.AfterSelect += treeView_Project_AfterSelect;
            treeView_Right.AfterSelect += treeView_Project_AfterSelect;

            // Drag from left tree
            treeView_Left.ItemDrag += TreeView_Left_ItemDrag;

            // Drop onto right tree
            treeView_Right.DragEnter += TreeView_Right_DragEnter;
            treeView_Right.DragOver += TreeView_Right_DragOver;
            treeView_Right.DragDrop += TreeView_Right_DragDrop;

            ApplyCHighlightingColors(avalonEditor);

            // In Form1_Load, after you wire DnD on treeView_Right
            SetupRightTreeViewContextMenu();
            treeView_Right.NodeMouseClick += treeView_Right_NodeMouseClick;
            treeView_Right.KeyDown += treeView_Right_KeyDown;
        }

        #endregion Form Initialization

        // Project menustrip items
        #region  menu items
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select the LEFT project folder";
                dialog.InitialDirectory = rootPath;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrEmpty(dialog.FileName))
                {
                    projectDirPath = dialog.FileName;

                    // Populate the new LEFT tree (Projects tab)
                    if (treeView_Left != null)
                        PopulateTreeView(treeView_Left, projectDirPath);

                    // Optional: keep legacy tree populated as well
                    PopulateTreeViewWithFoldersAndFiles(projectDirPath);

                    AppSettings.Default.ProjectPath = projectDirPath;
                    AppSettings.Default.Save();
                }
            }
        }


        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Try to recover current project/device if fields are empty
            if (string.IsNullOrEmpty(projectDirPath))
            {
                var savedPath = AppSettings.Default.ProjectPath;
                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                {
                    projectDirPath = savedPath;
                }
            }


            // Save the file in the editor if needed
            if (!string.IsNullOrEmpty(currentViewFilePath) && avalonEditor != null)
            {
                File.WriteAllText(currentViewFilePath, avalonEditor.Text);
            }

            // Persist the project path for next run
            if (!string.IsNullOrEmpty(projectDirPath))
            {
                AppSettings.Default.ProjectPath = projectDirPath;
                AppSettings.Default.Save();
                System.Windows.Forms.MessageBox.Show("Project settings saved.", "Save", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                RefreshAvalonEditor();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("No project selected.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }


        private void deviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Device selection removed in this simplified branch
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
                    AppSettings.Default.ProjectPath = projectDirPath;
                    AppSettings.Default.Save();

                    // Removed: GetDevice();

                }
            }
        }

        // Make the Generate click handler await the async generator and add guards
        private async void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Placeholder: generation functionality removed/under construction
            await System.Threading.Tasks.Task.CompletedTask;
            System.Windows.Forms.MessageBox.Show("Generate is not implemented in this branch.");
        }

        private void openRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                dialog.Title = "Select the RIGHT project folder";
                dialog.InitialDirectory = rootPath;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrEmpty(dialog.FileName))
                {
                    projectDirPathRight = dialog.FileName;
                    PopulateTreeView(treeView_Right, projectDirPathRight);
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
                if (colorEntry== null) return;
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

        #endregion Panel1 Config groupBox

        #endregion System tab controls

        // Generic population for a tree
        private void PopulateTreeView(TreeView tree, string rootFolderPath)
        {
            tree.BeginUpdate();
            try
            {
                tree.Nodes.Clear();
                var rootDirectoryInfo = new DirectoryInfo(rootFolderPath);
                var rootNode = new TreeNode(rootDirectoryInfo.Name) { Tag = rootDirectoryInfo };
                tree.Nodes.Add(rootNode);
                AddDirectoriesAndFiles(rootDirectoryInfo, rootNode);
                rootNode.Expand();
            }
            finally
            {
                tree.EndUpdate();
            }
        }

        // Drag from left tree
        private void TreeView_Left_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node)
            {
                var path = GetNodePath(node);
                if (string.IsNullOrEmpty(path)) return;

                // FIX: Use System.Windows.Forms.DataObject, not System.Security.Cryptography.Xml.DataObject
                var data = new System.Windows.Forms.DataObject();
                data.SetData(DataFormats.FileDrop, new[] { path }); // standard file drop
                data.SetData("SourceRootPath", projectDirPath ?? string.Empty); // custom, if needed later

                DoDragDrop(data, System.Windows.Forms.DragDropEffects.Copy);
            }
        }

        // Right tree drag handlers
        private void TreeView_Right_DragEnter(object? sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = System.Windows.Forms.DragDropEffects.Copy; // WinForms DragDropEffects
            else
                e.Effect = System.Windows.Forms.DragDropEffects.None;
        }

        private void TreeView_Right_DragOver(object? sender, System.Windows.Forms.DragEventArgs e)
        {
            TreeView_SetCopyEffectIfValid(treeView_Right, e);
        }

        private void TreeView_SetCopyEffectIfValid(TreeView tv, System.Windows.Forms.DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false))
            {
                e.Effect = System.Windows.Forms.DragDropEffects.None;
                return;
            }

            var targetNode = tv.GetNodeAt(tv.PointToClient(new System.Drawing.Point(e.X, e.Y)));
            if (targetNode?.Tag is FileInfo)
            {
                // dropping on a file: copy alongside it -> valid
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
            }
            else
            {
                // folder or empty (root): valid as well
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
            }
        }

        private void TreeView_Right_DragDrop(object? sender, System.Windows.Forms.DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return;
            var files = (string[])e.Data!.GetData(DataFormats.FileDrop)!;

            // Find drop target
            var tv = treeView_Right;
            var clientPoint = tv.PointToClient(new System.Drawing.Point(e.X, e.Y));
            var targetNode = tv.GetNodeAt(clientPoint);

            var targetDir = GetDropTargetDirectory(targetNode, projectDirPathRight);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;

            foreach (var srcPath in files)
            {
                try
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        CopyDirectory(srcDir.FullName, destDir);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                        File.Copy(srcPath, destFile, overwrite: true);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show($"Copy failed:\n{ex.Message}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Refresh right tree after copy
            if (!string.IsNullOrEmpty(projectDirPathRight))
                PopulateTreeView(treeView_Right, projectDirPathRight);
        }

        // Helpers
        private static string GetDropTargetDirectory(TreeNode? targetNode, string rootPath)
        {
            if (targetNode == null || targetNode.Tag == null)
                return rootPath;

            return targetNode.Tag switch
            {
                DirectoryInfo di => di.FullName,
                FileInfo fi => Path.GetDirectoryName(fi.FullName) ?? rootPath,
                _ => rootPath
            };
        }

        // Add these methods (e.g., after SetupTreeViewContextMenu)
        private void SetupRightTreeViewContextMenu()
        {
            rightTreeContextMenu = new ContextMenuStrip();
            rightDeleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedNodeRight());
            rightTreeContextMenu.Items.Add(rightDeleteMenuItem);
        }

        private void treeView_Right_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            treeView_Right.SelectedNode = e.Node;
            rightContextNode = e.Node;

            // Prevent deleting the root node
            rightDeleteMenuItem.Enabled = e.Node.Parent != null;
            rightTreeContextMenu.Show(treeView_Right, e.Location);
        }

        private void treeView_Right_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedNodeRight();
                e.Handled = true;
            }
        }

        private void DeleteSelectedNodeRight()
        {
            var node = rightContextNode ?? treeView_Right.SelectedNode;
            rightContextNode = null;
            if (node == null) return;

            // Disallow deleting the root (top-level) node
            if (node.Parent == null)
            {
                System.Windows.Forms.MessageBox.Show("Cannot delete the project root folder.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(projectDirPathRight) || !Directory.Exists(projectDirPathRight))
            {
                System.Windows.Forms.MessageBox.Show("No RIGHT project root is open.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (node.Tag is FileInfo fi)
                {
                    if (!IsUnderRoot(fi.FullName, projectDirPathRight))
                    {
                        System.Windows.Forms.MessageBox.Show("Blocked: outside RIGHT project root.", "Delete",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    if (!IsUnderRoot(di.FullName, projectDirPathRight))
                    {
                        System.Windows.Forms.MessageBox.Show("Blocked: outside RIGHT project root.", "Delete",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                System.Windows.Forms.MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Small helper to validate a path is under a given root (case-insensitive)
        private static bool IsUnderRoot(string path, string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return false;
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        // Add this helper method to the Form1 class to fix CS0103
        private string GetNodePath(TreeNode node)
        {
            if (node?.Tag is FileSystemInfo fsi)
                return fsi.FullName;
            return string.Empty;
        }

        // Recursively copy a directory tree (copy-only, preserves structure)
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;

            Directory.CreateDirectory(destDir);

            // Copy files in current directory
            foreach (var file in src.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(destDir, file.Name);
                file.CopyTo(target, overwrite: true);
                File.SetAttributes(target, FileAttributes.Normal);
            }

            // Recurse into subdirectories
            foreach (var dir in src.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                CopyDirectory(dir.FullName, Path.Combine(destDir, dir.Name));
            }
        }

    }
}