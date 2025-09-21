namespace PIC32Mn_PROJ.Services.Abstractions
{
    public interface ISettingsService
    {
        string? ProjectPath { get; set; }
        string? MirrorProjectPath { get; set; }
        void Save();
    }
}
