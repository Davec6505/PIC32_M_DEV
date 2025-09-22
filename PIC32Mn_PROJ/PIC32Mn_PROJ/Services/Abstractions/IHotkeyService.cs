using System.Collections.Generic;
using System.Windows.Forms;

namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface IHotkeyService
    {
        // Returns configured hotkey or Keys.None if not set
        Keys Get(string actionId);
        // Set and persist a hotkey
        void Set(string actionId, Keys keys);
        // Read-only snapshot of all bindings
        IReadOnlyDictionary<string, Keys> GetAll();
        // Reset to defaults and persist
        void ResetDefaults();
    }
}
