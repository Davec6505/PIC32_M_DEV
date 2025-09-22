using System;
using System.Windows.Forms;

namespace PIC32Mn_PROJ
{
    partial class Form1
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Enable full-featured tab drag/drop management on the main tab control
            TabDragDropManager.Enable(tabControl1, isMainHost: true);
        }
    }
}