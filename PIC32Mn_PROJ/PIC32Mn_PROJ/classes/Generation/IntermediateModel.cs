using System.Collections.Generic;

namespace PIC32Mn_PROJ.Generation
{
    // Core intermediate model for Harmony3 .tfl parsing and code gen
    public class IntermediateModel
    {
        public string Device { get; set; } = string.Empty;
        public List<PeripheralModel> Peripherals { get; set; } = new();
        public List<PinConfig> Pins { get; set; } = new();
        public List<ClockConfig> Clocks { get; set; } = new();
        public Dictionary<string, string> Settings { get; set; } = new();
    }

    public class PeripheralModel
    {
        public string Name { get; set; } = string.Empty;   // e.g., GPIO, I2C1, SPI2
        public string Type { get; set; } = string.Empty;   // family/type if provided in .tfl
        public Dictionary<string, string> Settings { get; set; } = new();
        public List<PinConfig> Pins { get; set; } = new();
    }

    public class PinConfig
    {
        public string Alias { get; set; } = string.Empty;  // optional UI alias
        public string Name { get; set; } = string.Empty;   // e.g., RE5
        public string Port { get; set; } = string.Empty;   // E
        public int Pin { get; set; }
        public string Direction { get; set; } = string.Empty; // in/out
        public string Function { get; set; } = string.Empty;  // GPIO/UART_RX/etc.
    }

    public class ClockConfig
    {
        public string Name { get; set; } = string.Empty;     // e.g., SYSCLK
        public string Source { get; set; } = string.Empty;   // e.g., FRC
        public uint FrequencyHz { get; set; }
        public bool Enabled { get; set; }
        public Dictionary<string, string> Settings { get; set; } = new();
    }

    public class GeneratedFile
    {
        public string RelativePath { get; set; } = string.Empty; // e.g., srcs/plib_gpio.c
        public string Content { get; set; } = string.Empty;
    }
}
