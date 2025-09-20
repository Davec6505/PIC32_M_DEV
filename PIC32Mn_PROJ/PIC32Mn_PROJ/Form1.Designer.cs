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
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            mccToolStripMenuItem = new ToolStripMenuItem();
            mCCStandaloneToolStripMenuItem = new ToolStripMenuItem();
            mPLABXToolStripMenuItem = new ToolStripMenuItem();
            createProjectToolStripMenuItem = new ToolStripMenuItem();
            generateToolStripMenuItem = new ToolStripMenuItem();
            treeView_Project = new TreeView();
            splitContainer1 = new SplitContainer();
            tabControl1 = new TabControl();
            tabPage_Projects = new TabPage();
            treeView_Right = new TreeView();
            tabPage_View = new TabPage();
            vSCodeToolStripMenuItem = new ToolStripMenuItem();
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
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(135, 28);
            menuStrip1.TabIndex = 0;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, openRightToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(46, 24);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(167, 26);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // openRightToolStripMenuItem
            // 
            openRightToolStripMenuItem.Name = "openRightToolStripMenuItem";
            openRightToolStripMenuItem.Size = new Size(167, 26);
            openRightToolStripMenuItem.Text = "Open Right";
            openRightToolStripMenuItem.Click += openRightToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(167, 26);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(167, 26);
            saveAsToolStripMenuItem.Text = "Save As";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(167, 26);
            exitToolStripMenuItem.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { mccToolStripMenuItem, createProjectToolStripMenuItem, generateToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(49, 24);
            editToolStripMenuItem.Text = "Edit";
            // 
            // mccToolStripMenuItem
            // 
            mccToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { mCCStandaloneToolStripMenuItem, mPLABXToolStripMenuItem, vSCodeToolStripMenuItem });
            mccToolStripMenuItem.Name = "mccToolStripMenuItem";
            mccToolStripMenuItem.Size = new Size(224, 26);
            mccToolStripMenuItem.Text = "Mirror Project";
            // 
            // mCCStandaloneToolStripMenuItem
            // 
            mCCStandaloneToolStripMenuItem.Name = "mCCStandaloneToolStripMenuItem";
            mCCStandaloneToolStripMenuItem.Size = new Size(224, 26);
            mCCStandaloneToolStripMenuItem.Text = "MCC-Standalone";
            mCCStandaloneToolStripMenuItem.Click += mCCStandaloneToolStripMenuItem_Click;
            // 
            // mPLABXToolStripMenuItem
            // 
            mPLABXToolStripMenuItem.Name = "mPLABXToolStripMenuItem";
            mPLABXToolStripMenuItem.Size = new Size(224, 26);
            mPLABXToolStripMenuItem.Text = "MPLABX";
            mPLABXToolStripMenuItem.Click += mPLABXToolStripMenuItem_Click;
            // 
            // createProjectToolStripMenuItem
            // 
            createProjectToolStripMenuItem.Name = "createProjectToolStripMenuItem";
            createProjectToolStripMenuItem.Size = new Size(224, 26);
            createProjectToolStripMenuItem.Text = "Create Project";
            createProjectToolStripMenuItem.Click += createProjectToolStripMenuItem_Click;
            // 
            // generateToolStripMenuItem
            // 
            generateToolStripMenuItem.Name = "generateToolStripMenuItem";
            generateToolStripMenuItem.Size = new Size(224, 26);
            generateToolStripMenuItem.Text = "Generate";
            generateToolStripMenuItem.Click += generateToolStripMenuItem_Click;
            // 
            // treeView_Project
            // 
            treeView_Project.Dock = DockStyle.Fill;
            treeView_Project.Location = new Point(0, 28);
            treeView_Project.Name = "treeView_Project";
            treeView_Project.Size = new Size(135, 909);
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
            splitContainer1.SplitterDistance = 135;
            splitContainer1.SplitterWidth = 8;
            splitContainer1.TabIndex = 3;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage_Projects);
            tabControl1.Controls.Add(tabPage_View);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1492, 937);
            tabControl1.TabIndex = 1;
            // 
            // tabPage_Projects
            // 
            tabPage_Projects.Controls.Add(treeView_Right);
            tabPage_Projects.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabPage_Projects.Location = new Point(4, 29);
            tabPage_Projects.Name = "tabPage_Projects";
            tabPage_Projects.Padding = new Padding(3);
            tabPage_Projects.Size = new Size(1484, 904);
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
            treeView_Right.Size = new Size(1478, 898);
            treeView_Right.TabIndex = 1;
            // 
            // tabPage_View
            // 
            tabPage_View.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabPage_View.Location = new Point(4, 29);
            tabPage_View.Name = "tabPage_View";
            tabPage_View.Padding = new Padding(3);
            tabPage_View.Size = new Size(1480, 904);
            tabPage_View.TabIndex = 2;
            tabPage_View.Text = "View";
            tabPage_View.UseVisualStyleBackColor = true;
            // 
            // vSCodeToolStripMenuItem
            // 
            vSCodeToolStripMenuItem.Name = "vSCodeToolStripMenuItem";
            vSCodeToolStripMenuItem.Size = new Size(224, 26);
            vSCodeToolStripMenuItem.Text = "VS Code";
            vSCodeToolStripMenuItem.Click += vSCodeToolStripMenuItem_Click;
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
        private TabPage tabPage_View;

        private ToolStripMenuItem openRightToolStripMenuItem;
        private TabPage tabPage_Projects;
        private TreeView treeView_Right;
        private ToolStripMenuItem mCCStandaloneToolStripMenuItem;
        private ToolStripMenuItem mPLABXToolStripMenuItem;
        private ToolStripMenuItem vSCodeToolStripMenuItem;
    }
}
