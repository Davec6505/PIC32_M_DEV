using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class TabService : ITabService
    {
        private readonly IEditorService _editor;
        private readonly IHighlightingService _hl;
        private readonly IProjectTreeService _tree;

        public TabService(IEditorService editor, IHighlightingService hl, IProjectTreeService tree)
        {
            _editor = editor; _hl = hl; _tree = tree;
        }

        public void OpenFile(TabControl tabControl, string filePath, string? xshdRootPath = null)
        {
            if (!File.Exists(filePath)) return;

            var existing = FindTab(tabControl, filePath);
            if (existing != null)
            {
                tabControl.SelectedTab = existing;
                return;
            }

            var editor = new TextEditor
            {
                ShowLineNumbers = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White)
            };

            // Show a loading placeholder immediately to avoid blocking the UI thread
            editor.Text = "Loading...";

            // Prepare hosting controls
            var host = new ElementHost { Dock = DockStyle.Fill, Child = editor };
            var page = new TabPage(Path.GetFileName(filePath))
            {
                Tag = filePath
            };
            page.Controls.Add(host);
            tabControl.TabPages.Add(page);
            tabControl.SelectedTab = page;

            // Attach per-editor context menu with Save / Close actions
            AttachEditorContextMenu(tabControl, page, editor, xshdRootPath);

            // Load file content asynchronously to avoid UI freeze/deadlock
            _ = LoadIntoEditorAsync(tabControl, editor, filePath, xshdRootPath);
        }

        private async Task LoadIntoEditorAsync(TabControl tabControl, TextEditor editorControl, string filePath, string? xshdRootPath)
        {
            string text;
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var reader = new StreamReader(stream);
                text = await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (tabControl.IsHandleCreated)
                {
                    tabControl.BeginInvoke(() =>
                    {
                        MessageBox.Show($"Failed to open file:\n{ex.Message}", "Open File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        editorControl.Text = string.Empty;
                    });
                }
                return;
            }

            if (tabControl.IsHandleCreated)
            {
                tabControl.BeginInvoke(() =>
                {
                    editorControl.Text = text;

                    if (!string.IsNullOrEmpty(xshdRootPath))
                    {
                        var ext = Path.GetExtension(filePath).ToLowerInvariant();
                        if (ext is ".c" or ".h")
                        {
                            _hl.EnsureCustomCRegistered(xshdRootPath);
                            editorControl.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");
                        }
                        else
                        {
                            var def = _hl.GetForPath(filePath);
                            if (def != null) editorControl.SyntaxHighlighting = def;
                        }
                    }
                });
            }
        }

        private void AttachEditorContextMenu(TabControl tabControl, TabPage page, TextEditor editor, string? xshdRootPath)
        {
            var cm = new System.Windows.Controls.ContextMenu();

            var miCut = new System.Windows.Controls.MenuItem { Header = "Cut" };
            miCut.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Cut;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea)) cmd.Execute(null, editor.TextArea);
            };
            var miCopy = new System.Windows.Controls.MenuItem { Header = "Copy" };
            miCopy.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Copy;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea)) cmd.Execute(null, editor.TextArea);
            };
            var miPaste = new System.Windows.Controls.MenuItem { Header = "Paste" };
            miPaste.Click += (s, e) =>
            {
                var cmd = System.Windows.Input.ApplicationCommands.Paste;
                if (editor.TextArea != null && cmd.CanExecute(null, editor.TextArea)) cmd.Execute(null, editor.TextArea);
            };

            var miSave = new System.Windows.Controls.MenuItem { Header = "Save" };
            miSave.Click += async (s, e) => await SaveEditorAsync(tabControl, page, editor);

            var miSaveAs = new System.Windows.Controls.MenuItem { Header = "Save As" };
            miSaveAs.Click += async (s, e) => await SaveEditorAsAsync(tabControl, page, editor, xshdRootPath);

            var miClose = new System.Windows.Controls.MenuItem { Header = "Close Tab" };
            miClose.Click += (s, e) => CloseActive(tabControl);

            cm.Items.Add(miCut);
            cm.Items.Add(miCopy);
            cm.Items.Add(miPaste);
            cm.Items.Add(new System.Windows.Controls.Separator());
            cm.Items.Add(miSave);
            cm.Items.Add(miSaveAs);
            cm.Items.Add(miClose);
            cm.Opened += (s, e) =>
            {
                bool hasSel = editor?.TextArea?.Selection?.IsEmpty == false;
                miCut.IsEnabled = hasSel && !editor.IsReadOnly;
                miCopy.IsEnabled = hasSel;
                miPaste.IsEnabled = !editor.IsReadOnly && System.Windows.Clipboard.ContainsText();
            };
            editor.ContextMenu = cm;
        }

        private async Task SaveEditorAsync(TabControl tabControl, TabPage page, TextEditor editor)
        {
            var currentPath = page.Tag as string;
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                await SaveEditorAsAsync(tabControl, page, editor, null);
                return;
            }

            try
            {
                await File.WriteAllTextAsync(currentPath!, editor.Text, new UTF8Encoding(false));
                MessageBox.Show($"Saved: {currentPath}", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed:\n{ex.Message}", "Save", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveEditorAsAsync(TabControl tabControl, TabPage page, TextEditor editor, string? xshdRootPath)
        {
            using var sfd = new SaveFileDialog
            {
                Title = "Save As",
                FileName = Path.GetFileName(page.Tag as string ?? "untitled.c"),
                Filter = "All Files|*.*|C Source (*.c)|*.c|Header (*.h)|*.h",
                FilterIndex = 1
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                await File.WriteAllTextAsync(sfd.FileName, editor.Text, new UTF8Encoding(false));

                // If another tab already has this path, switch to it and close current
                var existing = FindTab(tabControl, sfd.FileName);
                if (existing != null && existing != page)
                {
                    // Select existing and close this duplicate
                    tabControl.SelectedTab = existing;
                    tabControl.TabPages.Remove(page);
                    page.Dispose();
                    return;
                }

                // Update current tab to the new path
                page.Tag = sfd.FileName;
                page.Text = Path.GetFileName(sfd.FileName);

                // Update highlighting if needed
                if (!string.IsNullOrEmpty(xshdRootPath))
                {
                    var ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                    if (ext is ".c" or ".h")
                    {
                        _hl.EnsureCustomCRegistered(xshdRootPath);
                        editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C-PIC");
                    }
                    else
                    {
                        var def = _hl.GetForPath(sfd.FileName);
                        if (def != null) editor.SyntaxHighlighting = def;
                        else editor.SyntaxHighlighting = null;
                    }
                }

                MessageBox.Show($"Saved: {sfd.FileName}", "Save As", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save As failed:\n{ex.Message}", "Save As", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CloseActive(TabControl tabControl)
        {
            if (tabControl.SelectedTab is TabPage page)
            {
                tabControl.TabPages.Remove(page);
                page.Dispose();
            }
        }

        public void CloseFile(TabControl tabControl, string filePath)
        {
            var page = FindTab(tabControl, filePath);
            if (page != null)
            {
                tabControl.TabPages.Remove(page);
                page.Dispose();
            }
        }

        private static TabPage? FindTab(TabControl tabControl, string filePath)
        {
            foreach (TabPage p in tabControl.TabPages)
            {
                if (string.Equals(p.Tag as string, filePath, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }
    }
}
