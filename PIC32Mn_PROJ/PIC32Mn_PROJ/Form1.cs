using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.WindowsAPICodePack.Dialogs;
using PIC32Mn_PROJ.classes;
using PIC32Mn_PROJ.Services.Abstractions;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows; // WPF base types
using System.Windows.Forms; // WinForms
using System.Windows.Forms.Integration; // ElementHost
using DataFormats = System.Windows.Forms.DataFormats;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {
        // Injected services
        private readonly ISettingsService _settings;
        private readonly IDialogService _dialogs;
        private readonly IProjectTreeService _treeSvc;
        private readonly IFileSystemService _fs;
        private readonly IHighlightingService _highlightingSvc;
        private readonly IShellService _shellSvc;
        private readonly IEditorService _editorSvc;

        // App specific paths
        string rootPath = string.Empty;
        string packsPath = string.Empty;

        // Project properties
        public string projectDirPath { get; set; }
        public string projectName { get; set; }
        public string projectVersion { get; set; }
        public string projectDir { get; set; }
        public string projectType { get; set; }
        public string mirror_projectPath { get; set; }
        public string device { get; set; }
        public bool saveNeeded { get; set; } = false;

        // View/editor
        private TextEditor avalonEditor;
        private string currentViewFilePath;

        // Tree and context
        private ContextMenuStrip treeContextMenu;
        private ToolStripMenuItem deleteMenuItem;
        private TreeNode? contextNode;
        private string projectDirPathRight { get; set; }

        private ContextMenuStrip rightTreeContextMenu;
        private ToolStripMenuItem rightDeleteMenuItem;
        private TreeNode? rightContextNode;

        private ToolStripMenuItem leftPasteMenuItem;
        private ToolStripMenuItem rightCopyMenuItem;
        private List<string> rightCopyBufferPaths = new();

        // Shell
        private RichTextBox psOutput;
        private TextBox psInput;
        private bool pendingMakeRefresh;
        private TreeNode? pendingRefreshNode;
        private bool _shellOutputSubscribed;

        // View header
        private System.Windows.Forms.Label viewHeaderLabel;

        public Form1(
            ISettingsService settings,
            IDialogService dialogs,
            IProjectTreeService treeSvc,
            IFileSystemService fs,
            IHighlightingService highlightingSvc,
            IShellService shellSvc,
            IEditorService editorSvc)
        {
            _settings = settings;
            _dialogs = dialogs;
            _treeSvc = treeSvc;
            _fs = fs;
            _highlightingSvc = highlightingSvc;
            _shellSvc = shellSvc;
            _editorSvc = editorSvc;

            InitializeComponent();
            rootPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\";
            packsPath = $"{rootPath}XML\\";
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            // Restore paths via settings service
            var savedPath = _settings.ProjectPath;
            if (!string.IsNullOrEmpty(savedPath))
            {
                projectDirPath = savedPath;
                _treeSvc.PopulateLeft(treeView_Project, projectDirPath);
            }
            var savedMirror = _settings.MirrorProjectPath;
            if (!string.IsNullOrEmpty(savedMirror))
            {
                mirror_projectPath = savedMirror;
                _treeSvc.Populate(treeView_Right, mirror_projectPath);
            }

            // AvalonEdit setup
            TextEditor avalonEditor = new TextEditor
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                ShowLineNumbers = true,
                SyntaxHighlighting = null,
                IsReadOnly = false,
                Margin = new Thickness(0),
                Padding = new Thickness(0, 4, 0, 0)
            };
            avalonEditor.TextArea.TextView.Margin = new Thickness(0, 2, 0, 0);
            SetupAvalonEditorContextMenu(avalonEditor);

            // Container inside View tab
            var viewContainer = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = System.Drawing.Color.White
            };
            tabPage_View.Padding = new Padding(0);
            tabPage_View.Controls.Add(viewContainer);

            // ElementHost for editor
            ElementHost elementHost = new ElementHost
            {
                BackColor = System.Drawing.Color.White,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Dock = DockStyle.Fill,
                Child = avalonEditor
            };

            viewContainer.SuspendLayout();
            viewContainer.Controls.Add(elementHost);
            viewContainer.ResumeLayout(true);

            // Split: editor (top) + console (bottom)
            var splitView = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 6
            };
            splitView.Margin = new Padding(0);
            splitView.Panel1.Padding = new Padding(0);
            splitView.Panel2.Padding = new Padding(0);
            splitView.Panel1.Controls.Add(elementHost);

            // Console
            var consolePanel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.White };
            psOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Consolas", 9f)
            };
            var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 28 };
            psInput = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle };
            var btnRun = new Button { Text = "Run", Dock = DockStyle.Right, Width = 60 };
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
            splitView.Panel2.Controls.Add(consolePanel);

            viewContainer.Controls.Add(splitView);
            viewContainer.Controls.SetChildIndex(splitView, 1);

            this.FormClosed += Form1_FormClosed;

            this.avalonEditor = avalonEditor;
            avalonEditor.TextChanged += (s, e2) => { saveNeeded = true; };

            // Subscribe to editor service events
            _editorSvc.Opened += OnEditorOpened;
            _editorSvc.Saved += OnEditorSaved;
            _editorSvc.Closed += OnEditorClosed;

            // Tree menus and handlers (left tree methods in Form1.ProjectTree.cs)
            SetupTreeViewContextMenu();
            treeView_Project.NodeMouseClick += treeView_Project_NodeMouseClick;
            treeView_Project.KeyDown += treeView_Project_KeyDown;
            treeView_Project.AfterSelect += treeView_Project_AfterSelect;
            treeView_Right.AfterSelect += treeView_Project_AfterSelect;
            treeView_Right.ItemDrag += TreeView_Right_ItemDrag;
            treeView_Project.AllowDrop = true;
            treeView_Project.DragEnter += TreeView_Project_DragEnter;
            treeView_Project.DragOver += TreeView_Project_DragOver;
            treeView_Project.DragDrop += TreeView_Project_DragDrop;

            // Right tree context
            SetupRightTreeViewContextMenu();
            treeView_Right.NodeMouseClick += treeView_Right_NodeMouseClick;
            treeView_Right.KeyDown += treeView_Right_KeyDown;

            _highlightingSvc.ApplyBaseC(avalonEditor);
        }

        // Menu handlers
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select the LEFT project folder",
                InitialDirectory = rootPath
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrEmpty(dialog.FileName))
            {
                projectDirPath = dialog.FileName;
                _treeSvc.PopulateLeft(treeView_Project, projectDirPath);
                _settings.ProjectPath = projectDirPath;
                _settings.Save();
            }
        }

        private void closeRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.TabPages.Remove(tabPage_Projects);
            projectDirPathRight = string.Empty;
            _settings.MirrorProjectPath = string.Empty;
            _settings.Save();
            treeView_Right.Nodes.Clear();
            rightContextNode = null;
            rightCopyBufferPaths.Clear();
            rightCopyMenuItem.Enabled = false;
            rightDeleteMenuItem.Enabled = false;
            if (!tabControl1.TabPages.Contains(tabPage_Projects))
                tabControl1.TabPages.Add(tabPage_Projects);
        }

        private async void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(projectDirPath))
            {
                var savedPath = _settings.ProjectPath;
                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                    projectDirPath = savedPath;
            }
            if (avalonEditor == null)
            {
                System.Windows.Forms.MessageBox.Show("Editor is not ready.", "Save", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            try
            {
                _editorSvc.Text = avalonEditor.Text;
                if (string.IsNullOrWhiteSpace(_editorSvc.CurrentPath))
                {
                    await SaveAsInternalAsync();
                    return;
                }
                var ok = await _editorSvc.SaveAsync();
                if (ok)
                {
                    saveNeeded = false;
                    if (!string.IsNullOrEmpty(projectDirPath))
                    {
                        _settings.ProjectPath = projectDirPath;
                        _settings.Save();
                        System.Windows.Forms.MessageBox.Show("File saved.", "Save", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                        RefreshAvalonEditor();
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("No project selected.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Save failed:\n{ex.Message}", "Save", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private async void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                _editorSvc.Text = avalonEditor.Text;
                var ok = await SaveAsInternalAsync();
                if (ok)
                    System.Windows.Forms.MessageBox.Show("File saved.", "Save As", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Save As failed:\n{ex.Message}", "Save As", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private async Task<bool> SaveAsInternalAsync()
        {
            var initialDir =
                (!string.IsNullOrEmpty(currentViewFilePath) && Directory.Exists(Path.GetDirectoryName(currentViewFilePath!)))
                    ? Path.GetDirectoryName(currentViewFilePath)!
                    : (!string.IsNullOrEmpty(projectDirPath) && Directory.Exists(projectDirPath))
                        ? projectDirPath
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var currentName = !string.IsNullOrEmpty(currentViewFilePath)
                ? Path.GetFileName(currentViewFilePath)
                : "untitled.c";

            var ok = await _editorSvc.SaveAsAsync(initialDir, currentName);
            if (ok)
            {
                currentViewFilePath = _editorSvc.CurrentPath ?? string.Empty;
                saveNeeded = false;
                DisplayFileInViewTab(currentViewFilePath);
                if (!string.IsNullOrEmpty(projectDirPath) && _treeSvc.IsUnderRoot(currentViewFilePath, projectDirPath))
                    _treeSvc.AddOrUpdateFileNode(treeView_Project, projectDirPath, currentViewFilePath);
            }
            return ok;
        }

        private void exitToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void DisplayFileInViewTab(string filePath)
        {
            if (!File.Exists(filePath) || avalonEditor == null) return;

            await _editorSvc.OpenAsync(filePath);
            avalonEditor.Text = _editorSvc.Text;
            currentViewFilePath = filePath;
            UpdateViewHeader(filePath);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (ext is ".c" or ".h")
            {
                _highlightingSvc.EnsureCustomCRegistered(rootPath);
                avalonEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C-PIC");
                return;
            }

            var def = _highlightingSvc.GetForPath(filePath);
            if (def != null)
            {
                avalonEditor.SyntaxHighlighting = def;
            }
            else
            {
                avalonEditor.SyntaxHighlighting = null;
            }
        }

        private void RefreshAvalonEditor()
        {
            if (!string.IsNullOrEmpty(currentViewFilePath) && File.Exists(currentViewFilePath) && avalonEditor != null)
                avalonEditor.Text = File.ReadAllText(currentViewFilePath);
        }

        private void CloseCurrentView()
        {
            if (avalonEditor == null) return;
            _editorSvc.Close(avalonEditor);
            currentViewFilePath = string.Empty;
            saveNeeded = false;
            UpdateViewHeader(null);
        }

        private void OnEditorOpened(object? sender, EventArgs e)
        {
            currentViewFilePath = _editorSvc.CurrentPath ?? string.Empty;
            UpdateViewHeader(currentViewFilePath);
        }

        private void OnEditorSaved(object? sender, EventArgs e)
        {
            var path = _editorSvc.CurrentPath;
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(projectDirPath) && _treeSvc.IsUnderRoot(path, projectDirPath))
                _treeSvc.AddOrUpdateFileNode(treeView_Project, projectDirPath, path);
        }

        private void OnEditorClosed(object? sender, EventArgs e)
        {
            UpdateViewHeader(null);
        }

        private void treeView_Project_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is FileInfo fileInfo)
                DisplayFileInViewTab(fileInfo.FullName);
        }

        // Right tree context menu (keep here)
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
            if (node.Parent == null)
            {
                System.Windows.Forms.MessageBox.Show("Cannot delete the project root folder.", "Delete", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(projectDirPathRight) || !Directory.Exists(projectDirPathRight))
            {
                System.Windows.Forms.MessageBox.Show("No RIGHT project root is open.", "Delete", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (node.Tag is FileInfo fi)
                {
                    if (!IsUnderRoot(fi.FullName, projectDirPathRight))
                    {
                        System.Windows.Forms.MessageBox.Show("Blocked: outside RIGHT project root.", "Delete", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        return;
                    }
                    var confirm = System.Windows.Forms.MessageBox.Show($"Delete file:\n{fi.FullName}?", "Confirm Delete",
                        System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning, System.Windows.Forms.MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;
                    if (!string.IsNullOrEmpty(currentViewFilePath) && string.Equals(currentViewFilePath, fi.FullName, StringComparison.OrdinalIgnoreCase))
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
                        System.Windows.Forms.MessageBox.Show("Blocked: outside RIGHT project root.", "Delete", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        return;
                    }
                    var confirm = System.Windows.Forms.MessageBox.Show($"Delete folder and all contents:\n{di.FullName}?",
                        "Confirm Delete Folder", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning,
                        System.Windows.Forms.MessageBoxDefaultButton.Button2);
                    if (confirm != DialogResult.Yes) return;
                    if (Directory.Exists(di.FullName))
                        Directory.Delete(di.FullName, recursive: true);
                    node.Remove();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Delete failed:\n{ex.Message}", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private static bool IsUnderRoot(string path, string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return false;
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }

        private void RightCopySelectedNode()
        {
            var node = rightContextNode ?? treeView_Right.SelectedNode;
            if (node == null || node.Tag is not FileSystemInfo fsi) return;
            rightCopyBufferPaths = new List<string> { fsi.FullName };
        }

        // Paste from right clipboard into left tree target
        private void LeftPasteFromRightClipboard()
        {
            if (rightCopyBufferPaths == null || rightCopyBufferPaths.Count == 0) return;
            if (string.IsNullOrEmpty(projectDirPath) || !Directory.Exists(projectDirPath))
            {
                System.Windows.Forms.MessageBox.Show("No LEFT project root is open.", "Paste", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
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
                        _fs.CopyDirectoryWithPrompt(srcDir.FullName, destDir, ref policy);
                    }
                    else if (File.Exists(srcPath))
                    {
                        var fileName = Path.GetFileName(srcPath);
                        var destFile = Path.Combine(targetDir, fileName);
                        _fs.TryCopyFileWithPrompt(srcPath, destFile, ref policy);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Paste failed:\n{ex.Message}", "Paste", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
            var dirNodeToRefresh = GetDirectoryNodeForDrop(node);
            if (dirNodeToRefresh != null)
                RepopulateDirectoryNode(dirNodeToRefresh);
        }

        // Header update for view
        private void UpdateViewHeader(string? path)
        {
            if (viewHeaderLabel == null) return;
            viewHeaderLabel.Text = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
            viewHeaderLabel.Tag = path ?? string.Empty;
        }

        // Context menu on WPF AvalonEdit
        private void SetupAvalonEditorContextMenu(TextEditor editor)
        {
            var cm = new System.Windows.Controls.ContextMenu();
            var miCut = new System.Windows.Controls.MenuItem { Header = "Cut" };
            miCut.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Cut;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea)) cmd.Execute(null, editor.TextArea);
            };
            var miCopy = new System.Windows.Controls.MenuItem { Header = "Copy" };
            miCopy.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Copy;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea)) cmd.Execute(null, editor.TextArea);
            };
            var miPaste = new System.Windows.Controls.MenuItem { Header = "Paste" };
            miPaste.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Paste;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea)) cmd.Execute(null, editor.TextArea);
            };
            var miSave = new System.Windows.Controls.MenuItem { Header = "Save" };
            miSave.Click += async (s, e) => await Task.Run(() => saveToolStripMenuItem_Click(this, EventArgs.Empty));
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

        // Remaining menu handlers referenced by Designer
        private void mCCStandaloneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var scr = new scripts();
            scr.launch("startMcc", mirror_projectPath);
            scr.alert_changes(mirror_projectPath);
        }

        private void mPLABXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var scr = new scripts();
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
            using var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select the parent folder for your new project",
                InitialDirectory = rootPath
            };
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
                Directory.CreateDirectory(Path.Combine(newProjectPath, "srcs"));
                Directory.CreateDirectory(Path.Combine(newProjectPath, "incs"));
                Directory.CreateDirectory(Path.Combine(newProjectPath, "libs"));
                Directory.CreateDirectory(Path.Combine(newProjectPath, "objs"));
                Directory.CreateDirectory(Path.Combine(newProjectPath, "other"));
                File.Copy($"{rootPath}dependancies\\makefiles\\Makefile_Root", Path.Combine(newProjectPath, "", "Makefile"));
                if (Directory.Exists(Path.Combine(newProjectPath, "srcs")))
                {
                    File.Copy($"{rootPath}dependancies\\makefiles\\Makefile_Srcs", Path.Combine(newProjectPath, "srcs", "Makefile"));
                    Directory.CreateDirectory(Path.Combine(newProjectPath, "srcs\\startup"));
                    if (Directory.Exists(Path.Combine(newProjectPath, "srcs", "startup")))
                    {
                        File.Copy($"{rootPath}dependancies\\project_files\\startup.S", Path.Combine(newProjectPath, "srcs\\startup", "startup.S"));
                    }
                }
                _treeSvc.PopulateLeft(treeView_Project, projectDirPath);
                _settings.ProjectPath = projectDirPath;
                _settings.Save();
            }
            var res = System.Windows.Forms.MessageBox.Show("Now select the mirror project for your new project.", "Mirror Project", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information);
            if (res == DialogResult.No) return;
            var mirror = (string.IsNullOrEmpty(mirror_projectPath)) ? "C:\\" : mirror_projectPath;
            var ofd = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog { IsFolderPicker = true, Title = "Select the device mirror project for the " + projectName, InitialDirectory = mirror };
            if (ofd.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                if (!string.IsNullOrEmpty(ofd.FileName) && Directory.Exists(ofd.FileName))
                {
                    mirror_projectPath = ofd.FileName;
                    _settings.MirrorProjectPath = mirror_projectPath;
                    _settings.Save();
                    _treeSvc.Populate(treeView_Right, mirror_projectPath);
                }
            }
        }

        private async void generateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.CompletedTask;
            System.Windows.Forms.MessageBox.Show("Generate is not implemented in this branch.");
        }

        private void cSourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (avalonEditor == null)
                {
                    System.Windows.Forms.MessageBox.Show("Editor is not ready.", "New C Source", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }
                _editorSvc.NewC(avalonEditor);
                currentViewFilePath = string.Empty;
                saveNeeded = true;
                UpdateViewHeader("untitled.c");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Create C source failed:\n{ex.Message}", "New C Source", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void headerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (avalonEditor == null)
                {
                    System.Windows.Forms.MessageBox.Show("Editor is not ready.", "New Header", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return;
                }
                _editorSvc.NewHeader(avalonEditor);
                currentViewFilePath = string.Empty;
                saveNeeded = true;
                UpdateViewHeader("untitled.h");
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Create header failed:\n{ex.Message}", "New Header", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void openRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select the RIGHT project folder",
                InitialDirectory = rootPath
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && !string.IsNullOrEmpty(dialog.FileName))
            {
                projectDirPathRight = dialog.FileName;
                _settings.MirrorProjectPath = projectDirPathRight;
                _settings.Save();
                _treeSvc.Populate(treeView_Right, projectDirPathRight);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _editorSvc.Opened -= OnEditorOpened;
            _editorSvc.Saved -= OnEditorSaved;
            _editorSvc.Closed -= OnEditorClosed;
        }
    }
}