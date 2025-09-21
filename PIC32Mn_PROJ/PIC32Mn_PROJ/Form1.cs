using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PIC32Mn_PROJ.classes; // added
// removed: using PIC32Mn_PROJ.classes;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows; // WPF base types (FontWeights, etc.)
using System.Windows.Forms.Integration; // ElementHost
using System.Xml;
using DataFormats = System.Windows.Forms.DataFormats;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {

   
        // Application specific paths
        #region App specific paths    
        string rootPath = string.Empty;
        string packsPath = string.Empty;

        #endregion App specific paths

        // Project properties
        #region Project properties

        public string projectDirPath { get; set; }
        public string projectName { get; set; }
        public string projectVersion { get; set; }
        public string projectDir { get; set; }
        public string projectType { get; set; }

        public string mirror_projectPath { get; set; }

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

        private ToolStripMenuItem leftPasteMenuItem;
        private ToolStripMenuItem rightCopyMenuItem;
        private List<string> rightCopyBufferPaths = new();

        // PowerShell console fields
        private Process? psProcess;
        private RichTextBox psOutput;
        private TextBox psInput;

        // Console-triggered refresh state
        private bool pendingMakeRefresh;
        private TreeNode? pendingRefreshNode;

        // View tab header label to show current filename
        private System.Windows.Forms.Label viewHeaderLabel;

        #region Form Initialization
        public Form1()
        {
            InitializeComponent();

            // path of Form1 C:\Users\davec\GIT\PIC32_M_DEV\PIC32Mn_PROJ\PIC32Mn_PROJ\XML\
            rootPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\";
            //packsPath = "C:\\Program Files\\Microchip\\MPLABX\\v6.25\\packs\\Microchip\\PIC32MZ-EF_DFP\\1.4.168\\";
            packsPath = $"{rootPath}XML\\";

            //paths specific to this application, will have to sort this out

        }

        public void Form1_Load(object sender, EventArgs e)
        {
           
            // Project Initialization
            var savedPath = AppSettings.Default.ProjectPath;
            if (!string.IsNullOrEmpty(savedPath))
            {
                projectDirPath = savedPath;
                PopulateTreeViewWithFoldersAndFiles(projectDirPath);
            }
            var savedMirror = AppSettings.Default.mirror_ProjectPath;
            if (!string.IsNullOrEmpty(savedMirror))
            {
                mirror_projectPath = savedMirror;
                PopulateTreeView(treeView_Right, mirror_projectPath);
            }


            // AvalonEdit setup
            TextEditor avalonEditor = new TextEditor();
            avalonEditor.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            avalonEditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            avalonEditor.ShowLineNumbers = true;
            avalonEditor.SyntaxHighlighting = null;
            avalonEditor.IsReadOnly = false;
            // Add a small top inset so the first line is never visually clipped under host/split borders
            avalonEditor.Margin = new Thickness(0);
            avalonEditor.Padding = new Thickness(0, 4, 0, 0);
            avalonEditor.TextArea.TextView.Margin = new Thickness(0, 2, 0, 0);

            // Add editor context menu (cut/copy/paste/save/close)
            SetupAvalonEditorContextMenu(avalonEditor);

            // Container to control layout inside the View tab
            var viewContainer = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = System.Drawing.Color.White
            };
            tabPage_View.Padding = new Padding(0);
            tabPage_View.Controls.Add(viewContainer);


            // ElementHost fills the remaining space under the header
            ElementHost elementHost = new ElementHost
            {
                BackColor = System.Drawing.Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Dock = DockStyle.Fill,
                Child = avalonEditor
            };

            // IMPORTANT: add Fill first, then Top (header last) so it docks first
            viewContainer.SuspendLayout();
            viewContainer.Controls.Add(elementHost);
            viewContainer.ResumeLayout(true);

            // Split: top editor, bottom PowerShell
            var splitView = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 6
            };
            // Ensure no accidental insets
            splitView.Margin = new Padding(0);
            splitView.Panel1.Padding = new Padding(0);
            splitView.Panel2.Padding = new Padding(0);

            // TOP: editor
            splitView.Panel1.Controls.Add(elementHost);

            // BOTTOM: PowerShell console
            var consolePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White
            };

            // output (fills)
            psOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Consolas", 9f)
            };

            // input (bottom)
            var inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28
            };
            psInput = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            var btnRun = new Button
            {
                Text = "Run",
                Dock = DockStyle.Right,
                Width = 60
            };
            btnRun.Click += (s, e2) => SendShellCommand(psInput.Text);
            psInput.KeyDown += (s, e2) =>
            {
                if (e2.KeyCode == Keys.Enter)
                {
                    e2.SuppressKeyPress = true;
                    SendShellCommand(psInput.Text);
                }
            };

            inputPanel.Controls.Add(psInput);
            inputPanel.Controls.Add(btnRun);

            consolePanel.Controls.Add(psOutput);
            consolePanel.Controls.Add(inputPanel);

            // assemble split
            splitView.Panel2.Controls.Add(consolePanel);

            // IMPORTANT: add split under the header panel you already created
            viewContainer.Controls.Add(splitView);
            viewContainer.Controls.SetChildIndex(splitView, 1); // keep header at index 0

            // start shell process lazily on first use
            this.FormClosed += Form1_FormClosed;

            this.avalonEditor = avalonEditor;
            avalonEditor.TextChanged += (s, e2) => { saveNeeded = true; };
            // Removed LoadDefaults(); since functionality was deleted
            treeView_Project.AfterSelect += treeView_Project_AfterSelect;
            // treeView_Project.
            // Add these lines to enable right-click delete on the dynamic tree
            SetupTreeViewContextMenu();
            treeView_Project.NodeMouseClick += treeView_Project_NodeMouseClick;
            treeView_Project.KeyDown += treeView_Project_KeyDown;

            // Selection preview
            treeView_Project.AfterSelect += treeView_Project_AfterSelect;
            treeView_Right.AfterSelect += treeView_Project_AfterSelect;

            // DRAG SOURCE: right Projects tree
            treeView_Right.ItemDrag += TreeView_Right_ItemDrag;

            // DROP TARGET: left original tree
            treeView_Project.AllowDrop = true;
            treeView_Project.DragEnter += TreeView_Project_DragEnter;
            treeView_Project.DragOver += TreeView_Project_DragOver;
            treeView_Project.DragDrop += TreeView_Project_DragDrop;

            // Keep right-click delete on right tree
            SetupRightTreeViewContextMenu();
            treeView_Right.NodeMouseClick += treeView_Right_NodeMouseClick;
            treeView_Right.KeyDown += treeView_Right_KeyDown;

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
                    // if (treeView_Left != null)
                    //PopulateTreeView(treeView_Left, projectDirPath);

                    // Optional: keep legacy tree populated as well
                    PopulateTreeViewWithFoldersAndFiles(projectDirPath);

                    AppSettings.Default.ProjectPath = projectDirPath;
                    AppSettings.Default.Save();
                }
            }
        }

        private void closeRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Close the right-side project view
            tabControl1.TabPages.Remove(tabPage_Projects);
            projectDirPathRight = string.Empty;
            AppSettings.Default.mirror_ProjectPath = string.Empty;
            AppSettings.Default.Save();
            treeView_Right.Nodes.Clear();
            rightContextNode = null;
            rightCopyBufferPaths.Clear();
            rightCopyMenuItem.Enabled = false;
            rightDeleteMenuItem.Enabled = false;
            // Optionally, re-add the tab for future use
            if (!tabControl1.TabPages.Contains(tabPage_Projects))
            {
                tabControl1.TabPages.Add(tabPage_Projects);
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

            if (avalonEditor == null)
            {
                System.Windows.Forms.MessageBox.Show("Editor is not ready.", "Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // If this is a new, unsaved buffer, prompt Save As
                if (string.IsNullOrWhiteSpace(currentViewFilePath))
                {
                    if (ExecuteSaveAs()) // saved successfully
                    {
                        // Persist the project path for next run (unchanged behavior)
                        if (!string.IsNullOrEmpty(projectDirPath))
                        {
                            AppSettings.Default.ProjectPath = projectDirPath;
                            AppSettings.Default.Save();
                            RefreshAvalonEditor();
                        }
                    }
                    return;
                }

                // Normal Save to existing path
                File.WriteAllText(currentViewFilePath, avalonEditor.Text);
                saveNeeded = false;

                // Persist the project path for next run
                if (!string.IsNullOrEmpty(projectDirPath))
                {
                    AppSettings.Default.ProjectPath = projectDirPath;
                    AppSettings.Default.Save();
                    System.Windows.Forms.MessageBox.Show("File saved.", "Save",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    RefreshAvalonEditor();
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("No project selected.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Save failed:\n{ex.Message}", "Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (ExecuteSaveAs())
                {
                    System.Windows.Forms.MessageBox.Show("File saved.", "Save As",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Save As failed:\n{ex.Message}", "Save As",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
           this.Close();
        }

        // Reusable Save As implementation used by both Save and Save As
        private bool ExecuteSaveAs()
        {
            if (avalonEditor == null)
                throw new InvalidOperationException("Editor is not ready.");

            // Initial directory preference: current file dir -> project root -> Documents
            var initialDir =
                (!string.IsNullOrEmpty(currentViewFilePath) && Directory.Exists(Path.GetDirectoryName(currentViewFilePath!)))
                    ? Path.GetDirectoryName(currentViewFilePath)!
                    : (!string.IsNullOrEmpty(projectDirPath) && Directory.Exists(projectDirPath))
                        ? projectDirPath
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var currentName = !string.IsNullOrEmpty(currentViewFilePath)
                ? Path.GetFileName(currentViewFilePath)
                : "untitled.c";

            var sfd = new System.Windows.Forms.SaveFileDialog
            {
                InitialDirectory = initialDir,
                FileName = currentName,
                Filter =
                    "C/C header/source (*.c;*.h)|*.c;*.h|" +
                    "Assembly (*.s;*.asm)|*.s;*.asm|" +
                    "Makefiles (Makefile;*.mk)|Makefile;*.mk|" +
                    "JSON (*.json)|*.json|" +
                    "XML (*.xml)|*.xml|" +
                    "All files (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true,
                ValidateNames = true
            };

            var ext = Path.GetExtension(currentName);
            if (!string.IsNullOrEmpty(ext))
                sfd.DefaultExt = ext.TrimStart('.');

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return false;

            // Write file
            File.WriteAllText(sfd.FileName, avalonEditor.Text, Encoding.UTF8);

            // Update context
            currentViewFilePath = sfd.FileName;
            saveNeeded = false;

            // Re-apply highlighting based on the new filename/extension
            DisplayFileInViewTab(currentViewFilePath);

            // Incrementally update the tree ONLY if saved inside the project root (no collapse)
            if (!string.IsNullOrEmpty(projectDirPath) && IsUnderProjectRoot(currentViewFilePath))
            {
                AddOrUpdateFileNode(treeView_Project, projectDirPath, currentViewFilePath);
            }

            return true;
        }

        // Incrementally add/update a file node under the project tree without collapsing the whole tree
        private void AddOrUpdateFileNode(TreeView tree, string rootFolderPath, string filePath)
        {
            if (tree.Nodes.Count == 0 || string.IsNullOrEmpty(rootFolderPath) || string.IsNullOrEmpty(filePath))
                return;

            // Root node is the project root
            var rootNode = tree.Nodes[0];
            if (rootNode.Tag is not DirectoryInfo rootDi)
                return;

            // Make sure we are operating under the same root
            var normRoot = Path.GetFullPath(rootFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normFile = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!normFile.StartsWith(normRoot, StringComparison.OrdinalIgnoreCase))
                return;

            // Find or create the directory chain under the root
            var rel = Path.GetRelativePath(normRoot, normFile);
            var dirRel = Path.GetDirectoryName(rel) ?? string.Empty;
            var segments = dirRel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            TreeNode current = rootNode;
            string currentPath = normRoot;

            foreach (var seg in segments)
            {
                currentPath = Path.Combine(currentPath, seg);
                // Look for existing directory child by full path
                TreeNode? dirNode = null;
                foreach (TreeNode child in current.Nodes)
                {
                    if (child.Tag is DirectoryInfo cdi && string.Equals(
                            Path.GetFullPath(cdi.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                            currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        dirNode = child;
                        break;
                    }
                }

                if (dirNode == null)
                {
                    var di = new DirectoryInfo(currentPath);
                    if (!di.Exists) Directory.CreateDirectory(di.FullName);
                    dirNode = new TreeNode(di.Name) { Tag = di };
                    current.Nodes.Add(dirNode);
                }

                current = dirNode;
            }

            // Add or update the file node under the final directory
            var fileName = Path.GetFileName(normFile);
            TreeNode? fileNode = null;
            foreach (TreeNode child in current.Nodes)
            {
                if (child.Tag is FileInfo cfi && string.Equals(
                        Path.GetFullPath(cfi.FullName).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        normFile, StringComparison.OrdinalIgnoreCase))
                {
                    fileNode = child;
                    break;
                }
            }

            if (fileNode == null)
            {
                var fi = new FileInfo(normFile);
                fileNode = new TreeNode(fi.Name) { Tag = fi };
                current.Nodes.Add(fileNode);
            }
            else
            {
                // Update node text/tag in case it changed (rare, but safe)
                fileNode.Text = fileName;
                fileNode.Tag = new FileInfo(normFile);
            }

            // Optionally expand just the parent chain to reveal the new file
            current.Expand();
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }


        private void mCCStandaloneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Device selection removed in this simplified branch
            scripts scr = new scripts();
            scr.launch("startMcc", mirror_projectPath);
            scr.alert_changes(mirror_projectPath);
        }

        private void mPLABXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Device selection removed in this simplified branch
            scripts scr = new scripts();
            scr.launch("startMplabX", mirror_projectPath);
            scr.alert_changes(mirror_projectPath);
        }

        private void vSCodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var scr = new scripts();
            scr.launch("vscode", projectDirPath);
            scr.alert_changes(projectDirPath);
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
                        //File.Copy($"{rootPath}dependancies\\project_files\\main.c", Path.Combine(newProjectPath, "srcs", "main.c"));
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

                }
            }
            // Prompt to select mirror project
            var res = System.Windows.Forms.MessageBox.Show("Now select the mirror project for your new project.", "Mirror Project", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);

            if (res == DialogResult.No)
                return;

            var mirror = (string.IsNullOrEmpty(mirror_projectPath)) ? "C:\\" : mirror_projectPath;
            OpenFolderDialog ofd = new OpenFolderDialog
            {
                Title = "Select the device mirror project for the " + projectName,
                InitialDirectory = mirror
            };
            ofd.ShowDialog();
            if (!string.IsNullOrEmpty(ofd.FolderName) && Directory.Exists(ofd.FolderName))
            {
                mirror_projectPath = ofd.FolderName;
                AppSettings.Default.mirror_ProjectPath = mirror_projectPath;
                AppSettings.Default.Save();
                PopulateTreeView(treeView_Right, mirror_projectPath);
            }
        }

        // Make the Generate click handler await the async generator and add guards
        private async void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Placeholder: generation functionality removed/under construction
            await System.Threading.Tasks.Task.CompletedTask;
            System.Windows.Forms.MessageBox.Show("Generate is not implemented in this branch.");
        }

        private void cSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (avalonEditor == null)
                {
                    System.Windows.Forms.MessageBox.Show("Editor is not ready.", "New C Source",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create an unsaved buffer with a minimal template
                string template = C_FileBuilder.BuildCSourceTemplate("untitled.c");
                avalonEditor.Text = template;

                // Mark as unsaved buffer
                currentViewFilePath = string.Empty;
                saveNeeded = true;

                // Apply C highlighting for the buffer
                EnsureCustomCHighlightingRegistered();
                avalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");

                // Update header title
                UpdateViewHeader("untitled.c");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Create C source failed:\n{ex.Message}", "New C Source",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        private void headerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (avalonEditor == null)
                {
                    System.Windows.Forms.MessageBox.Show("Editor is not ready.", "New Header",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create an unsaved buffer with a minimal header template
                string template = C_FileBuilder.BuildHeaderTemplate("untitled.h");
                avalonEditor.Text = template;

                // Mark as unsaved buffer
                currentViewFilePath = string.Empty;
                saveNeeded = true;

                // Apply C highlighting for the buffer
                EnsureCustomCHighlightingRegistered();
                avalonEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");

                // Update header title
                UpdateViewHeader("untitled.h");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Create header failed:\n{ex.Message}", "New Header",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    AppSettings.Default.mirror_ProjectPath = projectDirPathRight;
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

        // Update SetupTreeViewContextMenu to include Paste
        private void SetupTreeViewContextMenu()
        {
            treeContextMenu = new ContextMenuStrip();

            leftPasteMenuItem = new ToolStripMenuItem("Paste", null, (s, e) => LeftPasteFromRightClipboard());
            treeContextMenu.Items.Add(leftPasteMenuItem);

            deleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedNode());
            treeContextMenu.Items.Add(deleteMenuItem);
        }

        private void treeView_Project_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            treeView_Project.SelectedNode = e.Node;
            contextNode = e.Node;

            // Enable paste when buffer has content
            leftPasteMenuItem.Enabled = rightCopyBufferPaths != null && rightCopyBufferPaths.Count > 0;

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
                        UpdateViewHeader(null);
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
            UpdateViewHeader(filePath);
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

        private void CloseCurrentView()
        {
            if (avalonEditor == null) return;

            // optional: prompt to save if needed
            // if (saveNeeded) { ... }

            avalonEditor.Text = string.Empty;
            currentViewFilePath = string.Empty;
            saveNeeded = false;
            avalonEditor.SyntaxHighlighting = null;
            UpdateViewHeader(null);
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

        // Drag from RIGHT Projects tree
        private void TreeView_Right_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            if (e.Item is not TreeNode node) return;
            var path = GetNodePath(node);
            if (string.IsNullOrEmpty(path)) return;

            var data = new System.Windows.Forms.DataObject();
            data.SetData(DataFormats.FileDrop, new[] { path });
            data.SetData("SourceRootPath", projectDirPathRight ?? string.Empty);
            DoDragDrop(data, System.Windows.Forms.DragDropEffects.Copy);
        }

        // Drop onto right tree
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

        // Drop onto LEFT original tree
        private void TreeView_Project_DragEnter(object? sender, System.Windows.Forms.DragEventArgs e)
        {
            e.Effect = (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                ? System.Windows.Forms.DragDropEffects.Copy
                : System.Windows.Forms.DragDropEffects.None;
        }

        private void TreeView_Project_DragOver(object? sender, System.Windows.Forms.DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false))
            {
                e.Effect = System.Windows.Forms.DragDropEffects.None;
                return;
            }

            var tv = treeView_Project;
            var targetNode = tv.GetNodeAt(tv.PointToClient(new System.Drawing.Point(e.X, e.Y)));
            // Dropping on a file copies alongside; on a folder (or empty) copies inside -> valid
            e.Effect = System.Windows.Forms.DragDropEffects.Copy;
        }

        private void TreeView_Project_DragDrop(object? sender, System.Windows.Forms.DragEventArgs e)
        {
            if (!(e.Data?.GetDataPresent(DataFormats.FileDrop) ?? false)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;

            var tv = treeView_Project;
            var clientPoint = tv.PointToClient(new System.Drawing.Point(e.X, e.Y));
            var targetNode = tv.GetNodeAt(clientPoint);

            var targetDir = GetDropTargetDirectory(targetNode, projectDirPath);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;

            var policy = OverwritePolicy.Ask;

            try
            {
                foreach (var srcPath in files)
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        CopyDirectoryWithPrompt(srcDir.FullName, destDir, ref policy);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        TryCopyFileWithPrompt(srcPath, destFile, ref policy);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // user cancelled; stop
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Copy failed:\n{ex.Message}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Refresh only the affected directory node (keeps expansion state)
            var dirNodeToRefresh = GetDirectoryNodeForDrop(targetNode);
            if (dirNodeToRefresh != null)
                RepopulateDirectoryNode(dirNodeToRefresh);
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

            rightCopyMenuItem = new ToolStripMenuItem("Copy", null, (s, e) => RightCopySelectedNode());
            rightTreeContextMenu.Items.Add(rightCopyMenuItem);

            rightDeleteMenuItem = new ToolStripMenuItem("Delete", null, (s, e) => DeleteSelectedNodeRight());
            rightTreeContextMenu.Items.Add(rightDeleteMenuItem);
        }

        private void treeView_Right_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            treeView_Right.SelectedNode = e.Node;
            rightContextNode = e.Node;

            // Prevent deleting the root node
            //rightDeleteMenuItem.Enabled = e.Node.Parent != null;

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
                        UpdateViewHeader(null);
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

        // Add this helper to copy directories while honoring the overwrite policy
        private static void CopyDirectoryWithPrompt(string sourceDir, string destDir, ref OverwritePolicy policy)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;

            Directory.CreateDirectory(destDir);

            foreach (var file in src.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(destDir, file.Name);
                TryCopyFileWithPrompt(file.FullName, target, ref policy);
            }

            foreach (var dir in src.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                var subDest = Path.Combine(destDir, dir.Name);
                CopyDirectoryWithPrompt(dir.FullName, subDest, ref policy);
            }
        }

        private void RightCopySelectedNode()
        {
            var node = rightContextNode ?? treeView_Right.SelectedNode;
            if (node == null || node.Tag is not FileSystemInfo fsi) return;

            rightCopyBufferPaths = new List<string> { fsi.FullName };
            // Optionally: notify user
            // System.Windows.Forms.MessageBox.Show($"Copied: {fsi.FullName}", "Copy", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LeftPasteFromRightClipboard()
        {
            if (rightCopyBufferPaths == null || rightCopyBufferPaths.Count == 0) return;
            if (string.IsNullOrEmpty(projectDirPath) || !Directory.Exists(projectDirPath))
            {
                System.Windows.Forms.MessageBox.Show("No LEFT project root is open.", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var node = contextNode ?? treeView_Project.SelectedNode;
            var targetDir = GetDropTargetDirectory(node, projectDirPath);
            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return;

            var policy = OverwritePolicy.Ask;

            try
            {
                foreach (var srcPath in rightCopyBufferPaths)
                {
                    if (Directory.Exists(srcPath))
                    {
                        var srcDir = new DirectoryInfo(srcPath);
                        var destDir = Path.Combine(targetDir, srcDir.Name);
                        CopyDirectoryWithPrompt(srcDir.FullName, destDir, ref policy);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        TryCopyFileWithPrompt(srcPath, destFile, ref policy);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // user cancelled; stop
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Paste failed:\n{ex.Message}", "Paste",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Refresh only the affected directory node (keeps expansion state)
            var dirNodeToRefresh = GetDirectoryNodeForDrop(node);
            if (dirNodeToRefresh != null)
                RepopulateDirectoryNode(dirNodeToRefresh);
        }

        private TreeNode GetDirectoryNodeForDrop(TreeNode node)
        {
            // Assuming the node represents a directory, return it directly.
            return node;
        }

        // Add this helper method to the Form1 class to fix CS0103
        private void RepopulateDirectoryNode(TreeNode dirNode)
        {
            if (dirNode == null || dirNode.Tag is not DirectoryInfo di) return;

            dirNode.Nodes.Clear();
            AddDirectoriesAndFiles(di, dirNode);
            dirNode.Expand();
        }

        // Add this enum anywhere in the Form1 class (e.g., near other helpers)
        private enum OverwritePolicy
        {
            Ask,
            YesToAll,
            NoToAll
        }

        // Add this helper: copy with per-file prompt, remembering policy across the operation
        private static bool TryCopyFileWithPrompt(string srcFile, string destFile, ref OverwritePolicy policy)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            // If there's no existing file, just copy
            if (!File.Exists(destFile))
            {
                File.Copy(srcFile, destFile, overwrite: true);
                File.SetAttributes(destFile, FileAttributes.Normal);
                return true;
            }

            // Existing file: decide based on policy
            switch (policy)
            {
                case OverwritePolicy.YesToAll:
                    File.Copy(srcFile, destFile, overwrite: true);
                    File.SetAttributes(destFile, FileAttributes.Normal);
                    return true;

                case OverwritePolicy.NoToAll:
                    return false;

                case OverwritePolicy.Ask:
                default:
                    // Ask for this file
                    var res = System.Windows.Forms.MessageBox.Show(
                        $"The file already exists:\n{destFile}\nDo you want to replace it?",
                        "Copy and Replace",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                    if (res == DialogResult.Cancel)
                        throw new OperationCanceledException("User cancelled copy.");

                    if (res == DialogResult.Yes)
                    {
                        // Optional: ask to apply to all remaining conflicts
                        var applyAll = System.Windows.Forms.MessageBox.Show(
                            "Apply this choice (Replace) to all remaining existing files?",
                            "Apply to All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);

                        if (applyAll == DialogResult.Yes)
                            policy = OverwritePolicy.YesToAll;

                        File.Copy(srcFile, destFile, overwrite: true);
                        File.SetAttributes(destFile, FileAttributes.Normal);
                        return true;
                    }
                    else
                    {
                        // res == No
                        var applyAll = System.Windows.Forms.MessageBox.Show(
                            "Apply this choice (Skip) to all remaining existing files?",
                            "Apply to All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2);

                        if (applyAll == DialogResult.Yes)
                            policy = OverwritePolicy.NoToAll;

                        return false;
                    }
            }
        }


        // Add inside the Form1 class, near the other shell helpers
        private void AppendConsoleLine(string line)
        {
            if (psOutput == null) return;

            if (psOutput.InvokeRequired)
            {
                psOutput.BeginInvoke(new Action<string>(AppendConsoleLine), line);
                return;
            }

            psOutput.AppendText(line + Environment.NewLine);
            psOutput.SelectionStart = psOutput.TextLength;
            psOutput.ScrollToCaret();
        }
        private void EnsureShellStarted()
        {
            if (psProcess != null && !psProcess.HasExited) return;

            var exe = GetAvailablePowerShell(); // tries pwsh, then powershell.exe
            var cwd = GetLeftWorkingDirectory(); // <-- get working dir from left tree
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "-NoLogo -NoExit",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = cwd // <-- start in that directory
            };

            psProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            psProcess.OutputDataReceived += (s, e) => { if (e.Data != null) OnShellOutput(e.Data); };
            psProcess.ErrorDataReceived  += (s, e) => { if (e.Data != null) OnShellOutput(e.Data); };
            psProcess.Exited += (s, e) => AppendConsoleLine("[process exited]");

            if (psProcess.Start())
            {
                psProcess.BeginOutputReadLine();
                psProcess.BeginErrorReadLine();
                AppendConsoleLine($"Started {exe} (cwd: {cwd})");
            }
            else
            {
                AppendConsoleLine("Failed to start PowerShell.");
            }
        }

        // Add this helper inside the Form1 class (e.g., near the other shell helpers)
        private string GetLeftWorkingDirectory()
        {
            try
            {
                var node = treeView_Project?.SelectedNode;

                // Use selected directory, or the directory of the selected file
                switch (node?.Tag)
                {
                    case DirectoryInfo di when di.Exists:
                        return di.FullName;
                    case FileInfo fi when File.Exists(fi.FullName):
                        return Path.GetDirectoryName(fi.FullName)!;
                }
            }
            catch { /* ignore */ }

            // Fallback to the left project root, then current process dir
            if (!string.IsNullOrEmpty(projectDirPath) && Directory.Exists(projectDirPath))
                return projectDirPath;

            return Environment.CurrentDirectory;
        }



        private static string GetAvailablePowerShell()
        {
            // Prefer pwsh if available, fallback to Windows PowerShell
            var pwsh = "pwsh.exe";
            var winps = "powershell.exe";
            try
            {
                // Try to resolve pwsh on PATH
                using var probe = Process.Start(new ProcessStartInfo
                {
                    FileName = pwsh,
                    Arguments = "-v",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                if (probe != null) return pwsh;
            }
            catch { /* ignore */ }
            return winps;
        }


        private void OnShellOutput(string line)
        {
            AppendConsoleLine(line);
            if (pendingMakeRefresh && line.Contains("__MAKE_DONE__", StringComparison.Ordinal))
            {
                pendingMakeRefresh = false;
                // Refresh the captured directory node on the UI thread
                if (pendingRefreshNode != null)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => SafeRefreshNode(pendingRefreshNode)));
                    else
                        SafeRefreshNode(pendingRefreshNode);
                }
            }
        }

        // Ensure the node is a directory node and refresh it
        private void SafeRefreshNode(TreeNode node)
        {
            var dirNode = node;
            if (dirNode.Tag is FileInfo && dirNode.Parent != null)
                dirNode = dirNode.Parent;

            if (dirNode.Tag is DirectoryInfo di && Directory.Exists(di.FullName))
                RepopulateDirectoryNode(dirNode);
        }

        // Get current directory node from left tree selection
        private TreeNode? GetSelectedDirectoryNode()
        {
            var node = treeView_Project?.SelectedNode;
            if (node == null) return null;
            if (node.Tag is DirectoryInfo) return node;
            if (node.Tag is FileInfo) return node.Parent;
            return null;
        }


        private void SendShellCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            EnsureShellStarted();

            try
            {
                var trimmed = text.Trim();
                var cwd = GetLeftWorkingDirectory();
                var psLiteral = cwd.Replace("'", "''");

                AppendConsoleLine($"> {text}");
                psProcess!.StandardInput.WriteLine($"Set-Location -LiteralPath '{psLiteral}'");

                if (string.Equals(trimmed, "make build_dir", StringComparison.OrdinalIgnoreCase))
                {
                    // capture which node to refresh and send sentinel
                    pendingRefreshNode = GetSelectedDirectoryNode();
                    pendingMakeRefresh = true;
                    psProcess.StandardInput.WriteLine("make build_dir; Write-Host __MAKE_DONE__");
                }
                else
                {
                    psProcess.StandardInput.WriteLine(text);
                }

                psProcess.StandardInput.Flush();
                psInput.Clear();
            }
            catch (Exception ex)
            {
                AppendConsoleLine($"[error] {ex.Message}");
            }
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                if (psProcess != null && !psProcess.HasExited)
                {
                    psProcess.StandardInput.WriteLine("exit");
                    if (!psProcess.WaitForExit(1500))
                        psProcess.Kill(entireProcessTree: true);
                }
                psProcess?.Dispose();
            }
            catch { /* ignore */ }
        }

        // Update the view tab header with current filename (or clear when null)
        private void UpdateViewHeader(string? path)
        {
            if (viewHeaderLabel == null) return;
            viewHeaderLabel.Text = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
            // Optional: store full path in Tag for debugging/inspection
            viewHeaderLabel.Tag = path ?? string.Empty;
        }

        // Build a WPF context menu for the AvalonEdit control with Cut/Copy/Paste/Save/Close
        private void SetupAvalonEditorContextMenu(TextEditor editor)
        {
            var cm = new System.Windows.Controls.ContextMenu();

            var miCut = new System.Windows.Controls.MenuItem { Header = "Cut" };
            miCut.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Cut;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea))
                    cmd.Execute(null, editor.TextArea);
            };

            var miCopy = new System.Windows.Controls.MenuItem { Header = "Copy" };
            miCopy.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Copy;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea))
                    cmd.Execute(null, editor.TextArea);
            };

            var miPaste = new System.Windows.Controls.MenuItem { Header = "Paste" };
            miPaste.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Paste;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea))
                    cmd.Execute(null, editor.TextArea);
            };

            var miSave = new System.Windows.Controls.MenuItem { Header = "Save" };
            miSave.Click += (s, e) => saveToolStripMenuItem_Click(this, EventArgs.Empty);

            var miClose = new System.Windows.Controls.MenuItem { Header = "Close" };
            miClose.Click += (s, e) => CloseCurrentView();

            cm.Items.Add(miCut);
            cm.Items.Add(miCopy);
            cm.Items.Add(miPaste);
            cm.Items.Add(new System.Windows.Controls.Separator());
            cm.Items.Add(miSave);
            cm.Items.Add(miClose);

            cm.Opened += (s, e) =>
            {
                bool hasSel = editor?.TextArea?.Selection?.IsEmpty == false;
                miCut.IsEnabled = hasSel && !editor.IsReadOnly;
                miCopy.IsEnabled = hasSel;
                miPaste.IsEnabled = !editor.IsReadOnly && System.Windows.Clipboard.ContainsText();
            };

            editor.ContextMenu = cm;
        }
    }
}