namespace PIC32Mn_PROJ
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            deviceToolStripMenuItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            tabPage_System = new TabPage();
            tabPage_Gpio = new TabPage();
            flowPanelPins = new FlowLayoutPanel();
            treeView_Project = new TreeView();
            splitContainer1 = new SplitContainer();
            menuStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage_Gpio.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(200, 28);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem, exitToolStripMenuItem });
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
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(224, 26);
            saveToolStripMenuItem.Text = "Save";
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(224, 26);
            saveAsToolStripMenuItem.Text = "Save As";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(224, 26);
            exitToolStripMenuItem.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { deviceToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(49, 24);
            editToolStripMenuItem.Text = "Edit";
            // 
            // deviceToolStripMenuItem
            // 
            deviceToolStripMenuItem.Name = "deviceToolStripMenuItem";
            deviceToolStripMenuItem.Size = new Size(224, 26);
            deviceToolStripMenuItem.Text = "Device";
            deviceToolStripMenuItem.Click += deviceToolStripMenuItem_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage_System);
            tabControl1.Controls.Add(tabPage_Gpio);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1001, 839);
            tabControl1.TabIndex = 1;
            // 
            // tabPage_System
            // 
            tabPage_System.Location = new Point(4, 29);
            tabPage_System.Name = "tabPage_System";
            tabPage_System.Padding = new Padding(3);
            tabPage_System.Size = new Size(993, 806);
            tabPage_System.TabIndex = 0;
            tabPage_System.Text = "SYSTEM";
            tabPage_System.UseVisualStyleBackColor = true;
            // 
            // tabPage_Gpio
            // 
            tabPage_Gpio.Controls.Add(flowPanelPins);
            tabPage_Gpio.Location = new Point(4, 29);
            tabPage_Gpio.Name = "tabPage_Gpio";
            tabPage_Gpio.Padding = new Padding(3);
            tabPage_Gpio.Size = new Size(993, 806);
            tabPage_Gpio.TabIndex = 1;
            tabPage_Gpio.Text = "GPIO";
            tabPage_Gpio.UseVisualStyleBackColor = true;
            // 
            // flowPanelPins
            // 
            flowPanelPins.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowPanelPins.AutoScroll = true;
            flowPanelPins.FlowDirection = FlowDirection.TopDown;
            flowPanelPins.Location = new Point(3, 3);
            flowPanelPins.Name = "flowPanelPins";
            flowPanelPins.Size = new Size(1195, 772);
            flowPanelPins.TabIndex = 0;
            flowPanelPins.WrapContents = false;
            // 
            // treeView_Project
            // 
            treeView_Project.Dock = DockStyle.Fill;
            treeView_Project.Location = new Point(0, 28);
            treeView_Project.Name = "treeView_Project";
            treeView_Project.Size = new Size(200, 811);
            treeView_Project.TabIndex = 2;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(treeView_Project);
            splitContainer1.Panel1.Controls.Add(menuStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabControl1);
            splitContainer1.Size = new Size(1209, 839);
            splitContainer1.SplitterDistance = 200;
            splitContainer1.SplitterWidth = 8;
            splitContainer1.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1209, 839);
            Controls.Add(splitContainer1);
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage_Gpio.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private TabControl tabControl1;
        private TabPage tabPage_System;
        private TabPage tabPage_Gpio;
        private FlowLayoutPanel flowPanelPins;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem deviceToolStripMenuItem;
        private TreeView treeView_Project;
        private SplitContainer splitContainer1;
    }
}
