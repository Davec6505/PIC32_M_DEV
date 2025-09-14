# PIC32_M_DEV

A Windows application for configuring, visualizing, and generating code for Microchip PIC32 microcontrollers. This tool provides a graphical interface for device configuration, clock setup, pin mapping, and project management, streamlining embedded development workflows.

## Features

- **Device Configuration:**  
  Easily select and configure PIC32 device settings using a user-friendly interface.
- **Clock System Visualization:**  
  Interactive clock diagram with real-time updates and configuration options.
- **Pin Mapping:**  
  Visual pin configuration and function assignment for supported PIC32 devices.
- **Project Management:**  
  Create, open, and manage multiple projects with persistent settings.
- **Code Generation:**  
  Generates configuration files and code templates (e.g., `config_bits.c.tt`, `plib_clk.c.tt`) based on your selections.
- **JSON-Based Configuration:**  
  Uses JSON files for module and device configuration, enabling extensibility and easy updates.
- **Integration with Microchip Harmony:**  
  Designed to work with Harmony-generated files and device packs.

## Getting Started

### Prerequisites

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Visual Studio 2022 or later (recommended for development)
- Microchip Harmony device packs (for device XML/ATDF files)

### Building the Project

1. Clone the repository:
    ```sh
    https://github.com/Davec6505/PIC32_M_DEV.git
    ```
2. Open `PIC32Mn_PROJ.sln` in Visual Studio.
3. Restore NuGet packages if prompted.
4. Build the solution.

### Running the Application

- Run the project from Visual Studio (`F5` or `Ctrl+F5`).
- On first launch, set up your project directory and select your target device.

## Directory Structure

```
PIC32Mn_PROJ/
??? dependancies/
?   ??? modules/         # JSON configuration modules (e.g., CRU.json)
?   ??? templates/       # Code generation templates (.tt, .ftl)
?   ??? gpio/            # Pin mapping data
?   ??? ...              # Other dependencies
??? PIC32Mn_PROJ/        # Main WinForms project source
?   ??? Form1.cs         # Main form logic
?   ??? classes/         # Supporting classes
?   ??? ...
??? README.md
??? ...
```

## Usage

- **Device Selection:**  
  Use the menu to select or create a project and choose your PIC32 device.
- **Configuration:**  
  Adjust clock, fuse, and pin settings using the provided tabs and diagrams.
- **Saving/Loading:**  
  Project settings are saved in JSON files for easy reloading and sharing.
- **Code Generation:**  
  Generated code and configuration files are output to your project directory.

## Contributing

Contributions are welcome! Please open issues or submit pull requests for bug fixes, enhancements, or new features.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- [Microchip Technology](https://www.microchip.com/) for device documentation and Harmony framework.
- [ICSharpCode.AvalonEdit](https://github.com/icsharpcode/AvalonEdit) for code editor integration.
