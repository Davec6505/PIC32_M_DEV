using PIC32Mn_PROJ.Services.Abstractions;

namespace PIC32Mn_PROJ.Services.Implementation
{
    public class SettingsService : ISettingsService
    {
        public string? ProjectPath
        {
            get => AppSettings.Default.ProjectPath;
            set => AppSettings.Default.ProjectPath = value ?? string.Empty;
        }

        public string? MirrorProjectPath
        {
            get => AppSettings.Default.mirror_ProjectPath;
            set => AppSettings.Default.mirror_ProjectPath = value ?? string.Empty;
        }

        public void Save() => AppSettings.Default.Save();
    }
}
