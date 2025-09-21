using ICSharpCode.AvalonEdit;
using System.Threading.Tasks;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IEditorService
    {
        string? CurrentPath { get; }
        string Text { get; set; }
        Task OpenAsync(string path);
        Task<bool> SaveAsync(string? path = null);
        Task<bool> SaveAsAsync(string initialDir, string defaultFileName);
        void NewC(TextEditor editor);
        void NewHeader(TextEditor editor);
        void Close(TextEditor editor);
        event EventHandler? Saved;
        event EventHandler? Opened;
        event EventHandler? Closed;
    }
}
