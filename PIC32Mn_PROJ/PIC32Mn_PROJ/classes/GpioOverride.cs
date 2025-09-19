using System.Text.Json.Serialization;

namespace PIC32Mn_PROJ.classes
{
    public class GpioOverride
    {
        public string PinKey { get; set; } = "";          // e.g. "PIN1"
        public string PinName { get; set; } = "";         // e.g. "RB0"
        public bool Enabled { get; set; }                  // checkbox state
        public bool Output { get; set; }                   // true if Out selected
        public string? Function { get; set; }              // selected function from combo

        [JsonIgnore]
        public string PortChannel
        {
            get
            {
                // Expect names like RB0, RE10, etc.
                if (!string.IsNullOrEmpty(PinName) && PinName.Length >= 2 && PinName[0] == 'R')
                    return PinName[1].ToString();
                return "";
            }
        }

        [JsonIgnore]
        public int PortPin
        {
            get
            {
                if (!string.IsNullOrEmpty(PinName) && PinName.Length >= 3)
                {
                    var digits = PinName.Substring(2);
                    if (int.TryParse(digits, out var n)) return n;
                }
                return -1;
            }
        }
    }
}
