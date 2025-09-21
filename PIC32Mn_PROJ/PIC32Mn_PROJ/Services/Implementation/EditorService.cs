using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using PIC32Mn_PROJ.Services.Abstractions;
using PIC32Mn_PROJ.classes;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class EditorService : IEditorService
    {
        private readonly IHighlightingService _hl;
        private readonly IDialogService _dialogs;
        private readonly IProjectTreeService _tree;
        private readonly ISettingsService _settings;
        public string? CurrentPath { get; private set; }
        public string Text { get; set; } = string.Empty;

        public event EventHandler? Saved;
        public event EventHandler? Opened;
        public event EventHandler? Closed;

        public EditorService(IHighlightingService hl, IDialogService dialogs, IProjectTreeService tree, ISettingsService settings)
        {
            _hl = hl; _dialogs = dialogs; _tree = tree; _settings = settings;
        }

        public async Task OpenAsync(string path)
        {
            if (!File.Exists(path)) return;
            Text = await File.ReadAllTextAsync(path);
            CurrentPath = path;
            Opened?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> SaveAsync(string? path = null)
        {
            path ??= CurrentPath;
            if (string.IsNullOrEmpty(path)) return false;
            await File.WriteAllTextAsync(path, Text, Encoding.UTF8);
            CurrentPath = path;
            Saved?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public async Task<bool> SaveAsAsync(string initialDir, string defaultFileName)
        {
            var filter =
                "C/C header/source (*.c;*.h)|*.c;*.h|" +
                "Assembly (*.s;*.asm)|*.s;*.asm|" +
                "Makefiles (Makefile;*.mk)|Makefile;*.mk|" +
                "JSON (*.json)|*.json|" +
                "XML (*.xml)|*.xml|" +
                "All files (*.*)|*.*";
            var ext = Path.GetExtension(defaultFileName);
            var file = _dialogs.SaveFile("Save As", filter, initialDir, defaultFileName, string.IsNullOrEmpty(ext) ? null : ext);
            if (string.IsNullOrEmpty(file)) return false;
            await File.WriteAllTextAsync(file!, Text, Encoding.UTF8);
            CurrentPath = file;
            Saved?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void NewC(TextEditor editor)
        {
            Text = C_FileBuilder.BuildCSourceTemplate("untitled.c");
            editor.Text = Text;
            _hl.EnsureCustomCRegistered(GetRootPath());
            editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");
            CurrentPath = string.Empty;
        }

        public void NewHeader(TextEditor editor)
        {
            Text = C_FileBuilder.BuildHeaderTemplate("untitled.h");
            editor.Text = Text;
            _hl.EnsureCustomCRegistered(GetRootPath());
            editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");
            CurrentPath = string.Empty;
        }

        public void Close(TextEditor editor)
        {
            Text = string.Empty;
            editor.Text = string.Empty;
            editor.SyntaxHighlighting = null;
            CurrentPath = string.Empty;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private string GetRootPath()
        {
            // Derive from project path setting; fallback to process dir
            var p = _settings.ProjectPath;
            if (!string.IsNullOrEmpty(p))
            {
                var dir = Directory.GetParent(p)?.FullName ?? p;
                return dir;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
