using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public sealed class HotkeyService : IHotkeyService
    {
        private readonly ISettingsService _settings;

        // In-memory map
        private readonly Dictionary<string, Keys> _map = new();

        // Default bindings (can be extended)
        private static readonly Dictionary<string, Keys> Defaults = new()
        {
            ["Save"] = Keys.Control | Keys.S,
            ["ToggleConsole"] = Keys.Control | Keys.OemQuotes,
            ["OpenGitTab"] = Keys.Control | Keys.G,
            ["CloseGitTab"] = Keys.Control | Keys.Shift | Keys.G,
            ["Stage"] = Keys.Control | Keys.Shift | Keys.S,
            ["Commit"] = Keys.Control | Keys.Enter,
            ["Fetch"] = Keys.Control | Keys.F5,
            ["Pull"] = Keys.Control | Keys.Shift | Keys.P,
            ["Push"] = Keys.Control | Keys.Shift | Keys.U
        };

        public HotkeyService(ISettingsService settings)
        {
            _settings = settings;
            Load();
        }

        public Keys Get(string actionId) => _map.TryGetValue(actionId, out var k) ? k : Keys.None;

        public void Set(string actionId, Keys keys)
        {
            _map[actionId] = keys;
            Save();
        }

        public IReadOnlyDictionary<string, Keys> GetAll() => _map;

        public void ResetDefaults()
        {
            _map.Clear();
            foreach (var kv in Defaults)
                _map[kv.Key] = kv.Value;
            Save();
        }

        private void Load()
        {
            // Persisted as a simple string in settings, e.g. "Save=131137;ToggleConsole=196656;..."
            // If not present, use defaults and persist them.
            var raw = (_settings as SettingsService)?.GetString("Hotkeys");
            if (string.IsNullOrEmpty(raw))
            {
                ResetDefaults();
                return;
            }
            _map.Clear();
            foreach (var pair in raw.Split(';'))
            {
                var parts = pair.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out var val))
                {
                    _map[parts[0]] = (Keys)val;
                }
            }
            // Ensure any new defaults get merged in
            foreach (var kv in Defaults)
                if (!_map.ContainsKey(kv.Key)) _map[kv.Key] = kv.Value;
            Save();
        }

        private void Save()
        {
            var s = string.Join(";", _map.Select(kv => $"{kv.Key}={(int)kv.Value}"));
            (_settings as SettingsService)?.SetString("Hotkeys", s);
            _settings.Save();
        }
    }
}
