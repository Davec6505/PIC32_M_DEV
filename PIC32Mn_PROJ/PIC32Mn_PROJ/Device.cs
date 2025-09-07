using PIC32Mn_PROJ.classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{
    delegate void SaveDeviceDelegate(string device);
    public partial class Device : Form
    {
        public string? _Device { get; set; }
        public Device(){InitializeComponent();}
        public Device(string? device = null):this()
        {
            this._Device = device;
        }

        private void Device_Load(object sender, EventArgs e)
        {
            if (comboBox_Device.Items.Contains(_Device))
            {
                comboBox_Device.SelectedItem = _Device;
            }
        }


        private void comboBox_Device_SelectedIndexChanged(object sender, EventArgs e)
        {
            _Device = comboBox_Device.SelectedItem.ToString();
        }

        private void buttonDeviceSave_Click(object sender, EventArgs e)
        {
                SaveDevice(comboBox_Device.SelectedItem.ToString());
        }

        public void SaveDevice(string? device = null)
        {
            if (!string.IsNullOrEmpty(device))
            {
                this._Device = device;
                MessageBox.Show($"Device '{_Device}' saved successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a device.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


    }
}
