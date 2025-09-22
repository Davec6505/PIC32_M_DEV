using System;
using System.Drawing;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{
    public class FloatingTabsForm : Form
    {
        public TabControl TabHost { get; }

        public FloatingTabsForm()
        {
            Text = "Tabs";
            StartPosition = FormStartPosition.Manual;
            Size = new Size(900, 600);
            ShowInTaskbar = true;

            TabHost = new TabControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(TabHost);

            // Enable drag/drop for this floating host
            TabDragDropManager.Enable(TabHost, isMainHost: false);

            TabHost.ControlAdded += (_, __) => EnsureOpenState();
            TabHost.ControlRemoved += (_, __) => EnsureOpenState();
        }

        private void EnsureOpenState()
        {
            // Auto-close the floating window if empty
            if (TabHost.TabPages.Count == 0)
            {
                // Unregister before closing
                TabDragDropManager.Disable(TabHost);
                Close();
            }
        }
    }
}