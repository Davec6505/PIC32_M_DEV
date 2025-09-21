using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections.Generic;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {
        // Console output helper
        private void AppendConsole(string text)
        {
            if (psOutput == null) return;
            if (psOutput.InvokeRequired)
            {
                psOutput.BeginInvoke(new Action<string>(AppendConsole), text);
                return;
            }

            // Append text without adding extra newline (raw stream)
            psOutput.SelectionStart = psOutput.TextLength;
            psOutput.SelectionLength = 0;
            psOutput.SelectedText = text;
            psOutput.SelectionStart = psOutput.TextLength;
            psOutput.ScrollToCaret();
        }

        private void AppendConsoleLine(string line)
        {
            AppendConsole(line + Environment.NewLine);
        }

        // Strip ANSI escape sequences (SGR and cursor controls) as a fallback
        private static readonly Regex _ansiRegex = new Regex("\u001B\\[[0-9;?]*[A-Za-z]", RegexOptions.Compiled);
        private static string StripAnsi(string input) => string.IsNullOrEmpty(input) ? input : _ansiRegex.Replace(input, string.Empty);

        // Filter out default PowerShell prompt lines like: "PS C:\\path>" OR strip them when prefixed/embedded
        private static readonly Regex _psPromptRegex = new Regex("^PS [^>]*>\\s*$", RegexOptions.Compiled);
        private static readonly Regex _psPromptPrefixRegex = new Regex("^PS(?: [^>\r\n]*)?>\\s*", RegexOptions.Compiled);
        private static readonly Regex _psPromptAnywhereRegex = new Regex("(?:(?<=^)|(?<=\t)|(?<=\r)|(?<=\n)|(?<=\\s))PS(?: [^>\r\n]*)?>\\s*", RegexOptions.Compiled);

        // Sentinel used to know when to render our own prompt after a command finishes
        private const string PromptMarker = "__PROMPT_READY__";

        // Commands we inject that we want to hide if they echo back
        private const string InitCmd_OutputPlain = "$env:TERM=''; try { $PSStyle.OutputRendering = 'PlainText' } catch {}";
        private const string InitCmd_NoPrompt = "try { function prompt { '' } } catch {}";
        private const string InitCmd_NoProgress = "try { $ProgressPreference = 'SilentlyContinue' } catch {}";
        private const string SetLocationPrefix = "Set-Location -LiteralPath '";

        // Shell start and IO
        private string _shellCwd = string.Empty;
        private bool _shellAnsiDisabledSent = false;
        private bool _promptReady = false;

        private void EnsureShellStarted()
        {
            if (!_shellOutputSubscribed)
            {
                _shellSvc.Output += OnShellOutput;
                _shellOutputSubscribed = true;
            }
            if (string.IsNullOrEmpty(_shellCwd))
                _shellCwd = GetInitialWorkingDirectory();
            if (!_shellSvc.IsRunning)
            {
                _shellSvc.Start(_shellCwd);
                _shellAnsiDisabledSent = false; // reset flag on (re)start
                _promptReady = false; // wait for marker
            }

            // Send rendering init once to suppress ANSI in pwsh and external tools inheriting TERM
            if (!_shellAnsiDisabledSent)
            {
                try
                {
                    _shellSvc.Send(InitCmd_OutputPlain);
                    // Suppress interactive PowerShell prompt output; keep it silent, we render our own prompt
                    _shellSvc.Send(InitCmd_NoPrompt);
                    // Avoid progress bars writing control sequences
                    _shellSvc.Send(InitCmd_NoProgress);
                    // Ask for a prompt marker once init is done
                    _shellSvc.Send($"Write-Host {PromptMarker}");
                }
                catch { }
                _shellAnsiDisabledSent = true;
            }
        }

        private string GetInitialWorkingDirectory()
        {
            try
            {
                var node = treeView_Project?.SelectedNode;
                switch (node?.Tag)
                {
                    case DirectoryInfo di when di.Exists:
                        return di.FullName;
                    case FileInfo fi when File.Exists(fi.FullName):
                        return Path.GetDirectoryName(fi.FullName)!;
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(projectDirPath) && Directory.Exists(projectDirPath))
                return projectDirPath;
            return Environment.CurrentDirectory;
        }

        // Console emulation state
        private int _inputStart = 0; // caret index where current input begins
        private string Prompt => $"{_shellCwd}> ";

        // History state
        private readonly List<string> _history = new();
        private int _historyIndex = -1; // -1 means editing a new (empty) line

        private void Console_ShowPromptIfNeeded()
        {
            if (!_promptReady) return;
            if (psOutput == null || psOutput.IsDisposed) return;

            if (psOutput.InvokeRequired)
            {
                try { psOutput.BeginInvoke((Action)Console_ShowPromptIfNeeded); } catch { }
                return;
            }

            if (psOutput.TextLength == 0 || psOutput.Text.EndsWith("\n") || psOutput.Text.EndsWith("\r"))
            {
                // Write prompt at caret on UI thread
                psOutput.SelectionStart = psOutput.TextLength;
                psOutput.SelectionLength = 0;
                psOutput.SelectedText = Prompt;
                _inputStart = psOutput.TextLength;
                psOutput.ScrollToCaret();
            }
        }

        private void SetCurrentInput(string text)
        {
            if (psOutput == null) return;
            if (psOutput.InvokeRequired)
            {
                psOutput.BeginInvoke(new Action<string>(SetCurrentInput), text);
                return;
            }
            // Replace text from _inputStart to end with provided text
            psOutput.SelectionStart = _inputStart;
            psOutput.SelectionLength = psOutput.TextLength - _inputStart;
            psOutput.SelectedText = text;
            psOutput.SelectionStart = psOutput.TextLength; // move caret to end
        }

        private void Console_MouseDown(object? sender, MouseEventArgs e)
        {
            // Allow selecting any text for copy. Only editing is restricted elsewhere.
            psOutput?.Focus();
        }

        private void Console_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (psOutput == null) return;
            // Let control keys pass to KeyDown logic
            if (char.IsControl(e.KeyChar)) return;

            // Ensure edits don't modify output before the prompt
            if (psOutput.SelectionStart < _inputStart)
            {
                psOutput.SelectionStart = psOutput.TextLength;
                psOutput.SelectionLength = 0;
            }
            else if (psOutput.SelectionStart + psOutput.SelectionLength < _inputStart)
            {
                // Entire selection is before prompt: move to end
                psOutput.SelectionStart = psOutput.TextLength;
                psOutput.SelectionLength = 0;
            }
            else if (psOutput.SelectionStart < _inputStart)
            {
                // If selection crosses the prompt boundary, clamp to start at prompt
                int end = psOutput.SelectionStart + psOutput.SelectionLength;
                psOutput.SelectionStart = _inputStart;
                psOutput.SelectionLength = Math.Max(0, end - _inputStart);
            }
            // Allow character to be inserted normally
        }

        private void Console_KeyDown(object? sender, KeyEventArgs e)
        {
            if (psOutput == null) return;

            // Clipboard shortcuts
            if (e.Control && e.KeyCode == Keys.C && psOutput.SelectionLength > 0)
            {
                // Let normal copy happen, don't treat as interrupt
                return;
            }
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.SuppressKeyPress = true;
                Console_PasteFromClipboard();
                return;
            }

            // History navigation
            if (e.KeyCode == Keys.Up)
            {
                if (_history.Count > 0)
                {
                    if (_historyIndex == -1) _historyIndex = _history.Count; // start from after last
                    if (_historyIndex > 0)
                    {
                        _historyIndex--;
                        SetCurrentInput(_history[_historyIndex]);
                    }
                }
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Down)
            {
                if (_historyIndex >= 0)
                {
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        SetCurrentInput(_history[_historyIndex]);
                    }
                    else
                    {
                        _historyIndex = -1; // new blank entry
                        SetCurrentInput(string.Empty);
                    }
                }
                e.SuppressKeyPress = true;
                return;
            }

            // Enforce read-only before _inputStart
            if (psOutput.SelectionStart < _inputStart &&
                e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Up && e.KeyCode != Keys.Down &&
                e.KeyCode != Keys.Home && e.KeyCode != Keys.End)
            {
                psOutput.SelectionStart = psOutput.TextLength;
            }

            if (e.KeyCode == Keys.Home)
            {
                psOutput.SelectionStart = _inputStart;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.Back)
            {
                if (psOutput.SelectionStart <= _inputStart && psOutput.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            }
            else if (e.KeyCode == Keys.Return)
            {
                e.SuppressKeyPress = true;
                var cmd = psOutput.Text.Substring(_inputStart);
                AppendConsole(Environment.NewLine);
                ProcessConsoleCommand(cmd);
                return;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                // Ctrl+C send interrupt (when no selection)
                AppendConsole("^C" + Environment.NewLine);
                Console_ShowPromptIfNeeded();
                e.SuppressKeyPress = true;
                return;
            }
        }

        private void Console_CopySelection()
        {
            if (psOutput == null || psOutput.SelectionLength == 0) return;
            try { System.Windows.Forms.Clipboard.SetText(psOutput.SelectedText); } catch { }
        }

        private void Console_PasteFromClipboard()
        {
            if (psOutput == null) return;
            if (!System.Windows.Forms.Clipboard.ContainsText()) return;
            var text = System.Windows.Forms.Clipboard.GetText();
            // Paste only within current input region
            if (psOutput.SelectionStart < _inputStart)
                psOutput.SelectionStart = psOutput.TextLength;
            psOutput.SelectedText = text;
            psOutput.SelectionStart = psOutput.TextLength;
        }

        private void AddToHistory(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            // Avoid adding consecutive duplicates
            if (_history.Count == 0 || !string.Equals(_history[^1], command, StringComparison.Ordinal))
            {
                _history.Add(command);
            }
            _historyIndex = -1;
        }

        private static bool IsClearCommand(string input)
        {
            // Support cls, clear, clear-host and Clear-Host variants
            var t = input.Trim();
            if (t.Length == 0) return false;
            return string.Equals(t, "cls", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "clear", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "clear-host", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "clearhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "clear-host()", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "clear-host ()", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "clear-host;", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "Clear-Host", StringComparison.OrdinalIgnoreCase);
        }

        private void ProcessConsoleCommand(string text)
        {
            var trimmed = text.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                Console_ShowPromptIfNeeded();
                return;
            }

            // Record in history
            AddToHistory(trimmed);

            EnsureShellStarted();

            // Built-in clear handling (avoid RawUI errors in non-console host)
            if (IsClearCommand(trimmed))
            {
                if (psOutput != null)
                {
                    psOutput.Clear();
                    _inputStart = 0;
                }
                Console_ShowPromptIfNeeded();
                return;
            }

            // Handle built-in cd/chdir
            if (IsCdCommand(trimmed, out var cdArg))
            {
                if (string.IsNullOrWhiteSpace(cdArg))
                {
                    // Just print current directory
                    AppendConsoleLine(_shellCwd);
                    Console_ShowPromptIfNeeded();
                    return;
                }

                var target = ResolvePathArg(_shellCwd, cdArg);
                if (Directory.Exists(target))
                {
                    _shellCwd = target;
                    // Update shell process location
                    _shellSvc.Send($"Set-Location -LiteralPath '{EscapePsSingleQuote(_shellCwd)}'");
                    Console_ShowPromptIfNeeded();
                }
                else
                {
                    AppendConsoleLine("The system cannot find the path specified.");
                    Console_ShowPromptIfNeeded();
                }
                return;
            }

            try
            {
                // Run arbitrary command in current shell cwd
                _shellSvc.Send($"Set-Location -LiteralPath '{EscapePsSingleQuote(_shellCwd)}'");

                if (string.Equals(trimmed, "make build_dir", StringComparison.OrdinalIgnoreCase))
                {
                    pendingRefreshNode = GetSelectedDirectoryNode();
                    pendingMakeRefresh = true;
                    _shellSvc.Send("make build_dir; Write-Host __MAKE_DONE__");
                }
                else
                {
                    _shellSvc.Send(trimmed);
                }

                // After any command, request a prompt marker so we know when to render our own prompt
                _shellSvc.Send($"Write-Host {PromptMarker}");
            }
            catch (Exception ex)
            {
                AppendConsoleLine($"[error] {ex.Message}");
            }
        }

        private static bool IsCdCommand(string input, out string arg)
        {
            var m = Regex.Match(input, "^(?:cd|chdir)\\s*(.*)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                arg = m.Groups[1].Value.Trim();
                // Strip surrounding quotes if present
                if ((arg.StartsWith("\"") && arg.EndsWith("\"")) || (arg.StartsWith("'") && arg.EndsWith("'")))
                    arg = arg.Substring(1, arg.Length - 2);
                return true;
            }
            arg = string.Empty;
            return false;
        }

        private static string ResolvePathArg(string baseDir, string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return baseDir;
            var candidate = arg;
            if (!Path.IsPathRooted(candidate))
            {
                try { candidate = Path.GetFullPath(Path.Combine(baseDir, candidate)); }
                catch { candidate = Path.Combine(baseDir, candidate); }
            }
            return candidate;
        }

        private static string EscapePsSingleQuote(string s) => s.Replace("'", "''");

        private void OnShellOutput(string line)
        {
            // Remove ANSI sequences so output renders cleanly in the RichTextBox
            var plain = StripAnsi(line);

            // Filter-out PowerShell's own prompt if it leaks through as a standalone line
            if (_psPromptRegex.IsMatch(plain))
            {
                return;
            }

            // Strip any prompt prefix like "PS C:\\path> " or "PS> " from the beginning or embedded
            var cleaned = _psPromptPrefixRegex.Replace(plain, string.Empty);
            cleaned = _psPromptAnywhereRegex.Replace(cleaned, string.Empty);

            // Also remove our own prompt prefix if it accidentally precedes echoed pwsh output (startup race)
            if (!string.IsNullOrEmpty(_shellCwd) && cleaned.StartsWith(Prompt, StringComparison.Ordinal))
            {
                cleaned = cleaned.Substring(Prompt.Length);
            }

            // Handle our sentinel markers without echoing them
            if (pendingMakeRefresh && cleaned.Contains("__MAKE_DONE__", StringComparison.Ordinal))
            {
                pendingMakeRefresh = false;
                if (pendingRefreshNode != null)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => SafeRefreshNode(pendingRefreshNode)));
                    else
                        SafeRefreshNode(pendingRefreshNode);
                }
                Console_ShowPromptIfNeeded();
                return;
            }
            if (cleaned.Contains(PromptMarker, StringComparison.Ordinal))
            {
                _promptReady = true;
                Console_ShowPromptIfNeeded();
                return;
            }

            // Hide echoes of our init and housekeeping commands
            var trimmed = cleaned.Trim();
            if (trimmed.Length == 0)
            {
                // Do not render blank lines emitted by prompt redraws
                return;
            }
            if (string.Equals(trimmed, InitCmd_OutputPlain, StringComparison.Ordinal) ||
                string.Equals(trimmed, InitCmd_NoPrompt, StringComparison.Ordinal) ||
                string.Equals(trimmed, InitCmd_NoProgress, StringComparison.Ordinal) ||
                trimmed.StartsWith(SetLocationPrefix, StringComparison.Ordinal))
            {
                return;
            }

            AppendConsoleLine(cleaned);
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                _shellSvc.Stop();
                if (_shellOutputSubscribed)
                {
                    _shellSvc.Output -= OnShellOutput;
                    _shellOutputSubscribed = false;
                }
            }
            catch { }
        }

        // Helper from project tree
        private void SafeRefreshNode(TreeNode node)
        {
            var dirNode = node;
            if (dirNode.Tag is FileInfo && dirNode.Parent != null)
                dirNode = dirNode.Parent;

            if (dirNode.Tag is DirectoryInfo di && Directory.Exists(di.FullName))
                RepopulateDirectoryNode(dirNode);
        }

        private TreeNode? GetSelectedDirectoryNode()
        {
            var node = treeView_Project?.SelectedNode;
            if (node == null) return null;
            if (node.Tag is DirectoryInfo) return node;
            if (node.Tag is FileInfo) return node.Parent;
            return null;
        }
    }
}
