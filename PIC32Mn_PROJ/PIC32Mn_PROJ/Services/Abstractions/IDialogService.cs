using System;
using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IDialogService
    {
        // Common dialogs
        DialogResult Confirm(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.YesNo, MessageBoxIcon icon = MessageBoxIcon.Question);
        void Info(string message, string title);
        void Error(string message, string title);

        // Folder picker (returns null if cancelled)
        string? PickFolder(string title, string? initialDirectory = null);

        // Save file dialog (returns null if cancelled)
        string? SaveFile(string title, string filter, string? initialDirectory = null, string? defaultFileName = null, string? defaultExt = null);
    }
}
