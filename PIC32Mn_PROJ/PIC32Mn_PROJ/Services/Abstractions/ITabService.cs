using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface ITabService
    {
        void OpenFile(TabControl tabControl, string filePath, string? xshdRootPath = null);
        void CloseActive(TabControl tabControl);
        void CloseFile(TabControl tabControl, string filePath);
    }
}
