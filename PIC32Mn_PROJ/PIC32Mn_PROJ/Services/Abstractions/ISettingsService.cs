namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface ISettingsService
    {
        string? ProjectPath { get; set; }
        string? MirrorProjectPath { get; set; }
        void Save();

        // Additional simple key/value helpers (non-breaking for consumers that ignore them)
        string? GetString(string key);
        void SetString(string key, string value);
    }
}
