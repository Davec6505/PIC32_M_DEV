using System.Drawing;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{   
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            openRightToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            mccToolStripMenuItem = new ToolStripMenuItem();
            mCCStandaloneToolStripMenuItem = new ToolStripMenuItem();
            mPLABXToolStripMenuItem = new ToolStripMenuItem();
            vSCodeToolStripMenuItem = new ToolStripMenuItem();
            createProjectToolStripMenuItem = new ToolStripMenuItem();
            generateToolStripMenuItem = new ToolStripMenuItem();
            newToolStripMenuItem = new ToolStripMenuItem();
            cSourceToolStripMenuItem = new ToolStripMenuItem();
            headerToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            openConsoleToolStripMenuItem = new ToolStripMenuItem();
            closeConsoleToolStripMenuItem = new ToolStripMenuItem();
            hotkeysToolStripMenuItem = new ToolStripMenuItem();
            gitToolStripMenuItem = new ToolStripMenuItem();
            treeView_Project = new TreeView();
            splitContainer1 = new SplitContainer();
            tabControl1 = new TabControl();
            tabPage_Projects = new TabPage();
            treeView_Right = new TreeView();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage_Projects.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, optionsToolStripMenuItem, gitToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(264, 28);
            menuStrip1.TabIndex = 0;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, openRightToolStripMenuItem, toolStripMenuItem1, saveToolStripMenuItem, saveAsToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(224, 26);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // openRightToolStripMenuItem
            // 
            openRightToolStripMenuItem.Name = "openRightToolStripMenuItem";
            openRightToolStripMenuItem.Size = new Size(224, 26);
            openRightToolStripMenuItem.Text = "Open Right";
            openRightToolStripMenuItem.Click += openRightToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(224, 26);
            toolStripMenuItem1.Text = "Close Right";
            toolStripMenuItem1.Click += closeRightToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(224, 26);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(224, 26);
            saveAsToolStripMenuItem.Text = "Save As";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(224, 26);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click_1;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { mccToolStripMenuItem, createProjectToolStripMenuItem, generateToolStripMenuItem, newToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(49, 24);
            editToolStripMenuItem.Text = "Edit";
            // 
            // mccToolStripMenuItem
            // 
            mccToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { mCCStandaloneToolStripMenuItem, mPLABXToolStripMenuItem, vSCodeToolStripMenuItem });
            mccToolStripMenuItem.Name = "mccToolStripMenuItem";
            mccToolStripMenuItem.Size = new Size(185, 26);
            mccToolStripMenuItem.Text = "Mirror Project";
            // 
            // mCCStandaloneToolStripMenuItem
            // 
            mCCStandaloneToolStripMenuItem.Name = "mCCStandaloneToolStripMenuItem";
            mCCStandaloneToolStripMenuItem.Size = new Size(204, 26);
            mCCStandaloneToolStripMenuItem.Text = "MCC-Standalone";
            mCCStandaloneToolStripMenuItem.Click += mCCStandaloneToolStripMenuItem_Click;
            // 
            // mPLABXToolStripMenuItem
            // 
            mPLABXToolStripMenuItem.Name = "mPLABXToolStripMenuItem";
            mPLABXToolStripMenuItem.Size = new Size(204, 26);
            mPLABXToolStripMenuItem.Text = "MPLABX";
            mPLABXToolStripMenuItem.Click += mPLABXToolStripMenuItem_Click;
            // 
            // vSCodeToolStripMenuItem
            // 
            vSCodeToolStripMenuItem.Name = "vSCodeToolStripMenuItem";
            vSCodeToolStripMenuItem.Size = new Size(204, 26);
            vSCodeToolStripMenuItem.Text = "VS Code";
            vSCodeToolStripMenuItem.Click += vSCodeToolStripMenuItem_Click;
            // 
            // createProjectToolStripMenuItem
            // 
            createProjectToolStripMenuItem.Name = "createProjectToolStripMenuItem";
            createProjectToolStripMenuItem.Size = new Size(185, 26);
            createProjectToolStripMenuItem.Text = "Create Project";
            createProjectToolStripMenuItem.Click += createProjectToolStripMenuItem_Click;
            // 
            // generateToolStripMenuItem
            // 
            generateToolStripMenuItem.Name = "generateToolStripMenuItem";
            generateToolStripMenuItem.Size = new Size(185, 26);
            generateToolStripMenuItem.Text = "Generate";
            generateToolStripMenuItem.Click += generateToolStripMenuItem_Click;
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { cSourceToolStripMenuItem, headerToolStripMenuItem });
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(185, 26);
            newToolStripMenuItem.Text = "New";
            // 
            // cSourceToolStripMenuItem
            // 
            cSourceToolStripMenuItem.Name = "cSourceToolStripMenuItem";
            cSourceToolStripMenuItem.Size = new Size(153, 26);
            cSourceToolStripMenuItem.Text = "source .c";
            cSourceToolStripMenuItem.Click += cSourceToolStripMenuItem_Click;
            // 
            // headerToolStripMenuItem
            // 
            headerToolStripMenuItem.Name = "headerToolStripMenuItem";
            headerToolStripMenuItem.Size = new Size(153, 26);
            headerToolStripMenuItem.Text = "header .h";
            headerToolStripMenuItem.Click += headerToolStripMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openConsoleToolStripMenuItem, closeConsoleToolStripMenuItem, hotkeysToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(75, 24);
            optionsToolStripMenuItem.Text = "Options";
            // 
            // openConsoleToolStripMenuItem
            // 
            openConsoleToolStripMenuItem.Name = "openConsoleToolStripMenuItem";
            openConsoleToolStripMenuItem.Size = new Size(241, 26);
            openConsoleToolStripMenuItem.Text = "Open  Console  Ctrl + '";
            openConsoleToolStripMenuItem.Click += openConsoleToolStripMenuItem_Click;
            // 
            // closeConsoleToolStripMenuItem
            // 
            closeConsoleToolStripMenuItem.Name = "closeConsoleToolStripMenuItem";
            closeConsoleToolStripMenuItem.Size = new Size(241, 26);
            closeConsoleToolStripMenuItem.Text = "Close Console  Ctrl + '";
            closeConsoleToolStripMenuItem.Click += closeConsoleToolStripMenuItem_Click;
            // 
            // hotkeysToolStripMenuItem
            // 
            hotkeysToolStripMenuItem.Name = "hotkeysToolStripMenuItem";
            hotkeysToolStripMenuItem.Size = new Size(241, 26);
            hotkeysToolStripMenuItem.Text = "Hotkeys...";
            hotkeysToolStripMenuItem.Click += hotkeysToolStripMenuItem_Click;
            // 
            // gitToolStripMenuItem
            // 
            gitToolStripMenuItem.Name = "gitToolStripMenuItem";
            gitToolStripMenuItem.Size = new Size(42, 24);
            gitToolStripMenuItem.Text = "Git";
            // 
            // treeView_Project
            // 
            treeView_Project.Dock = DockStyle.Fill;
            treeView_Project.Location = new Point(0, 28);
            treeView_Project.Name = "treeView_Project";
            treeView_Project.Size = new Size(264, 909);
            treeView_Project.TabIndex = 2;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(treeView_Project);
            splitContainer1.Panel1.Controls.Add(menuStrip1);
            splitContainer1.Panel1MinSize = 80;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabControl1);
            splitContainer1.Panel2MinSize = 1000;
            splitContainer1.Size = new Size(1635, 937);
            splitContainer1.SplitterDistance = 264;
            splitContainer1.SplitterWidth = 8;
            splitContainer1.TabIndex = 3;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage_Projects);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1363, 937);
            tabControl1.TabIndex = 1;
            // 
            // tabPage_Projects
            // 
            tabPage_Projects.Controls.Add(treeView_Right);
            tabPage_Projects.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabPage_Projects.Location = new Point(4, 29);
            tabPage_Projects.Name = "tabPage_Projects";
            tabPage_Projects.Padding = new Padding(3);
            tabPage_Projects.Size = new Size(1355, 904);
            tabPage_Projects.TabIndex = 3;
            tabPage_Projects.Text = "Projects";
            tabPage_Projects.UseVisualStyleBackColor = true;
            // 
            // treeView_Right
            // 
            treeView_Right.Dock = DockStyle.Fill;
            treeView_Right.HideSelection = false;
            treeView_Right.Location = new Point(3, 3);
            treeView_Right.Name = "treeView_Right";
            treeView_Right.Size = new Size(1349, 898);
            treeView_Right.TabIndex = 1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1635, 937);
            Controls.Add(splitContainer1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage_Projects.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem mccToolStripMenuItem;
        private TreeView treeView_Project;
        private SplitContainer splitContainer1;
        private ToolStripMenuItem createProjectToolStripMenuItem;
        private ToolStripMenuItem generateToolStripMenuItem;
        private TabControl tabControl1;
        private ToolStripMenuItem openRightToolStripMenuItem;
        private TabPage tabPage_Projects;
        private TreeView treeView_Right;
        private ToolStripMenuItem mCCStandaloneToolStripMenuItem;
        private ToolStripMenuItem mPLABXToolStripMenuItem;
        private ToolStripMenuItem vSCodeToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem cSourceToolStripMenuItem;
        private ToolStripMenuItem headerToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem openConsoleToolStripMenuItem;
        private ToolStripMenuItem closeConsoleToolStripMenuItem;
        private ToolStripMenuItem gitToolStripMenuItem;
        private TabPage tabPage_Git;
        private ComboBox comboGitBranches;
        private Button btnGitCheckout;
        private Button btnGitNewBranch;
        private Button btnGitFetch;
        private Button btnGitPull;
        private Button btnGitPush;
        private Button btnGitRefresh;
        private ListView listViewGitStatus;
        private ColumnHeader colStatus;
        private ColumnHeader colPath;
        private Button btnGitStage;
        private Button btnGitUnstage;
        private TextBox txtGitCommit;
        private Button btnGitCommit;
        private ToolStripMenuItem hotkeysToolStripMenuItem;
        
        private void InitializeGitTabDesigner()
        {
            tabPage_Git = new TabPage();
            tabPage_Git.Text = "Git";
            tabPage_Git.UseVisualStyleBackColor = true;

            // Root layout for Git tab: 3 rows (toolbar, status list, commit row)
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(6)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Top toolbar
            var topBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 6)
            };

            comboGitBranches = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 240
            };

            btnGitCheckout = new Button { Text = "Checkout", TextAlign = ContentAlignment.MiddleCenter , Height = 30 };
            btnGitNewBranch = new Button { Text = "New Branch", TextAlign = ContentAlignment.MiddleCenter , Height = 30 };
            btnGitFetch = new Button { Text = "Fetch", TextAlign = ContentAlignment.MiddleCenter , Height = 30 };
            btnGitPull = new Button { Text = "Pull", TextAlign = ContentAlignment.MiddleCenter , Height = 30 };
            btnGitPush = new Button { Text = "Push", TextAlign = ContentAlignment.MiddleCenter , Height = 30 };
            btnGitRefresh = new Button { Text = "Refresh", TextAlign = ContentAlignment.MiddleCenter , Height = 30 };

            topBar.Controls.Add(comboGitBranches);
            topBar.Controls.Add(btnGitCheckout);
            topBar.Controls.Add(btnGitNewBranch);
            topBar.Controls.Add(btnGitFetch);
            topBar.Controls.Add(btnGitPull);
            topBar.Controls.Add(btnGitPush);
            topBar.Controls.Add(btnGitRefresh);

            // Status list
            listViewGitStatus = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            colStatus = new ColumnHeader { Text = "Status", Width = 140 };
            colPath = new ColumnHeader { Text = "Path", Width = 400 };
            listViewGitStatus.Columns.AddRange(new ColumnHeader[] { colStatus, colPath });

            // Bottom commit row
            var bottomBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 6, 0, 0)
            };
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Stage
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Unstage
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Commit textbox
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Commit button
            bottomBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0)); // spacer (future)

            btnGitStage = new Button { Text = "Stage", Margin = new Padding(0, 0, 6, 0),Height = 30 };
            btnGitUnstage = new Button { Text = "Unstage", Margin = new Padding(0, 0, 6, 0),Height = 30 };
            txtGitCommit = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 6, 0) };
            btnGitCommit = new Button { Text = "Commit", Height = 30 };

            bottomBar.Controls.Add(btnGitStage, 0, 0);
            bottomBar.Controls.Add(btnGitUnstage, 1, 0);
            bottomBar.Controls.Add(txtGitCommit, 2, 0);
            bottomBar.Controls.Add(btnGitCommit, 3, 0);

            // Assemble layout
            layout.Controls.Add(topBar, 0, 0);
            layout.Controls.Add(listViewGitStatus, 0, 1);
            layout.Controls.Add(bottomBar, 0, 2);

            tabPage_Git.Controls.Add(layout);

            // Add to tabs
            tabControl1.Controls.Add(tabPage_Git);

            // Adjust columns when the list or tab resizes
            tabPage_Git.Resize += (s, e) => UpdateGitListColumns();
            listViewGitStatus.Resize += (s, e) => UpdateGitListColumns();
            UpdateGitListColumns();
        }

        private void UpdateGitListColumns()
        {
            if (listViewGitStatus == null || colStatus == null || colPath == null) return;
            int total = listViewGitStatus.ClientSize.Width;
            int statusWidth = 140;
            int scrollbar = SystemInformation.VerticalScrollBarWidth;
            int padding = 8;
            colStatus.Width = statusWidth;
            colPath.Width = System.Math.Max(100, total - statusWidth - scrollbar - padding);
        }
    }
}
