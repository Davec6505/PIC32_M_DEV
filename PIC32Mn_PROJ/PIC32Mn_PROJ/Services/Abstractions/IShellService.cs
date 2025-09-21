using System;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IShellService : IDisposable
    {
        void Start(string workingDirectory);
        void Stop();
        void Send(string command);
        event Action<string>? Output;
        bool IsRunning { get; }
    }
}
