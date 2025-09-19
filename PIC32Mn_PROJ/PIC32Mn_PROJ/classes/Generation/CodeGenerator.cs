using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace PIC32Mn_PROJ.Generation
{
    // Emits C code from IntermediateModel (modular per peripheral)
    public static class CodeGenerator
    {
        public static IEnumerable<GeneratedFile> Generate(IntermediateModel model)
        {
            var files = new List<GeneratedFile>();

            // One init header and source per peripheral
            foreach (var per in model.Peripherals)
            {
                files.Add(GeneratePeripheralHeader(model, per));
                files.Add(GeneratePeripheralSource(model, per));
            }

            return files;
        }

        private static GeneratedFile GeneratePeripheralHeader(IntermediateModel model, PeripheralModel per)
        {
            var sb = new StringBuilder();
            var guard = $"PLIB_{per.Name.ToUpper()}_H";
            sb.AppendLine($"#ifndef {guard}\n#define {guard}");
            sb.AppendLine("#include <device.h>\n#include <stdint.h>\n#include <stdbool.h>\n#include <stddef.h>\n");
            sb.AppendLine($"void {per.Name}_Initialize(void);");
            sb.AppendLine($"#endif // {guard}");

            return new GeneratedFile
            {
                RelativePath = $"incs/plib_{per.Name}.h",
                Content = sb.ToString()
            };
        }

        private static GeneratedFile GeneratePeripheralSource(IntermediateModel model, PeripheralModel per)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"#include \"plib_{per.Name}.h\"");
            sb.AppendLine();
            sb.AppendLine($"void {per.Name}_Initialize(void)");
            sb.AppendLine("{");
            sb.AppendLine("    // Settings");
            foreach (var kv in per.Settings)
            {
                sb.AppendLine($"    // {kv.Key} = {kv.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("    // Pins");
            foreach (var pin in per.Pins)
            {
                var port = pin.Port;
                var bit = pin.Pin;
                if (pin.Direction.Equals("out", System.StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"    TRIS{port}CLR = (1U << {bit});");
                }
                else if (pin.Direction.Equals("in", System.StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"    TRIS{port}SET = (1U << {bit});");
                }
                sb.AppendLine($"    ANSEL{port}CLR = (1U << {bit}); // digital");
            }
            sb.AppendLine("}");

            return new GeneratedFile
            {
                RelativePath = $"srcs/plib_{per.Name}.c",
                Content = sb.ToString()
            };
        }
    }
}
