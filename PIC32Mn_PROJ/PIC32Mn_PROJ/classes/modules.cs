using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PIC32Mn_PROJ.classes
{
    internal class Modules : IEnumerable<object>
    {
        public string PIC_PATH { get; set; }
        public string OUT_PATH { get; set; }
        private Dictionary<string, (string Path1, string Path2)> modules_;

        // Add this constructor if not present

        public Modules(string picPath, string outPath)
        {
            PIC_PATH = picPath;
            OUT_PATH = outPath;

            //EXAMPLE OF MODULES
            //"FUSECONFIG"  | "ADCHS" | "CAN" | "CFG" | "CMP" | "CORE" | "CRU" | "RCON" | "DMA" | "DMT" | "ETH" | "GPIO" | "I2C" | "ICAP" |
            //"INT" | "JTAG" | "NVM" | "OCMP" | "PCACHE" | "PMP" | "RNG" | "RPIN" | "RPOUT" | "RTCC" | "SB" |  "SPI" | "SQI" | "TMR1" | "TMR" |
            //"UART" | "USB" | "USBCLKRST" | "WDT"

            modules_ = new Dictionary<string, (string, string)>
            {
                { "FUSECONFIG", (picPath, outPath + "FUSECONFIG.json") },
                { "ADCHS", (picPath, outPath + "ADCHS.json") },
                { "CAN", (picPath, outPath + "CAN.json") },
                { "CFG", (picPath, outPath + "CFG.json") },
                { "CMP", (picPath, outPath + "CMP.json") },
                { "CORE", (picPath, outPath + "CORE.json") },
                { "CRU", (picPath, outPath + "CRU.json") },
                { "RCON", (picPath, outPath + "RCON.json") },
                { "DMA", (picPath, outPath + "DMA.json") },
                { "DMT", (picPath, outPath + "DMT.json") },
                { "ETH", (picPath, outPath + "ETH.json") },
                { "GPIO", (picPath, outPath + "GPIO.json") },
                { "I2C", (picPath, outPath + "I2C.json") },
                { "ICAP", (picPath, outPath + "ICAP.json") },
                { "INT", (picPath, outPath + "INT.json") },
                { "JTAG", (picPath, outPath + "JTAG.json") },
                { "NVM", (picPath, outPath + "NVM.json") },
                { "OCMP", (picPath, outPath + "OCMP.json") },
                { "PCACHE", (picPath, outPath + "PCACHE.json") },
                { "PMP", (picPath, outPath + "PMP.json") },
                { "RNG", (picPath, outPath + "RNG.json") },
                { "RPIN", (picPath, outPath + "RPIN.json") },
                { "RPOUT", (picPath, outPath + "RPOUT.json") },
                { "RTCC", (picPath, outPath + "RTCC.json") },
                { "SB", (picPath, outPath + "SB.json") },
                { "SPI", (picPath, outPath + "SPI.json") },
                { "SQI", (picPath, outPath + "SQI.json") },
                { "TMR1", (picPath, outPath + "TMR1.json") },
                { "TMR", (picPath, outPath + "TMR.json") },
                { "UART", (picPath, outPath + "UART.json") },
                { "USB", (picPath, outPath + "USB.json") },
                { "USBCLKRST", (picPath, outPath + "USBCLKRST.json") },
                { "WDT", (picPath, outPath + "WDT.json") }

            };
       

        }

        // Implement IEnumerable<object>
        public IEnumerator<object> GetEnumerator()
        {
            foreach (var kvp in modules_)
            {
                yield return kvp;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
