using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IHighlightingService
    {
        void ApplyBaseC(TextEditor editor);
        void EnsureCustomCRegistered(string xshdRootPath);
        IHighlightingDefinition? GetForPath(string filePath);
    }
}
