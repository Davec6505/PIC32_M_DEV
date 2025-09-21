using PIC32Mn_PROJ.Services.Abstractions;
using System.IO;
using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class FileSystemService : IFileSystemService
    {
        private readonly IDialogService _dialogs;
        public FileSystemService(IDialogService dialogs) => _dialogs = dialogs;

        public void CopyDirectory(string sourceDir, string destDir)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;
            Directory.CreateDirectory(destDir);
            foreach (var file in src.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(destDir, file.Name);
                file.CopyTo(target, overwrite: true);
                File.SetAttributes(target, FileAttributes.Normal);
            }
            foreach (var dir in src.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                CopyDirectory(dir.FullName, Path.Combine(destDir, dir.Name));
            }
        }

        public void CopyDirectoryWithPrompt(string sourceDir, string destDir, ref OverwritePolicy policy)
        {
            var src = new DirectoryInfo(sourceDir);
            if (!src.Exists) return;
            Directory.CreateDirectory(destDir);
            foreach (var file in src.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                var target = Path.Combine(destDir, file.Name);
                TryCopyFileWithPrompt(file.FullName, target, ref policy);
            }
            foreach (var dir in src.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                var subDest = Path.Combine(destDir, dir.Name);
                CopyDirectoryWithPrompt(dir.FullName, subDest, ref policy);
            }
        }

        public bool TryCopyFileWithPrompt(string srcFile, string destFile, ref OverwritePolicy policy)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            if (!File.Exists(destFile))
            {
                File.Copy(srcFile, destFile, overwrite: true);
                File.SetAttributes(destFile, FileAttributes.Normal);
                return true;
            }

            switch (policy)
            {
                case OverwritePolicy.YesToAll:
                    File.Copy(srcFile, destFile, overwrite: true);
                    File.SetAttributes(destFile, FileAttributes.Normal);
                    return true;
                case OverwritePolicy.NoToAll:
                    return false;
                case OverwritePolicy.Ask:
                default:
                    var res = _dialogs.Confirm(
                        $"The file already exists:\n{destFile}\nDo you want to replace it?",
                        "Copy and Replace",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (res == DialogResult.Cancel)
                        throw new OperationCanceledException("User cancelled copy.");

                    if (res == DialogResult.Yes)
                    {
                        var applyAllReplace = _dialogs.Confirm(
                            "Apply this choice (Replace) to all remaining existing files?",
                            "Apply to All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        if (applyAllReplace == DialogResult.Yes)
                            policy = OverwritePolicy.YesToAll;

                        File.Copy(srcFile, destFile, overwrite: true);
                        File.SetAttributes(destFile, FileAttributes.Normal);
                        return true;
                    }
                    else
                    {
                        var applyAllSkip = _dialogs.Confirm(
                            "Apply this choice (Skip) to all remaining existing files?",
                            "Apply to All",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        if (applyAllSkip == DialogResult.Yes)
                            policy = OverwritePolicy.NoToAll;
                        return false;
                    }
            }
        }

        public bool IsUnderRoot(string path, string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return false;
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase);
        }
    }
}
