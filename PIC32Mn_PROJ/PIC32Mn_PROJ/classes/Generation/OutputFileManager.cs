using System.Collections.Generic;
using System.IO;

namespace PIC32Mn_PROJ.Generation
{
    public static class OutputFileManager
    {
        public static void Write(string projectDir, IEnumerable<GeneratedFile> files)
        {
            foreach (var f in files)
            {
                var full = Path.Combine(projectDir, f.RelativePath);
                var dir = Path.GetDirectoryName(full);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                File.WriteAllText(full, f.Content);
            }
        }
    }
}
