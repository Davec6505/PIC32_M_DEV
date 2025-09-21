using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using PIC32Mn_PROJ.Services.Abstractions;
using System.IO;
using System.Reflection;
using System.Xml;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class HighlightingService : IHighlightingService
    {
        public void ApplyBaseC(TextEditor editor)
        {
            var def = HighlightingManager.Instance.GetDefinition("C++");
            if (def == null) return;
            static void SetColor(HighlightingColor color, string? hex, bool? bold = null, bool? italic = null)
            {
                if (hex != null)
                {
                    var drawing = (System.Drawing.Color)System.ComponentModel.TypeDescriptor.GetConverter(typeof(System.Drawing.Color)).ConvertFromString(hex);
                    var media = System.Windows.Media.Color.FromArgb(drawing.A, drawing.R, drawing.G, drawing.B);
                    color.Foreground = new SimpleHighlightingBrush(media);
                }
                if (bold.HasValue) color.FontWeight = bold.Value ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
                if (italic.HasValue) color.FontStyle = italic.Value ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal;
            }
            foreach (var c in def.NamedHighlightingColors)
            {
                switch (c.Name)
                {
                    case "Keyword": SetColor(c, "#FF0080FF", bold: true); break;
                    case "Type": SetColor(c, "#FFFFA500"); break;
                    case "Comment": SetColor(c, "#FF5A995A", italic: true); break;
                    case "String": SetColor(c, "#FFCE9178"); break;
                    case "Number": SetColor(c, "#FFB5CEA8"); break;
                    case "Preprocessor": SetColor(c, "#FF9B9B9B"); break;
                }
            }
            editor.SyntaxHighlighting = def;
        }

        public void EnsureCustomCRegistered(string xshdRootPath)
        {
            const string name = "C-PIC";
            if (HighlightingManager.Instance.GetDefinition(name) != null) return;
            var xshdPath = Path.Combine(xshdRootPath, "dependancies", "highlighting", "c-pic.xshd");
            if (!File.Exists(xshdPath)) return;
            using var fs = File.OpenRead(xshdPath);
            using var reader = new XmlTextReader(fs);
            var def = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting(name, new[] { ".c", ".h" }, def);
        }

        public IHighlightingDefinition? GetForPath(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var asmRes = "PIC32Mn_PROJ.dependancies.Highlighting.asm.xshd";
            var mkRes  = "PIC32Mn_PROJ.dependancies.Highlighting.makefile.xshd";

            if (fileName.Equals("Makefile", System.StringComparison.OrdinalIgnoreCase))
                return LoadFromResource(mkRes);
            if (fileName.Equals("startup", System.StringComparison.OrdinalIgnoreCase))
                return LoadFromResource(asmRes);

            return ext switch
            {
                ".json" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                ".xml"  => HighlightingManager.Instance.GetDefinition("XML"),
                ".s" or ".asm" => LoadFromResource(asmRes),
                ".mk" => LoadFromResource(mkRes),
                _ => null
            };
        }

        private static IHighlightingDefinition? LoadFromResource(string resourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null) return null;
            using var reader = new XmlTextReader(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
    }
}
