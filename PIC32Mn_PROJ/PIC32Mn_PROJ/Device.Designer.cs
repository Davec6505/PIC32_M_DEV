namespace PIC32Mn_PROJ
{
    partial class Device
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            comboBox_Device = new ComboBox();
            label1 = new Label();
            buttonDeviceSave = new Button();
            SuspendLayout();
            // 
            // comboBox_Device
            // 
            comboBox_Device.FormattingEnabled = true;
            comboBox_Device.Items.AddRange(new object[] { "PIC32MZ1024EFH064", "PIC32MZ1024EFH100", "PIC32MZ1024EFH124", "PIC32MZ1024EFH144", "PIC32MZ2024EFH064", "PIC32MZ2024EFH100", "PIC32MZ2024EFH124", "PIC32MZ2024EFH144" });
            comboBox_Device.Location = new Point(189, 45);
            comboBox_Device.Name = "comboBox_Device";
            comboBox_Device.Size = new Size(219, 28);
            comboBox_Device.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 45);
            label1.Name = "label1";
            label1.Size = new Size(159, 20);
            label1.TabIndex = 1;
            label1.Text = "list of pic32mz devices";
            // 
            // buttonDeviceSave
            // 
            buttonDeviceSave.Location = new Point(433, 45);
            buttonDeviceSave.Name = "buttonDeviceSave";
            buttonDeviceSave.Size = new Size(94, 29);
            buttonDeviceSave.TabIndex = 2;
            buttonDeviceSave.Text = "Save";
            buttonDeviceSave.UseVisualStyleBackColor = true;
            buttonDeviceSave.Click += buttonDeviceSave_Click;
            // 
            // Device
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(548, 109);
            Controls.Add(buttonDeviceSave);
            Controls.Add(label1);
            Controls.Add(comboBox_Device);
            Name = "Device";
            Text = "Device";
            Load += Device_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox comboBox_Device;
        private Label label1;
        private Button buttonDeviceSave;
    }
}