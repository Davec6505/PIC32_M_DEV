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
        public Device()
        {
            InitializeComponent();
        }
        public Device(string? device = null)
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

        private void buttonDeviceSave_Click(object sender, EventArgs e)
        {

        }

        private void comboBox_Device_SelectedIndexChanged(object sender, EventArgs e)
        {
            _Device = comboBox_Device.SelectedItem.ToString();
        }

        public void SaveDevice()
        {
            if (!string.IsNullOrEmpty(_Device))
            {
                // Save the selected device to application settings
                Properties.Settings.Default.SelectedDevice = _Device;
                Properties.Settings.Default.Save();
            }
        }
    }
}
