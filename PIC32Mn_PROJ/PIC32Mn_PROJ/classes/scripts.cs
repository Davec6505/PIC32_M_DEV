using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Media.Media3D;

namespace PIC32Mn_PROJ.classes
{
    internal class scripts
    {
        internal void launch(string app,string? project = null)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "dependancies", "scripts", app+".ps1");
            var ps1 = new ProcessStartInfo()
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File {path} -Project {project}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process =  Process.Start(ps1))
            {
                if (process != null)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            MessageBox.Show(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            MessageBox.Show(e.Data);
                        }
                    };

                    process.WaitForExit();
                }
                else
                {
                    MessageBox.Show($"Can't open a process to start application {app}");
                }
            }
        }

        internal void launchMcc(string project)
        {
            var ps1 = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File {AppContext.BaseDirectory}\\dependancies\\scripts\\startMcc.ps1 -Project {project}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(ps1))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
              //  process.WaitForExit();
                MessageBox.Show(string.IsNullOrWhiteSpace(output) ?
                    "MCC loaded sucessfully." : error, "MCC",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            }

        }

        internal void launchMplabX(string project)
        {
            var ps1 = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File {AppContext.BaseDirectory}\\dependancies\\scripts\\startMplabX.ps1 -Project {project}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(ps1))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
               // process.WaitForExit();
                MessageBox.Show(string.IsNullOrWhiteSpace(output) ?
                    "MPLAB X loaded sucessfully." : error, "MPLAB X",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            }
        }

        internal void alert_changes(string project)
        {
            using (var watch = new FileSystemWatcher(project))
            {
                watch.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                watch.Changed += (sender, e) => OnChanged(sender, e, project);
                watch.Created += (sender, e) => OnChanged(sender, e, project);
                watch.Deleted += (sender, e) => OnChanged(sender, e, project);
                watch.Renamed += (sender, e) => OnRenamed(sender, e, project);
                watch.EnableRaisingEvents = true;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e, string project)
        {
            MessageBox.Show($"File {e.ChangeType}: {e.FullPath}", "Project Change Detected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnRenamed(object sender, RenamedEventArgs e, string project)
        {
            MessageBox.Show($"File Renamed: {e.OldFullPath} -> {e.FullPath}", "Project Change Detected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
