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
using MessageBox = System.Windows.Forms.MessageBox; // disambiguate
using DataFormats = System.Windows.Forms.DataFormats;
using LibGit2Sharp;

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
        private readonly ITabService _tabService;
        private readonly IGitService _gitSvc;
        private readonly IHotkeyService _hotkeysSvc;

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
        private TextBox psInput; // legacy field kept but unused
        private bool pendingMakeRefresh;
        private TreeNode? pendingRefreshNode;
        private bool _shellOutputSubscribed;
        private SplitContainer rightPaneSplit; // top: tabs, bottom: console

        // View header
        private System.Windows.Forms.Label viewHeaderLabel;

        // Git tab state
        private string _gitRepoRoot = string.Empty;
        private string _gitCurrentBranch = string.Empty;

        // Action identifiers used by IHotkeyService
        private const string HK_Save = "Save";
        private const string HK_ToggleConsole = "ToggleConsole";
        private const string HK_OpenGitTab = "OpenGitTab";
        private const string HK_CloseGitTab = "CloseGitTab";
        private const string HK_Stage = "Stage";
        private const string HK_Commit = "Commit";
        private const string HK_Fetch = "Fetch";
        private const string HK_Pull = "Pull";
        private const string HK_Push = "Push";

        public Form1(
            ISettingsService settings,
            IDialogService dialogs,
            IProjectTreeService treeSvc,
            IFileSystemService fs,
            IHighlightingService highlightingSvc,
            IShellService shellSvc,
            IEditorService editorSvc,
            ITabService tabService,
            IGitService gitService,
            IHotkeyService hotkeys)
        {
            _settings = settings;
            _dialogs = dialogs;
            _treeSvc = treeSvc;
            _fs = fs;
            _highlightingSvc = highlightingSvc;
            _shellSvc = shellSvc;
            _editorSvc = editorSvc;
            _tabService = tabService;
            _gitSvc = gitService;
            _hotkeysSvc = hotkeys;

            InitializeComponent();
            rootPath = "C:\\Users\\davec\\GIT\\PIC32_M_DEV\\PIC32Mn_PROJ\\PIC32Mn_PROJ\\";
            packsPath = $"{rootPath}XML\\";
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            // Ensure form sees shortcuts before child controls
            this.KeyPreview = true;
            // Add a global message filter to catch keys from hosted controls (e.g., WPF)
            _hotkeyFilter = new GlobalHotkeyFilter(this);
            System.Windows.Forms.Application.AddMessageFilter(_hotkeyFilter);

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
                ShowProjectTab();
            }
            else
            {
                HideProjectTab();
            }

            // Minimal AvalonEdit instance retained for features that still depend on it (e.g., New file templates)
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

            this.FormClosed += Form1_FormClosed;

            this.avalonEditor = avalonEditor;
            avalonEditor.TextChanged += (s, e2) => { saveNeeded = true; };

            // Build right pane split with integrated console
            BuildRightPaneWithConsole();

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

            // Add Git menu items now that _gitSvc is available
            InitializeGitMenu();

            // Add context menu to Git tab to allow closing it
            if (tabPage_Git != null)
            {
                var cmGit = new ContextMenuStrip();
                cmGit.Items.Add(new ToolStripMenuItem("Close Git Tab", null, (s, ea) => Git_CloseTab()));
                tabPage_Git.ContextMenuStrip = cmGit;
            }

            // Wire Git tab handlers and refresh
            WireGitTabEvents();
            _ = Git_RefreshAllAsync();
        }

        // Global message filter to catch hotkeys even when focus is in hosted controls
        private GlobalHotkeyFilter? _hotkeyFilter;
        private sealed class GlobalHotkeyFilter : IMessageFilter
        {
            private readonly Form1 _owner;
            public GlobalHotkeyFilter(Form1 owner) => _owner = owner;
            private const int WM_KEYDOWN = 0x0100;
            private const int WM_SYSKEYDOWN = 0x0104;
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_KEYDOWN || m.Msg == WM_SYSKEYDOWN)
                {
                    var key = (Keys)m.WParam.ToInt32();
                    // Combine with current modifiers
                    var keyData = key | Control.ModifierKeys;
                    if (_owner.HandleHotkey(keyData))
                        return true; // consume
                }
                return false;
            }
        }

        // Central hotkey handler used by both ProcessCmdKey and message filter
        private bool HandleHotkey(Keys keyData)
        {
            try
            {
                if (keyData == _hotkeysSvc.Get(HK_Save)) { saveToolStripMenuItem_Click(this, EventArgs.Empty); return true; }
                if (keyData == _hotkeysSvc.Get(HK_ToggleConsole)) { ToggleConsole(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_OpenGitTab)) { Git_ShowTab(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_CloseGitTab)) { Git_CloseTab(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_Stage)) { _ = Git_StageSelectedAsync(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_Commit)) { _ = Git_ButtonCommitAsync(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_Fetch)) { _ = Git_ButtonFetchAsync(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_Pull)) { _ = Git_ButtonPullAsync(); return true; }
                if (keyData == _hotkeysSvc.Get(HK_Push)) { _ = Git_ButtonPushAsync(); return true; }
            }
            catch { }
            return false;
        }

        // Robust global shortcut handling (works even with hosted controls)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (HandleHotkey(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildRightPaneWithConsole()
        {
            // Create the bottom console UI using only a RichTextBox (type directly in it)
            var consolePanel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.White };
            psOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Consolas", 9f),
                HideSelection = false,
                DetectUrls = false,
                AcceptsTab = true,
                WordWrap = false
            };
            // Hook console input behaviors
            psOutput.KeyDown += Console_KeyDown;
            psOutput.KeyPress += Console_KeyPress; // allow typing filter
            psOutput.MouseDown += Console_MouseDown;

            // Context menu for copy/paste
            var consoleMenu = new ContextMenuStrip();
            var miCopy = new ToolStripMenuItem("Copy", null, (s, e) => Console_CopySelection());
            var miPaste = new ToolStripMenuItem("Paste", null, (s, e) => Console_PasteFromClipboard());
            consoleMenu.Opening += (s, e) =>
            {
                miCopy.Enabled = psOutput.SelectionLength > 0;
                miPaste.Enabled = System.Windows.Forms.Clipboard.ContainsText();
            };
            consoleMenu.Items.Add(miCopy);
            consoleMenu.Items.Add(miPaste);
            psOutput.ContextMenuStrip = consoleMenu;

            consolePanel.Controls.Add(psOutput);

            // New split in right side: Panel1 = tabs, Panel2 = console
            rightPaneSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                SplitterWidth = 6,
                Panel2Collapsed = true
            };

            // Move the existing tabControl1 into Panel1
            splitContainer1.Panel2.Controls.Remove(tabControl1);
            tabControl1.Dock = DockStyle.Fill;
            rightPaneSplit.Panel1.Controls.Add(tabControl1);
            rightPaneSplit.Panel2.Controls.Add(consolePanel);

            // Install new right split into main layout
            splitContainer1.Panel2.Controls.Add(rightPaneSplit);
        }

        private void ShowConsole()
        {
            if (rightPaneSplit == null) return;
            rightPaneSplit.Panel2Collapsed = false;
            // Give bottom about 30% height
            rightPaneSplit.SplitterDistance = (int)(splitContainer1.Panel2.Height * 0.7);
            EnsureShellStarted();
            psOutput?.Focus();
            // Ensure a prompt is visible when opening
            Console_ShowPromptIfNeeded();
        }

        private void HideConsole()
        {
            if (rightPaneSplit == null) return;
            rightPaneSplit.Panel2Collapsed = true;
        }

        private void ToggleConsole()
        {
            if (rightPaneSplit == null) return;
            if (rightPaneSplit.Panel2Collapsed)
                ShowConsole();
            else
                HideConsole();
        }

        private void WireGitTabEvents()
        {
            if (btnGitRefresh != null) btnGitRefresh.Click += async (s, e) => await Git_RefreshAllAsync();
            if (btnGitPull != null) btnGitPull.Click += async (s, e) => await Git_ButtonPullAsync();
            if (btnGitPush != null) btnGitPush.Click += async (s, e) => await Git_ButtonPushAsync();
            if (btnGitFetch != null) btnGitFetch.Click += async (s, e) => await Git_ButtonFetchAsync();
            if (btnGitCheckout != null) btnGitCheckout.Click += async (s, e) => await Git_ButtonCheckoutAsync();
            if (btnGitNewBranch != null) btnGitNewBranch.Click += async (s, e) => await Git_ButtonNewBranchAsync();
            if (btnGitStage != null) btnGitStage.Click += async (s, e) => await Git_StageSelectedAsync();
            if (btnGitUnstage != null) btnGitUnstage.Click += async (s, e) => await Git_UnstageSelectedAsync();
            if (btnGitCommit != null) btnGitCommit.Click += async (s, e) => await Git_ButtonCommitAsync();
        }

        private void SetGitControlsEnabled(bool enabled)
        {
            var controls = new Control[]
            {
                comboGitBranches, btnGitCheckout, btnGitNewBranch, btnGitFetch, btnGitPull, btnGitPush,
                btnGitRefresh, listViewGitStatus, btnGitStage, btnGitUnstage, txtGitCommit, btnGitCommit
            };
            foreach (var c in controls)
            {
                if (c != null) c.Enabled = enabled;
            }
        }

        private async Task Git_RefreshAllAsync()
        {
            // Discover repo
            _gitRepoRoot = string.Empty;
            var start = string.IsNullOrEmpty(projectDirPath) ? Environment.CurrentDirectory : projectDirPath;
            if (_gitSvc.TryDiscoverRepo(start, out var info) && info != null)
            {
                _gitRepoRoot = info.Root;
                _gitCurrentBranch = info.CurrentBranch;
                SetGitControlsEnabled(true);
                await Git_LoadBranchesAsync();
                await Git_LoadStatusAsync();
            }
            else
            {
                SetGitControlsEnabled(false);
                MessageBox.Show("No git repository found under the current project path.", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task Git_LoadBranchesAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot) || comboGitBranches == null) return;
            try
            {
                comboGitBranches.Items.Clear();
                var branches = await _gitSvc.GetBranchesAsync(_gitRepoRoot);
                foreach (var b in branches) comboGitBranches.Items.Add(b);
                if (!string.IsNullOrEmpty(_gitCurrentBranch))
                {
                    for (int i = 0; i < comboGitBranches.Items.Count; i++)
                    {
                        if (string.Equals(comboGitBranches.Items[i]?.ToString(), _gitCurrentBranch, StringComparison.OrdinalIgnoreCase))
                        {
                            comboGitBranches.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load branches:\n{ex.Message}", "Git", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Git_LoadStatusAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot) || listViewGitStatus == null) return;
            try
            {
                listViewGitStatus.BeginUpdate();
                listViewGitStatus.Items.Clear();
                var items = await _gitSvc.GetStatusAsync(_gitRepoRoot);
                if (items.Count == 0)
                {
                    listViewGitStatus.Items.Add(
                        new ListViewItem(new[] { "Clean", "(working tree clean)" })
                        { ForeColor = System.Drawing.Color.DarkGreen });
                }
                else
                {
                    foreach (var it in items)
                    {
                        var lvi = new ListViewItem(new[] { it.Status, it.Path }) { Tag = it.Path };
                        listViewGitStatus.Items.Add(lvi);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load status:\n{ex.Message}", "Git",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listViewGitStatus.EndUpdate();
            }
        }

        private async Task Git_ButtonPullAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot)) { await Git_RefreshAllAsync(); if (string.IsNullOrEmpty(_gitRepoRoot)) return; }
            var output = await _gitSvc.PullAsync(_gitRepoRoot);
            MessageBox.Show(output, "git pull", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Git_LoadStatusAsync();
        }

        private async Task Git_ButtonPushAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot)) { await Git_RefreshAllAsync(); if (string.IsNullOrEmpty(_gitRepoRoot)) return; }
            var output = await _gitSvc.PushAsync(_gitRepoRoot);
            MessageBox.Show(output, "git push", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Git_LoadStatusAsync();
        }

        private async Task Git_ButtonFetchAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot)) { await Git_RefreshAllAsync(); if (string.IsNullOrEmpty(_gitRepoRoot)) return; }
            var output = await _gitSvc.FetchAsync(_gitRepoRoot);
            MessageBox.Show(output, "git fetch", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Git_LoadStatusAsync();
        }

        private async Task Git_ButtonCheckoutAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot) || comboGitBranches == null) return;
            var target = comboGitBranches.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(target)) { MessageBox.Show("Select a branch to checkout."); return; }
            try
            {
                await _gitSvc.SwitchBranchAsync(_gitRepoRoot, target, createIfMissing: false);
                _gitCurrentBranch = target;
                MessageBox.Show($"Checked out '{target}'.", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await Git_LoadStatusAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Checkout failed:\n{ex.Message}", "Git", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Git_ButtonNewBranchAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot)) return;
            string name = Microsoft.VisualBasic.Interaction.InputBox("Enter new branch name:", "New Branch", "feature/");
            if (string.IsNullOrWhiteSpace(name)) return;
            try
            {
                await _gitSvc.SwitchBranchAsync(_gitRepoRoot, name, createIfMissing: true);
                _gitCurrentBranch = name;
                await Git_LoadBranchesAsync();
                MessageBox.Show($"Created and switched to '{name}'.", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await Git_LoadStatusAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Create branch failed:\n{ex.Message}", "Git", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Git_StageSelectedAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot) || listViewGitStatus == null) return;
            if (listViewGitStatus.SelectedItems.Count == 0) { MessageBox.Show("Select files in the status list to stage."); return; }
            try
            {
                var paths = new List<string>();
                foreach (ListViewItem sel in listViewGitStatus.SelectedItems)
                {
                    var rel = sel.Tag?.ToString() ?? string.Empty;
                    var full = string.IsNullOrEmpty(rel) ? string.Empty : Path.Combine(_gitRepoRoot, rel);
                    if (!string.IsNullOrEmpty(full)) paths.Add(full);
                }
                if (paths.Count > 0)
                {
                    await _gitSvc.StageAsync(_gitRepoRoot, paths);
                    await Git_LoadStatusAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Stage failed:\n{ex.Message}", "Git", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Git_UnstageSelectedAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot) || listViewGitStatus == null) return;
            if (listViewGitStatus.SelectedItems.Count == 0) { MessageBox.Show("Select files in the status list to unstage."); return; }
            try
            {
                var paths = new List<string>();
                foreach (ListViewItem sel in listViewGitStatus.SelectedItems)
                {
                    var rel = sel.Tag?.ToString() ?? string.Empty;
                    var full = string.IsNullOrEmpty(rel) ? string.Empty : Path.Combine(_gitRepoRoot, rel);
                    if (!string.IsNullOrEmpty(full)) paths.Add(full);
                }
                if (paths.Count > 0)
                {
                    await _gitSvc.UnstageAsync(_gitRepoRoot, paths);
                    await Git_LoadStatusAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unstage failed:\n{ex.Message}", "Git", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task Git_ButtonCommitAsync()
        {
            if (string.IsNullOrEmpty(_gitRepoRoot) || txtGitCommit == null) return;
            var message = txtGitCommit.Text?.Trim();
            if (string.IsNullOrEmpty(message)) { MessageBox.Show("Enter a commit message."); return; }
            try
            {
                using var dlg = new CommitDialog(message);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                // Stage all and commit to reduce user errors that cause EmptyCommitException
                await _gitSvc.CommitAllAsync(_gitRepoRoot, dlg.Message, dlg.AuthorName, dlg.AuthorEmail);
                txtGitCommit.Clear();
                MessageBox.Show("Commit complete.", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await Git_LoadStatusAsync();
            }
            catch (LibGit2Sharp.EmptyCommitException)
            {
                MessageBox.Show("Nothing to commit (working tree clean or nothing staged).", "Git", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Commit failed:\n{ex.Message}", "Git", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Options -> Console menu handlers
        private void openConsoleToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            ShowConsole();
        }

        private void closeConsoleToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            HideConsole();
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
            // Hide Project tab when right project is closed
            HideProjectTab();

            projectDirPathRight = string.Empty;
            _settings.MirrorProjectPath = string.Empty;
            _settings.Save();
            treeView_Right.Nodes.Clear();
            rightContextNode = null;
            rightCopyBufferPaths.Clear();
            rightCopyMenuItem.Enabled = false;
            rightDeleteMenuItem.Enabled = false;
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
                // Open the saved file in its own tab instead of the removed View tab
                if (!string.IsNullOrEmpty(currentViewFilePath))
                    _tabService.OpenFile(tabControl1, currentViewFilePath, rootPath);

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
            // Legacy method retained for compatibility, but delegate to tab service now
            if (!File.Exists(filePath)) return;
            _tabService.OpenFile(tabControl1, filePath, rootPath);
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
                _tabService.OpenFile(tabControl1, fileInfo.FullName, rootPath);
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
                ShowProjectTab();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (_hotkeyFilter != null) System.Windows.Forms.Application.RemoveMessageFilter(_hotkeyFilter);
            _editorSvc.Opened -= OnEditorOpened;
            _editorSvc.Saved -= OnEditorSaved;
            _editorSvc.Closed -= OnEditorClosed;
        }

        // Options -> Hotkeys dialog
        private void hotkeysToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Hotkeys",
                StartPosition = FormStartPosition.CenterParent,
                Size = new System.Drawing.Size(560, 480),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            list.Columns.Add("Action", 260);
            list.Columns.Add("Keys", 240);

            void Populate()
            {
                list.BeginUpdate();
                list.Items.Clear();
                foreach (var kv in _hotkeysSvc.GetAll())
                    list.Items.Add(new ListViewItem(new[] { kv.Key, kv.Value.ToString() }));
                list.EndUpdate();
            }

            // Bottom bar with actions
            var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 52 };

            var btnReset = new Button { Text = "Reset Defaults", Left = 10, Top = 12, Width = 140 };
            var btnChange = new Button { Text = "Change...", Left = 160, Top = 12, Width = 110, Enabled = false };
            var btnRemove = new Button { Text = "Remove", Left = 280, Top = 12, Width = 100, Enabled = false };
            var btnClose = new Button { Text = "Close", Width = 100, Top = 12, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            panelBottom.Controls.AddRange(new Control[] { btnReset, btnChange, btnRemove, btnClose });
            panelBottom.Resize += (s, ea) => { btnClose.Left = panelBottom.ClientSize.Width - btnClose.Width - 10; };
            btnClose.Left = panelBottom.ClientSize.Width - btnClose.Width - 10;
            btnClose.DialogResult = DialogResult.OK;
            dlg.AcceptButton = btnClose;

            dlg.Controls.Add(list);
            dlg.Controls.Add(panelBottom);

            void UpdateButtons()
            {
                bool hasSel = list.SelectedItems.Count == 1;
                btnChange.Enabled = hasSel;
                btnRemove.Enabled = hasSel;
            }

            list.SelectedIndexChanged += (s, ea) => UpdateButtons();
            list.DoubleClick += (s, ea) => { if (btnChange.Enabled) ChangeSelected(); };

            btnReset.Click += (s, ea) =>
            {
                _hotkeysSvc.ResetDefaults();
                Populate();
                InitializeGitMenu(); // refresh menu accelerators
            };

            btnRemove.Click += (s, ea) =>
            {
                if (list.SelectedItems.Count != 1) return;
                var actionId = list.SelectedItems[0].SubItems[0].Text;
                _hotkeysSvc.Set(actionId, Keys.None); // unbind
                Populate();
                InitializeGitMenu();
            };

            btnChange.Click += (s, ea) => ChangeSelected();

            void ChangeSelected()
            {
                if (list.SelectedItems.Count != 1) return;
                var actionId = list.SelectedItems[0].SubItems[0].Text;

                using var cap = CreateCaptureDialog(list.SelectedItems[0].SubItems[1].Text);
                if (cap.ShowDialog(dlg) == DialogResult.OK)
                {
                    var picked = cap.Tag is Keys k ? k : Keys.None;
                    _hotkeysSvc.Set(actionId, picked);
                    Populate();
                    InitializeGitMenu();
                }
            }

            Populate();
            UpdateButtons();
            dlg.ShowDialog(this);

            // Reflect any changes into the Git menu again (safety)
            InitializeGitMenu();

            // Local helper: dialog to capture a new shortcut
            Form CreateCaptureDialog(string currentText)
            {
                var f = new Form
                {
                    Text = "Press new shortcut...",
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new System.Drawing.Size(420, 160),
                    MinimizeBox = false,
                    MaximizeBox = false,
                    ShowIcon = false,
                    ShowInTaskbar = false,
                    KeyPreview = true
                };

                var lbl = new Label
                {
                    Text = "Press the desired key combination (e.g., Ctrl+Shift+S).\nPress Clear to unbind.",
                    Dock = DockStyle.Top,
                    Height = 44
                };

                var txt = new TextBox
                {
                    ReadOnly = true,
                    Dock = DockStyle.Top,
                    Height = 28,
                    Text = currentText
                };

                var bottom = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                var btnClear = new Button { Text = "Clear", Left = 10, Top = 8, Width = 90 };
                var btnOk = new Button { Text = "OK", Width = 100, Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right, Enabled = true };
                var btnCancel = new Button { Text = "Cancel", Width = 100, Top = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right, DialogResult = DialogResult.Cancel };

                bottom.Controls.Add(btnClear);
                bottom.Controls.Add(btnOk);
                bottom.Controls.Add(btnCancel);
                bottom.Resize += (s, ea) =>
                {
                    btnCancel.Left = bottom.ClientSize.Width - btnCancel.Width - 10;
                    btnOk.Left = btnCancel.Left - btnOk.Width - 8;
                };
                btnCancel.Left = bottom.ClientSize.Width - btnCancel.Width - 10;
                btnOk.Left = btnCancel.Left - btnOk.Width - 8;

                f.Controls.Add(bottom);
                f.Controls.Add(txt);
                f.Controls.Add(lbl);

                // Stored result in Tag
                f.Tag = Keys.None;

                // Clear: unbind
                btnClear.Click += (s, ea) =>
                {
                    f.Tag = Keys.None;
                    txt.Text = "None";
                };

                // OK: accept whatever is in Tag
                btnOk.DialogResult = DialogResult.OK;
                f.AcceptButton = btnOk;
                f.CancelButton = btnCancel;

                // Capture key combo
                f.KeyDown += (s, ke) =>
                {
                    // Ignore pure modifiers (wait for a key)
                    if (ke.KeyCode is Keys.Menu or Keys.ShiftKey or Keys.ControlKey)
                    {
                        // Show current modifier state in preview
                        var k = (ke.Modifiers);
                        txt.Text = k == Keys.None ? "" : k.ToString();
                        ke.SuppressKeyPress = true;
                        return;
                    }

                    var combo = ke.KeyData;

                    // Normalize: avoid duplicate Shift/Control in text (KeyData already includes modifiers)
                    f.Tag = combo;
                    txt.Text = combo.ToString();
                    ke.SuppressKeyPress = true;
                };

                return f;
            }
        }
      
        // --- Helpers added to remove build-time missing method errors ---
        private void InitializeGitMenu()
        {
            if (gitToolStripMenuItem == null) return;
            gitToolStripMenuItem.DropDownItems.Clear();

            ToolStripMenuItem AddItem(string text, Keys keys, EventHandler onClick)
            {
                var mi = new ToolStripMenuItem(text);
                if (keys != Keys.None)
                {
                    mi.ShortcutKeys = keys;
                    mi.ShowShortcutKeys = true;
                }
                mi.Click += onClick;
                gitToolStripMenuItem.DropDownItems.Add(mi);
                return mi;
            }

            AddItem("Open Git Tab", _hotkeysSvc.Get(HK_OpenGitTab), (s, e) => Git_ShowTab());
            AddItem("Close Git Tab", _hotkeysSvc.Get(HK_CloseGitTab), (s, e) => Git_CloseTab());
            gitToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddItem("Refresh", Keys.None, async (s, e) => await Git_RefreshAllAsync());
            AddItem("Fetch", _hotkeysSvc.Get(HK_Fetch), async (s, e) => await Git_ButtonFetchAsync());
            AddItem("Pull", _hotkeysSvc.Get(HK_Pull), async (s, e) => await Git_ButtonPullAsync());
            AddItem("Push", _hotkeysSvc.Get(HK_Push), async (s, e) => await Git_ButtonPushAsync());
            gitToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            AddItem("Stage Selected", _hotkeysSvc.Get(HK_Stage), async (s, e) => await Git_StageSelectedAsync());
            AddItem("Unstage Selected", Keys.None, async (s, e) => await Git_UnstageSelectedAsync());
            AddItem("Commit", _hotkeysSvc.Get(HK_Commit), async (s, e) => await Git_ButtonCommitAsync());
        }

        private void ShowProjectTab()
        {
            if (tabPage_Projects == null || tabControl1 == null) return;
            if (!tabControl1.TabPages.Contains(tabPage_Projects))
                tabControl1.TabPages.Add(tabPage_Projects);
        }

        private void HideProjectTab()
        {
            if (tabPage_Projects == null || tabControl1 == null) return;
            if (tabControl1.TabPages.Contains(tabPage_Projects))
            {
                bool wasSelected = tabControl1.SelectedTab == tabPage_Projects;
                tabControl1.TabPages.Remove(tabPage_Projects);
                if (wasSelected && tabControl1.TabPages.Count > 0)
                    tabControl1.SelectedIndex = 0;
            }
        }

        private void Git_ShowTab()
        {
            if (tabPage_Git == null || tabControl1 == null) return;
            if (!tabControl1.TabPages.Contains(tabPage_Git))
                tabControl1.TabPages.Add(tabPage_Git);
            tabControl1.SelectedTab = tabPage_Git;
        }

        private void Git_CloseTab()
        {
            if (tabPage_Git == null || tabControl1 == null) return;
            if (tabControl1.TabPages.Contains(tabPage_Git))
            {
                bool wasSelected = tabControl1.SelectedTab == tabPage_Git;
                tabControl1.TabPages.Remove(tabPage_Git);
                if (wasSelected && tabControl1.TabPages.Count > 0)
                    tabControl1.SelectedIndex = 0;
            }
        }
    }
}