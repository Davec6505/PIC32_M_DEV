using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIC32Mn_PROJ.classes
{
    internal static class C_FileBuilder
    {


        internal static string BuildCSourceTemplate(string fileName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine($" * File: {fileName}");
            sb.AppendLine($" * Created: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine(" */");
            sb.AppendLine();
            sb.AppendLine("#include <stdint.h>");
            sb.AppendLine("#include <stdbool.h>");
            sb.AppendLine();
            sb.AppendLine("/* TODO: add your functions here */");
            sb.AppendLine();
            return sb.ToString();
        }


        internal static string BuildHeaderTemplate(string fileName)
        {
            var guard = MakeIncludeGuard(fileName);
            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine($" * File: {fileName}");
            sb.AppendLine($" * Created: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine(" */");
            sb.AppendLine();
            sb.AppendLine($"#ifndef {guard}");
            sb.AppendLine($"#define {guard}");
            sb.AppendLine();
            sb.AppendLine("#ifdef __cplusplus");
            sb.AppendLine("extern \"C\" {");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine("// TODO: Add declarations");
            sb.AppendLine();
            sb.AppendLine("#ifdef __cplusplus");
            sb.AppendLine("} // extern \"C\"");
            sb.AppendLine("#endif");
            sb.AppendLine();
            sb.AppendLine($"#endif /* {guard} */");
            return sb.ToString();
        }

        private static string MakeIncludeGuard(string fileName)
        {
            // Convert to UPPER_SNAKE_CASE and ensure it ends with _H
            var name = Path.GetFileName(fileName);
            var withoutExt = Path.GetFileNameWithoutExtension(name);
            var chars = withoutExt
                .Select(c => char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : '_')
                .ToArray();
            var guard = new string(chars);

            // Collapse multiple underscores
            while (guard.Contains("__"))
                guard = guard.Replace("__", "_");

            if (!guard.EndsWith("_H", StringComparison.Ordinal))
                guard += "_H";

            // Guards cannot start with a digit; prefix if needed
            if (guard.Length > 0 && char.IsDigit(guard[0]))
                guard = "_" + guard;

            return guard;
        }
    }
}
