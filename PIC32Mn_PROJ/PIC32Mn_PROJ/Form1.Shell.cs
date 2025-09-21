using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ
{
    public partial class Form1 : Form
    {
        // Console output helper
        private void AppendConsoleLine(string line)
        {
            if (psOutput == null) return;

            if (psOutput.InvokeRequired)
            {
                psOutput.BeginInvoke(new Action<string>(AppendConsoleLine), line);
                return;
            }

            psOutput.AppendText(line + Environment.NewLine);
            psOutput.SelectionStart = psOutput.TextLength;
            psOutput.ScrollToCaret();
        }

        // Start a PowerShell process with left-tree working directory
        private void EnsureShellStarted()
        {
            if (!_shellOutputSubscribed)
            {
                _shellSvc.Output += AppendConsoleLine;
                _shellOutputSubscribed = true;
            }
            if (_shellSvc.IsRunning) return;
            var cwd = GetLeftWorkingDirectory();
            _shellSvc.Start(cwd);
        }

        // Resolve working directory from left tree selection or project root
        private string GetLeftWorkingDirectory()
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
            catch { /* ignore */ }

            if (!string.IsNullOrEmpty(projectDirPath) && Directory.Exists(projectDirPath))
                return projectDirPath;

            return Environment.CurrentDirectory;
        }

        // Prefer pwsh, then Windows PowerShell
        private static string GetAvailablePowerShell()
        {
            var pwsh = "pwsh.exe";
            var winps = "powershell.exe";
            try
            {
                using var probe = Process.Start(new ProcessStartInfo
                {
                    FileName = pwsh,
                    Arguments = "-v",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });
                if (probe != null) return pwsh;
            }
            catch { /* ignore */ }
            return winps;
        }

        private void OnShellOutput(string line)
        {
            AppendConsoleLine(line);
            if (pendingMakeRefresh && line.Contains("__MAKE_DONE__", StringComparison.Ordinal))
            {
                pendingMakeRefresh = false;
                if (pendingRefreshNode != null)
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => SafeRefreshNode(pendingRefreshNode)));
                    else
                        SafeRefreshNode(pendingRefreshNode);
                }
            }
        }

        // Ensure the node is a directory node and refresh it
        private void SafeRefreshNode(TreeNode node)
        {
            var dirNode = node;
            if (dirNode.Tag is FileInfo && dirNode.Parent != null)
                dirNode = dirNode.Parent;

            if (dirNode.Tag is DirectoryInfo di && Directory.Exists(di.FullName))
                RepopulateDirectoryNode(dirNode);
        }

        // Get current directory node from left tree selection
        private TreeNode? GetSelectedDirectoryNode()
        {
            var node = treeView_Project?.SelectedNode;
            if (node == null) return null;
            if (node.Tag is DirectoryInfo) return node;
            if (node.Tag is FileInfo) return node.Parent;
            return null;
        }

        // Send command to the shell, handling sentinel for make
        private void SendShellCommand(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            EnsureShellStarted();

            try
            {
                var trimmed = text.Trim();
                var cwd = GetLeftWorkingDirectory();
                AppendConsoleLine($"> {text}");
                _shellSvc.Send($"Set-Location -LiteralPath '{cwd.Replace("'", "''")}'");

                if (string.Equals(trimmed, "make build_dir", StringComparison.OrdinalIgnoreCase))
                {
                    pendingRefreshNode = GetSelectedDirectoryNode();
                    pendingMakeRefresh = true;
                    _shellSvc.Send("make build_dir; Write-Host __MAKE_DONE__");
                }
                else
                {
                    _shellSvc.Send(text);
                }

                psInput.Clear();
            }
            catch (Exception ex)
            {
                AppendConsoleLine($"[error] {ex.Message}");
            }
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
        {
            try
            {
                _shellSvc.Stop();
                if (_shellOutputSubscribed)
                {
                    _shellSvc.Output -= AppendConsoleLine;
                    _shellOutputSubscribed = false;
                }
            }
            catch { /* ignore */ }
        }
    }
}
