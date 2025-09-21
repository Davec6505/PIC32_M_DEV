using System.IO;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public enum OverwritePolicy { Ask, YesToAll, NoToAll }

    public interface IFileSystemService
    {
        void CopyDirectory(string sourceDir, string destDir);
        void CopyDirectoryWithPrompt(string sourceDir, string destDir, ref OverwritePolicy policy);
        bool TryCopyFileWithPrompt(string srcFile, string destFile, ref OverwritePolicy policy);
        bool IsUnderRoot(string path, string rootPath);
    }
}
