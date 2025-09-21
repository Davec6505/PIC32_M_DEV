using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

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
            if (psProcess != null && !psProcess.HasExited) return;

            var exe = GetAvailablePowerShell();
            var cwd = GetLeftWorkingDirectory();
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "-NoLogo -NoExit",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = cwd
            };

            psProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
            psProcess.OutputDataReceived += (s, e) => { if (e.Data != null) OnShellOutput(e.Data); };
            psProcess.ErrorDataReceived  += (s, e) => { if (e.Data != null) OnShellOutput(e.Data); };
            psProcess.Exited += (s, e) => AppendConsoleLine("[process exited]");

            if (psProcess.Start())
            {
                psProcess.BeginOutputReadLine();
                psProcess.BeginErrorReadLine();
                AppendConsoleLine($"Started {exe} (cwd: {cwd})");
            }
            else
            {
                AppendConsoleLine("Failed to start PowerShell.");
            }
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
                var psLiteral = cwd.Replace("'", "''");

                AppendConsoleLine($"> {text}");
                psProcess!.StandardInput.WriteLine($"Set-Location -LiteralPath '{psLiteral}'");

                if (string.Equals(trimmed, "make build_dir", StringComparison.OrdinalIgnoreCase))
                {
                    pendingRefreshNode = GetSelectedDirectoryNode();
                    pendingMakeRefresh = true;
                    psProcess.StandardInput.WriteLine("make build_dir; Write-Host __MAKE_DONE__");
                }
                else
                {
                    psProcess.StandardInput.WriteLine(text);
                }

                psProcess.StandardInput.Flush();
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
                if (psProcess != null && !psProcess.HasExited)
                {
                    psProcess.StandardInput.WriteLine("exit");
                    if (!psProcess.WaitForExit(1500))
                        psProcess.Kill(entireProcessTree: true);
                }
                psProcess?.Dispose();
            }
            catch { /* ignore */ }
        }
    }
}
