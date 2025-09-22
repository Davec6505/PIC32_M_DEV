using Microsoft.Extensions.DependencyInjection;
using PIC32Mn_PROJ.Services.Abstractions;
using PIC32Mn_PROJ.Services.Implementation;

namespace PIC32Mn_PROJ
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection()
                // Services
                .AddSingleton<ISettingsService, SettingsService>()
                .AddSingleton<IDialogService, DialogService>()
                .AddSingleton<IFileSystemService, FileSystemService>()
                .AddSingleton<IProjectTreeService, ProjectTreeService>()
                .AddSingleton<IHighlightingService, HighlightingService>()
                .AddSingleton<IShellService, ShellService>()
                .AddSingleton<IEditorService, EditorService>()
                .AddSingleton<ITabService, TabService>()
                .AddSingleton<IGitService, GitService>()
                .AddSingleton<IHotkeyService, HotkeyService>()
                // Form
                .AddSingleton<Form1>()
                .BuildServiceProvider();

            var form = services.GetRequiredService<Form1>();
            Application.Run(form);
        }
    }
}