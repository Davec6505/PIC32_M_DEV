using System;
using System.Diagnostics;
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

        // Limits to avoid UI freezes when opening large or binary files
        private const int BinaryProbeBytes = 8192; // bytes to probe for binary detection
        private const int MaxHexPreviewBytes = 256 * 1024; // 256 KB hex preview cap
        private const int MaxTextPreviewBytes = 2 * 1024 * 1024; // 2 MB text preview cap

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
            _ = LoadIntoEditorAsync(tabControl, page, editor, filePath, xshdRootPath);
        }

        private async Task LoadIntoEditorAsync(TabControl tabControl, TabPage page, TextEditor editorControl, string filePath, string? xshdRootPath)
        {
            // Decide strategy based on extension and content probe
            bool treatAsBinary = false;
            long fileLength = 0;
            try
            {
                var fi = new FileInfo(filePath);
                if (fi.Exists) fileLength = fi.Length;
            }
            catch { }

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (IsBinaryExtension(ext)) treatAsBinary = true;
            else
            {
                try
                {
                    using var fsProbe = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var probeLen = (int)Math.Min(BinaryProbeBytes, fsProbe.Length);
                    var buf = new byte[probeLen];
                    var n = await fsProbe.ReadAsync(buf, 0, probeLen).ConfigureAwait(false);
                    treatAsBinary = LooksBinary(buf, n);
                }
                catch
                {
                    // If probing fails, fall back to text with safety limits
                    treatAsBinary = false;
                }
            }

            if (treatAsBinary)
            {
                // Render a truncated hex preview asynchronously
                string hexText = string.Empty;
                int shown = 0;
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    int toRead = (int)Math.Min(MaxHexPreviewBytes, fs.Length);
                    var buffer = new byte[toRead];
                    shown = await fs.ReadAsync(buffer, 0, toRead).ConfigureAwait(false);
                    hexText = BuildHexPreview(buffer, shown, fiLength: fileLength);
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
                        page.Text = Path.GetFileName(filePath) + " [binary]";
                        editorControl.IsReadOnly = true;
                        editorControl.SyntaxHighlighting = null;
                        editorControl.Text = hexText;
                    });
                }
                return;
            }

            // Treat as text: stream-read with a cap to avoid OOM for giant files
            try
            {
                string text;
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

                if (fileLength > MaxTextPreviewBytes)
                {
                    // Read only first MaxTextPreviewBytes and mark as truncated
                    using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                    var sb = new StringBuilder((int)Math.Min(fileLength, MaxTextPreviewBytes) + 1024);
                    char[] cbuf = new char[8192];
                    long remaining = MaxTextPreviewBytes;
                    while (remaining > 0)
                    {
                        int toRead = (int)Math.Min(cbuf.Length, remaining);
                        int read = await reader.ReadAsync(cbuf, 0, toRead).ConfigureAwait(false);
                        if (read <= 0) break;
                        sb.Append(cbuf, 0, read);
                        remaining -= read;
                    }
                    sb.AppendLine();
                    sb.AppendLine($"\n--- NOTE: Display truncated at {MaxTextPreviewBytes / 1024 / 1024} MB of {fileLength / 1024.0 / 1024.0:F2} MB. ---");
                    text = sb.ToString();
                }
                else
                {
                    using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                    text = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (tabControl.IsHandleCreated)
                {
                    tabControl.BeginInvoke(() =>
                    {
                        editorControl.Text = text;

                        if (!string.IsNullOrEmpty(xshdRootPath))
                        {
                            var extLocal = Path.GetExtension(filePath).ToLowerInvariant();
                            if (extLocal is ".c" or ".h")
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
        }

        private static bool IsBinaryExtension(string ext)
        {
            switch (ext)
            {
                case ".exe":
                case ".dll":
                case ".bin":
                case ".dat":
                case ".obj":
                case ".o":
                case ".lib":
                case ".so":
                case ".pdf":
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".bmp":
                case ".ico":
                case ".zip":
                case ".7z":
                case ".rar":
                case ".gz":
                case ".tar":
                case ".mp3":
                case ".mp4":
                case ".avi":
                case ".mov":
                    return true;
                default:
                    return false;
            }
        }

        private static bool LooksBinary(byte[] buffer, int length)
        {
            if (length == 0) return false;
            int nulls = 0, suspicious = 0;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[i];
                if (b == 0) { nulls++; continue; }
                // count non-text control bytes
                if (b < 7 || (b > 13 && b < 32)) suspicious++;
            }
            // Heuristics: any NUL strongly indicates binary; otherwise high ratio of control bytes
            if (nulls > 0) return true;
            double ratio = (double)suspicious / length;
            return ratio > 0.30; // 30% control chars
        }

        private static string BuildHexPreview(byte[] data, int length, long fiLength)
        {
            var sb = new StringBuilder(Math.Min(length, 1_000_000));
            sb.AppendLine("Binary file preview (read-only). Showing first " + length.ToString("N0") + " bytes of " + fiLength.ToString("N0") + ".");
            sb.AppendLine("Offset(h)  00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F  ASCII");
            int rows = (length + 15) / 16;
            for (int r = 0; r < rows; r++)
            {
                int offset = r * 16;
                sb.Append(offset.ToString("X8"));
                sb.Append("  ");
                int i;
                for (i = 0; i < 16; i++)
                {
                    int idx = offset + i;
                    if (idx < length) sb.Append(data[idx].ToString("X2"));
                    else sb.Append("  ");
                    sb.Append(' ');
                }
                sb.Append(' ');
                for (i = 0; i < 16; i++)
                {
                    int idx = offset + i;
                    if (idx < length)
                    {
                        char c = (data[idx] >= 32 && data[idx] <= 126) ? (char)data[idx] : '.';
                        sb.Append(c);
                    }
                    else sb.Append(' ');
                }
                sb.AppendLine();
            }
            if (fiLength > length)
            {
                sb.AppendLine("...");
                sb.AppendLine("[Preview truncated]");
            }
            sb.AppendLine();
            sb.AppendLine("Right-click for more actions (e.g., Open Externally).");
            return sb.ToString();
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

            var miOpenExternal = new System.Windows.Controls.MenuItem { Header = "Open Externally" };
            miOpenExternal.Click += (s, e) =>
            {
                var path = page.Tag as string;
                if (string.IsNullOrEmpty(path)) return;
                try
                {
                    var psi = new ProcessStartInfo(path!) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open externally:\n{ex.Message}", "Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var miClose = new System.Windows.Controls.MenuItem { Header = "Close Tab" };
            miClose.Click += (s, e) => CloseActive(tabControl);

            cm.Items.Add(miCut);
            cm.Items.Add(miCopy);
            cm.Items.Add(miPaste);
            cm.Items.Add(new System.Windows.Controls.Separator());
            cm.Items.Add(miSave);
            cm.Items.Add(miSaveAs);
            cm.Items.Add(miOpenExternal);
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
