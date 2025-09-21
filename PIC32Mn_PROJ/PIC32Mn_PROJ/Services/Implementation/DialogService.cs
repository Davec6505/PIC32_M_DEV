using Microsoft.WindowsAPICodePack.Dialogs;
using PIC32Mn_PROJ.Services.Abstractions;
using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class DialogService : IDialogService
    {
        public DialogResult Confirm(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.YesNo, MessageBoxIcon icon = MessageBoxIcon.Question)
            => MessageBox.Show(message, title, buttons, icon);

        public void Info(string message, string title)
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public void Error(string message, string title)
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public string? PickFolder(string title, string? initialDirectory = null)
        {
            using var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = title,
                InitialDirectory = string.IsNullOrEmpty(initialDirectory) ? null : initialDirectory
            };
            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
        }

        public string? SaveFile(string title, string filter, string? initialDirectory = null, string? defaultFileName = null, string? defaultExt = null)
        {
            using var sfd = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                InitialDirectory = initialDirectory ?? string.Empty,
                FileName = defaultFileName ?? string.Empty,
                AddExtension = true,
                OverwritePrompt = true,
                ValidateNames = true
            };
            if (!string.IsNullOrWhiteSpace(defaultExt)) sfd.DefaultExt = defaultExt!.TrimStart('.');
            return sfd.ShowDialog() == DialogResult.OK ? sfd.FileName : null;
        }
    }
}
