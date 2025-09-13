namespace PIC32Mn_PROJ
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private ToolTip toolTip1 = new ToolTip();
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
            createProjectToolStripMenuItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            tabPage_View = new TabPage();
            tabPage_System = new TabPage();
            splitContainer_System = new SplitContainer();
            panelConfigSections = new Panel();
            panel_ClockDiagram = new Panel();
            label_FRCOSC = new Label();
            label_FRCOSCDIV = new Label();
            label_POSCO = new Label();
            checkBox_OutOscON = new CheckBox();
            checkBox_OE = new CheckBox();
            numericUpDown_POSC = new NumericUpDown();
            numericUpDown_SOSC = new NumericUpDown();
            label_OSCIO = new Label();
            comboBox_OSCIOFNC = new ComboBox();
            label_SySClock = new Label();
            comboBox_FCKSM = new ComboBox();
            comboBox_FNOSC = new ComboBox();
            comboBox_FSOSCEN = new ComboBox();
            comboBox_FRCDIV = new ComboBox();
            comboBox_UPLLFSEL = new ComboBox();
            comboBox_FPLLODIV = new ComboBox();
            comboBox_FPLLRNG = new ComboBox();
            comboBox_FPLLICLK = new ComboBox();
            comboBox_FPLLMULT = new ComboBox();
            comboBox_FPLLIDIV = new ComboBox();
            comboBox_POSCMOD = new ComboBox();
            tabPage_Gpio = new TabPage();
            flowPanelPins = new FlowLayoutPanel();
            treeView_Project = new TreeView();
            splitContainer1 = new SplitContainer();
            menuStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage_System.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer_System).BeginInit();
            splitContainer_System.Panel1.SuspendLayout();
            splitContainer_System.Panel2.SuspendLayout();
            splitContainer_System.SuspendLayout();
            panel_ClockDiagram.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_POSC).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SOSC).BeginInit();
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
            menuStrip1.Size = new Size(25, 28);
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
            openToolStripMenuItem.Size = new Size(143, 26);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(143, 26);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(143, 26);
            saveAsToolStripMenuItem.Text = "Save As";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(143, 26);
            exitToolStripMenuItem.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { deviceToolStripMenuItem, createProjectToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(49, 24);
            editToolStripMenuItem.Text = "Edit";
            // 
            // deviceToolStripMenuItem
            // 
            deviceToolStripMenuItem.Name = "deviceToolStripMenuItem";
            deviceToolStripMenuItem.Size = new Size(185, 26);
            deviceToolStripMenuItem.Text = "Device";
            deviceToolStripMenuItem.Click += deviceToolStripMenuItem_Click;
            // 
            // createProjectToolStripMenuItem
            // 
            createProjectToolStripMenuItem.Name = "createProjectToolStripMenuItem";
            createProjectToolStripMenuItem.Size = new Size(185, 26);
            createProjectToolStripMenuItem.Text = "Create Project";
            createProjectToolStripMenuItem.Click += createProjectToolStripMenuItem_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage_View);
            tabControl1.Controls.Add(tabPage_System);
            tabControl1.Controls.Add(tabPage_Gpio);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1606, 937);
            tabControl1.TabIndex = 1;
            // 
            // tabPage_View
            // 
            tabPage_View.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabPage_View.Location = new Point(4, 29);
            tabPage_View.Name = "tabPage_View";
            tabPage_View.Padding = new Padding(3);
            tabPage_View.Size = new Size(1598, 904);
            tabPage_View.TabIndex = 2;
            tabPage_View.Text = "View";
            tabPage_View.UseVisualStyleBackColor = true;
            // 
            // tabPage_System
            // 
            tabPage_System.Controls.Add(splitContainer_System);
            tabPage_System.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabPage_System.Location = new Point(4, 29);
            tabPage_System.Name = "tabPage_System";
            tabPage_System.Padding = new Padding(3);
            tabPage_System.Size = new Size(1598, 904);
            tabPage_System.TabIndex = 0;
            tabPage_System.Text = "System";
            tabPage_System.UseVisualStyleBackColor = true;
            // 
            // splitContainer_System
            // 
            splitContainer_System.Dock = DockStyle.Fill;
            splitContainer_System.FixedPanel = FixedPanel.Panel2;
            splitContainer_System.IsSplitterFixed = true;
            splitContainer_System.Location = new Point(3, 3);
            splitContainer_System.Name = "splitContainer_System";
            // 
            // splitContainer_System.Panel1
            // 
            splitContainer_System.Panel1.AutoScroll = true;
            splitContainer_System.Panel1.Controls.Add(panelConfigSections);
            splitContainer_System.Panel1MinSize = 210;
            // 
            // splitContainer_System.Panel2
            // 
            splitContainer_System.Panel2.AllowDrop = true;
            splitContainer_System.Panel2.AutoScroll = true;
            splitContainer_System.Panel2.BackgroundImageLayout = ImageLayout.None;
            splitContainer_System.Panel2.Controls.Add(panel_ClockDiagram);
            splitContainer_System.Panel2MinSize = 1000;
            splitContainer_System.Size = new Size(1592, 898);
            splitContainer_System.SplitterDistance = 210;
            splitContainer_System.SplitterWidth = 10;
            splitContainer_System.TabIndex = 0;
            // 
            // panelConfigSections
            // 
            panelConfigSections.AutoScroll = true;
            panelConfigSections.AutoSize = true;
            panelConfigSections.Dock = DockStyle.Fill;
            panelConfigSections.Location = new Point(0, 0);
            panelConfigSections.Name = "panelConfigSections";
            panelConfigSections.Size = new Size(210, 898);
            panelConfigSections.TabIndex = 0;
            // 
            // panel_ClockDiagram
            // 
            panel_ClockDiagram.AutoScroll = true;
            panel_ClockDiagram.BackgroundImage = Properties.Resources.clock_154;
            panel_ClockDiagram.BackgroundImageLayout = ImageLayout.Stretch;
            panel_ClockDiagram.Controls.Add(label_FRCOSC);
            panel_ClockDiagram.Controls.Add(label_FRCOSCDIV);
            panel_ClockDiagram.Controls.Add(label_POSCO);
            panel_ClockDiagram.Controls.Add(checkBox_OutOscON);
            panel_ClockDiagram.Controls.Add(checkBox_OE);
            panel_ClockDiagram.Controls.Add(numericUpDown_POSC);
            panel_ClockDiagram.Controls.Add(numericUpDown_SOSC);
            panel_ClockDiagram.Controls.Add(label_OSCIO);
            panel_ClockDiagram.Controls.Add(comboBox_OSCIOFNC);
            panel_ClockDiagram.Controls.Add(label_SySClock);
            panel_ClockDiagram.Controls.Add(comboBox_FCKSM);
            panel_ClockDiagram.Controls.Add(comboBox_FNOSC);
            panel_ClockDiagram.Controls.Add(comboBox_FSOSCEN);
            panel_ClockDiagram.Controls.Add(comboBox_FRCDIV);
            panel_ClockDiagram.Controls.Add(comboBox_UPLLFSEL);
            panel_ClockDiagram.Controls.Add(comboBox_FPLLODIV);
            panel_ClockDiagram.Controls.Add(comboBox_FPLLRNG);
            panel_ClockDiagram.Controls.Add(comboBox_FPLLICLK);
            panel_ClockDiagram.Controls.Add(comboBox_FPLLMULT);
            panel_ClockDiagram.Controls.Add(comboBox_FPLLIDIV);
            panel_ClockDiagram.Controls.Add(comboBox_POSCMOD);
            panel_ClockDiagram.Dock = DockStyle.Fill;
            panel_ClockDiagram.Location = new Point(0, 0);
            panel_ClockDiagram.Name = "panel_ClockDiagram";
            panel_ClockDiagram.Size = new Size(1378, 898);
            panel_ClockDiagram.TabIndex = 0;
            // 
            // label_FRCOSC
            // 
            label_FRCOSC.AutoSize = true;
            label_FRCOSC.Location = new Point(406, 475);
            label_FRCOSC.Name = "label_FRCOSC";
            label_FRCOSC.Size = new Size(87, 20);
            label_FRCOSC.TabIndex = 20;
            label_FRCOSC.Tag = "";
            label_FRCOSC.Text = "8000000 Hz";
            // 
            // label_FRCOSCDIV
            // 
            label_FRCOSCDIV.AutoSize = true;
            label_FRCOSCDIV.Location = new Point(818, 475);
            label_FRCOSCDIV.Name = "label_FRCOSCDIV";
            label_FRCOSCDIV.Size = new Size(45, 20);
            label_FRCOSCDIV.TabIndex = 19;
            label_FRCOSCDIV.Tag = "POSCO_SETTING";
            label_FRCOSCDIV.Text = "####";
            // 
            // label_POSCO
            // 
            label_POSCO.AutoSize = true;
            label_POSCO.Location = new Point(818, 332);
            label_POSCO.Name = "label_POSCO";
            label_POSCO.Size = new Size(45, 20);
            label_POSCO.TabIndex = 18;
            label_POSCO.Tag = "POSCO_SETTING";
            label_POSCO.Text = "####";
            // 
            // checkBox_OutOscON
            // 
            checkBox_OutOscON.AutoSize = true;
            checkBox_OutOscON.BackColor = SystemColors.ScrollBar;
            checkBox_OutOscON.CheckAlign = ContentAlignment.MiddleRight;
            checkBox_OutOscON.Location = new Point(974, 151);
            checkBox_OutOscON.Name = "checkBox_OutOscON";
            checkBox_OutOscON.Size = new Size(53, 24);
            checkBox_OutOscON.TabIndex = 17;
            checkBox_OutOscON.Text = "ON";
            checkBox_OutOscON.TextImageRelation = TextImageRelation.TextBeforeImage;
            checkBox_OutOscON.UseVisualStyleBackColor = false;
            // 
            // checkBox_OE
            // 
            checkBox_OE.AutoSize = true;
            checkBox_OE.Location = new Point(1260, 113);
            checkBox_OE.Name = "checkBox_OE";
            checkBox_OE.Size = new Size(18, 17);
            checkBox_OE.TabIndex = 16;
            checkBox_OE.UseVisualStyleBackColor = true;
            // 
            // numericUpDown_POSC
            // 
            numericUpDown_POSC.BackColor = SystemColors.ScrollBar;
            numericUpDown_POSC.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            numericUpDown_POSC.Location = new Point(645, 313);
            numericUpDown_POSC.Maximum = new decimal(new int[] { 200000000, 0, 0, 0 });
            numericUpDown_POSC.Name = "numericUpDown_POSC";
            numericUpDown_POSC.Size = new Size(118, 27);
            numericUpDown_POSC.TabIndex = 15;
            numericUpDown_POSC.Tag = "label_POSC";
            numericUpDown_POSC.TextAlign = HorizontalAlignment.Center;
            numericUpDown_POSC.Value = new decimal(new int[] { 24000000, 0, 0, 0 });
            // 
            // numericUpDown_SOSC
            // 
            numericUpDown_SOSC.BackColor = SystemColors.ScrollBar;
            numericUpDown_SOSC.Font = new Font("Segoe UI", 10.2F, FontStyle.Italic, GraphicsUnit.Point, 0);
            numericUpDown_SOSC.Location = new Point(393, 653);
            numericUpDown_SOSC.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            numericUpDown_SOSC.Name = "numericUpDown_SOSC";
            numericUpDown_SOSC.Size = new Size(104, 30);
            numericUpDown_SOSC.TabIndex = 14;
            numericUpDown_SOSC.TextAlign = HorizontalAlignment.Center;
            numericUpDown_SOSC.Value = new decimal(new int[] { 32768, 0, 0, 0 });
            // 
            // label_OSCIO
            // 
            label_OSCIO.AutoSize = true;
            label_OSCIO.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            label_OSCIO.Location = new Point(124, 408);
            label_OSCIO.Name = "label_OSCIO";
            label_OSCIO.Size = new Size(55, 17);
            label_OSCIO.TabIndex = 13;
            label_OSCIO.Text = "OSC IO:";
            // 
            // comboBox_OSCIOFNC
            // 
            comboBox_OSCIOFNC.BackColor = SystemColors.ScrollBar;
            comboBox_OSCIOFNC.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_OSCIOFNC.FormattingEnabled = true;
            comboBox_OSCIOFNC.Location = new Point(179, 405);
            comboBox_OSCIOFNC.Name = "comboBox_OSCIOFNC";
            comboBox_OSCIOFNC.Size = new Size(70, 25);
            comboBox_OSCIOFNC.TabIndex = 12;
            comboBox_OSCIOFNC.Tag = "OSCIOFNC";
            comboBox_OSCIOFNC.Text = "OSC IO";
            // 
            // label_SySClock
            // 
            label_SySClock.AutoSize = true;
            label_SySClock.BackColor = SystemColors.ScrollBar;
            label_SySClock.BorderStyle = BorderStyle.Fixed3D;
            label_SySClock.Location = new Point(1182, 486);
            label_SySClock.Name = "label_SySClock";
            label_SySClock.Size = new Size(111, 22);
            label_SySClock.TabIndex = 11;
            label_SySClock.Text = "200000000mhz";
            // 
            // comboBox_FCKSM
            // 
            comboBox_FCKSM.BackColor = SystemColors.ScrollBar;
            comboBox_FCKSM.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FCKSM.FormattingEnabled = true;
            comboBox_FCKSM.Items.AddRange(new object[] { "DIV_1", "DIV_2", "DIV_4", "DIV_8", "DIV_16", "DIV_32", "DIV_64", "DIV_256" });
            comboBox_FCKSM.Location = new Point(1182, 751);
            comboBox_FCKSM.Name = "comboBox_FCKSM";
            comboBox_FCKSM.Size = new Size(114, 25);
            comboBox_FCKSM.TabIndex = 10;
            comboBox_FCKSM.Tag = "FCKSM";
            comboBox_FCKSM.Text = "CKSM";
            // 
            // comboBox_FNOSC
            // 
            comboBox_FNOSC.BackColor = SystemColors.ScrollBar;
            comboBox_FNOSC.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FNOSC.FormattingEnabled = true;
            comboBox_FNOSC.Items.AddRange(new object[] { "DIV_1", "DIV_2", "DIV_4", "DIV_8", "DIV_16", "DIV_32", "DIV_64", "DIV_256" });
            comboBox_FNOSC.Location = new Point(849, 701);
            comboBox_FNOSC.Name = "comboBox_FNOSC";
            comboBox_FNOSC.Size = new Size(91, 25);
            comboBox_FNOSC.TabIndex = 9;
            comboBox_FNOSC.Tag = "FNOSC";
            comboBox_FNOSC.Text = "NOSC";
            // 
            // comboBox_FSOSCEN
            // 
            comboBox_FSOSCEN.BackColor = SystemColors.ScrollBar;
            comboBox_FSOSCEN.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FSOSCEN.FormattingEnabled = true;
            comboBox_FSOSCEN.Location = new Point(300, 701);
            comboBox_FSOSCEN.Name = "comboBox_FSOSCEN";
            comboBox_FSOSCEN.Size = new Size(81, 20);
            comboBox_FSOSCEN.TabIndex = 8;
            comboBox_FSOSCEN.Tag = "FSOSCEN";
            comboBox_FSOSCEN.Text = "SOSCEN";
            // 
            // comboBox_FRCDIV
            // 
            comboBox_FRCDIV.BackColor = SystemColors.ScrollBar;
            comboBox_FRCDIV.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FRCDIV.FormattingEnabled = true;
            comboBox_FRCDIV.Items.AddRange(new object[] { "DIV_1", "DIV_2", "DIV_4", "DIV_8", "DIV_16", "DIV_32", "DIV_64", "DIV_256" });
            comboBox_FRCDIV.Location = new Point(652, 513);
            comboBox_FRCDIV.Name = "comboBox_FRCDIV";
            comboBox_FRCDIV.Size = new Size(91, 25);
            comboBox_FRCDIV.TabIndex = 7;
            comboBox_FRCDIV.Tag = "";
            comboBox_FRCDIV.Text = "FRCDIV";
            // 
            // comboBox_UPLLFSEL
            // 
            comboBox_UPLLFSEL.BackColor = SystemColors.ScrollBar;
            comboBox_UPLLFSEL.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_UPLLFSEL.FormattingEnabled = true;
            comboBox_UPLLFSEL.Location = new Point(261, 80);
            comboBox_UPLLFSEL.Name = "comboBox_UPLLFSEL";
            comboBox_UPLLFSEL.Size = new Size(81, 20);
            comboBox_UPLLFSEL.TabIndex = 6;
            comboBox_UPLLFSEL.Tag = "UPLLFSEL";
            comboBox_UPLLFSEL.Text = "UPLLFSEL";
            // 
            // comboBox_FPLLODIV
            // 
            comboBox_FPLLODIV.BackColor = SystemColors.ScrollBar;
            comboBox_FPLLODIV.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FPLLODIV.FormattingEnabled = true;
            comboBox_FPLLODIV.Location = new Point(609, 191);
            comboBox_FPLLODIV.Name = "comboBox_FPLLODIV";
            comboBox_FPLLODIV.Size = new Size(70, 20);
            comboBox_FPLLODIV.TabIndex = 5;
            comboBox_FPLLODIV.Tag = "FPLLODIV";
            comboBox_FPLLODIV.Text = "PLLODIV";
            // 
            // comboBox_FPLLRNG
            // 
            comboBox_FPLLRNG.BackColor = SystemColors.ScrollBar;
            comboBox_FPLLRNG.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FPLLRNG.FormattingEnabled = true;
            comboBox_FPLLRNG.Location = new Point(477, 221);
            comboBox_FPLLRNG.Name = "comboBox_FPLLRNG";
            comboBox_FPLLRNG.Size = new Size(92, 20);
            comboBox_FPLLRNG.TabIndex = 4;
            comboBox_FPLLRNG.Tag = "FPLLRNG";
            comboBox_FPLLRNG.Text = "PLLRNG";
            // 
            // comboBox_FPLLICLK
            // 
            comboBox_FPLLICLK.BackColor = SystemColors.ScrollBar;
            comboBox_FPLLICLK.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FPLLICLK.FormattingEnabled = true;
            comboBox_FPLLICLK.Location = new Point(144, 234);
            comboBox_FPLLICLK.Name = "comboBox_FPLLICLK";
            comboBox_FPLLICLK.Size = new Size(81, 20);
            comboBox_FPLLICLK.TabIndex = 3;
            comboBox_FPLLICLK.Tag = "FPLLICLK";
            comboBox_FPLLICLK.Text = "PLLICLK";
            // 
            // comboBox_FPLLMULT
            // 
            comboBox_FPLLMULT.BackColor = SystemColors.ScrollBar;
            comboBox_FPLLMULT.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FPLLMULT.FormattingEnabled = true;
            comboBox_FPLLMULT.Location = new Point(363, 234);
            comboBox_FPLLMULT.Name = "comboBox_FPLLMULT";
            comboBox_FPLLMULT.Size = new Size(70, 20);
            comboBox_FPLLMULT.TabIndex = 2;
            comboBox_FPLLMULT.Tag = "FPLLMULT";
            comboBox_FPLLMULT.Text = "PLLMULT";
            // 
            // comboBox_FPLLIDIV
            // 
            comboBox_FPLLIDIV.BackColor = SystemColors.ScrollBar;
            comboBox_FPLLIDIV.Font = new Font("Segoe UI", 6F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_FPLLIDIV.FormattingEnabled = true;
            comboBox_FPLLIDIV.Location = new Point(249, 214);
            comboBox_FPLLIDIV.Name = "comboBox_FPLLIDIV";
            comboBox_FPLLIDIV.Size = new Size(70, 20);
            comboBox_FPLLIDIV.TabIndex = 1;
            comboBox_FPLLIDIV.Tag = "FPLLIDIV";
            comboBox_FPLLIDIV.Text = "PLLIDIV";
            // 
            // comboBox_POSCMOD
            // 
            comboBox_POSCMOD.BackColor = SystemColors.ScrollBar;
            comboBox_POSCMOD.Font = new Font("Segoe UI", 7.8F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            comboBox_POSCMOD.FormattingEnabled = true;
            comboBox_POSCMOD.Location = new Point(486, 335);
            comboBox_POSCMOD.Name = "comboBox_POSCMOD";
            comboBox_POSCMOD.Size = new Size(55, 25);
            comboBox_POSCMOD.TabIndex = 0;
            comboBox_POSCMOD.Tag = "POSCMOD";
            comboBox_POSCMOD.Text = "POSCMOD";
            // 
            // tabPage_Gpio
            // 
            tabPage_Gpio.Controls.Add(flowPanelPins);
            tabPage_Gpio.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 0);
            tabPage_Gpio.Location = new Point(4, 29);
            tabPage_Gpio.Name = "tabPage_Gpio";
            tabPage_Gpio.Padding = new Padding(3);
            tabPage_Gpio.Size = new Size(1594, 904);
            tabPage_Gpio.TabIndex = 1;
            tabPage_Gpio.Text = "Gpio";
            tabPage_Gpio.UseVisualStyleBackColor = true;
            // 
            // flowPanelPins
            // 
            flowPanelPins.AutoScroll = true;
            flowPanelPins.Dock = DockStyle.Fill;
            flowPanelPins.FlowDirection = FlowDirection.TopDown;
            flowPanelPins.Location = new Point(3, 3);
            flowPanelPins.Name = "flowPanelPins";
            flowPanelPins.Size = new Size(1588, 898);
            flowPanelPins.TabIndex = 0;
            flowPanelPins.WrapContents = false;
            // 
            // treeView_Project
            // 
            treeView_Project.Dock = DockStyle.Fill;
            treeView_Project.Location = new Point(0, 28);
            treeView_Project.Name = "treeView_Project";
            treeView_Project.Size = new Size(25, 909);
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
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(tabControl1);
            splitContainer1.Panel2MinSize = 1480;
            splitContainer1.Size = new Size(1635, 937);
            splitContainer1.SplitterDistance = 25;
            splitContainer1.SplitterWidth = 8;
            splitContainer1.TabIndex = 3;
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
            tabControl1.ResumeLayout(false);
            tabPage_System.ResumeLayout(false);
            splitContainer_System.Panel1.ResumeLayout(false);
            splitContainer_System.Panel1.PerformLayout();
            splitContainer_System.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer_System).EndInit();
            splitContainer_System.ResumeLayout(false);
            panel_ClockDiagram.ResumeLayout(false);
            panel_ClockDiagram.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_POSC).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SOSC).EndInit();
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
        private ToolStripMenuItem createProjectToolStripMenuItem;
        private TabPage tabPage_View;
        private SplitContainer splitContainer_System;
        private Panel panelConfigSections;
        private Panel panel_ClockDiagram;
        private ComboBox comboBox_POSCMOD;
        private ComboBox comboBox_FPLLIDIV;
        private ComboBox comboBox_FPLLMULT;
        private ComboBox comboBox_FPLLICLK;
        private ComboBox comboBox_FPLLRNG;
        private ComboBox comboBox_FPLLODIV;
        private ComboBox comboBox_UPLLFSEL;
        private ComboBox comboBox_FRCDIV;
        private ComboBox comboBox_FSOSCEN;
        private ComboBox comboBox_FNOSC;
        private ComboBox comboBox_FCKSM;
        private Label label_SySClock;
        private ComboBox comboBox_OSCIOFNC;
        private Label label_OSCIO;
        private NumericUpDown numericUpDown_SOSC;
        private NumericUpDown numericUpDown_POSC;
        private CheckBox checkBox_OE;
        private CheckBox checkBox_OutOscON;
        private Label label_POSCO;
        private Label label_FRCOSC;
        private Label label_FRCOSCDIV;
    }
}
