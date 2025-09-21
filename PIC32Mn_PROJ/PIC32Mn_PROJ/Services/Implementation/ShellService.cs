using PIC32Mn_PROJ.Services.Abstractions;
using System.Diagnostics;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class ShellService : IShellService
    {
        private Process? _ps;
        public event Action<string>? Output;
        public bool IsRunning => _ps != null && !_ps.HasExited;

        public void Start(string workingDirectory)
        {
            if (IsRunning) return;
            var exe = GetAvailablePowerShell();
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "-NoLogo -NoExit",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            _ps = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _ps.OutputDataReceived += (s, e) => { if (e.Data != null) Output?.Invoke(e.Data); };
            _ps.ErrorDataReceived  += (s, e) => { if (e.Data != null) Output?.Invoke(e.Data); };
            _ps.Exited += (s, e) => Output?.Invoke("[process exited]");
            if (_ps.Start())
            {
                _ps.BeginOutputReadLine();
                _ps.BeginErrorReadLine();
                Output?.Invoke($"Started {exe} (cwd: {workingDirectory})");
            }
            else
            {
                Output?.Invoke("Failed to start PowerShell.");
            }
        }

        public void Stop()
        {
            try
            {
                if (_ps != null && !_ps.HasExited)
                {
                    _ps.StandardInput.WriteLine("exit");
                    if (!_ps.WaitForExit(1500)) _ps.Kill(entireProcessTree: true);
                }
            }
            catch { }
            finally { _ps?.Dispose(); _ps = null; }
        }

        public void Send(string command)
        {
            if (!IsRunning) return;
            _ps!.StandardInput.WriteLine(command);
            _ps.StandardInput.Flush();
        }

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
            catch { }
            return winps;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
