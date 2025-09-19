using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PIC32Mn_PROJ.Generation
{
    // Minimal Harmony3 .tfl XML parser -> IntermediateModel
    public static class TflParser
    {
        public static IntermediateModel Parse(string tflPath)
        {
            var model = new IntermediateModel();
            var doc = XDocument.Load(tflPath);
            var root = doc.Root!; // assume valid .tfl

            model.Device = (string?)root.Attribute("device") ?? string.Empty;

            // Peripherals
            foreach (var p in root.Descendants("peripheral"))
            {
                var per = new PeripheralModel
                {
                    Name = (string?)p.Attribute("name") ?? string.Empty,
                    Type = (string?)p.Attribute("type") ?? string.Empty,
                };

                foreach (var s in p.Elements("setting"))
                {
                    var key = (string?)s.Attribute("name") ?? string.Empty;
                    var val = (string?)s.Attribute("value") ?? string.Empty;
                    if (!string.IsNullOrEmpty(key)) per.Settings[key] = val;
                }

                foreach (var pin in p.Elements("pin"))
                {
                    var pc = new PinConfig
                    {
                        Alias = (string?)pin.Attribute("alias") ?? string.Empty,
                        Name = (string?)pin.Attribute("name") ?? string.Empty,
                        Port = (string?)pin.Attribute("port") ?? string.Empty,
                        Function = (string?)pin.Attribute("function") ?? "GPIO",
                        Direction = (string?)pin.Attribute("direction") ?? string.Empty,
                    };
                    int.TryParse((string?)pin.Attribute("number"), out var num);
                    pc.Pin = num;
                    per.Pins.Add(pc);
                }

                model.Peripherals.Add(per);
            }

            // Global Pins
            foreach (var pin in root.Element("pins")?.Elements("pin") ?? Enumerable.Empty<XElement>())
            {
                var pc = new PinConfig
                {
                    Alias = (string?)pin.Attribute("alias") ?? string.Empty,
                    Name = (string?)pin.Attribute("name") ?? string.Empty,
                    Port = (string?)pin.Attribute("port") ?? string.Empty,
                    Function = (string?)pin.Attribute("function") ?? "GPIO",
                    Direction = (string?)pin.Attribute("direction") ?? string.Empty,
                };
                int.TryParse((string?)pin.Attribute("number"), out var num);
                pc.Pin = num;
                model.Pins.Add(pc);
            }

            // Clocks
            foreach (var clk in root.Element("clocks")?.Elements("clock") ?? Enumerable.Empty<XElement>())
            {
                var cc = new ClockConfig
                {
                    Name = (string?)clk.Attribute("name") ?? string.Empty,
                    Source = (string?)clk.Attribute("source") ?? string.Empty,
                    Enabled = bool.TryParse((string?)clk.Attribute("enabled"), out var en) && en,
                };
                if (uint.TryParse((string?)clk.Attribute("frequencyHz"), out var f)) cc.FrequencyHz = f;
                foreach (var s in clk.Elements("setting"))
                {
                    var key = (string?)s.Attribute("name") ?? string.Empty;
                    var val = (string?)s.Attribute("value") ?? string.Empty;
                    if (!string.IsNullOrEmpty(key)) cc.Settings[key] = val;
                }
                model.Clocks.Add(cc);
            }

            // Root settings (optional)
            foreach (var s in root.Element("settings")?.Elements("setting") ?? Enumerable.Empty<XElement>())
            {
                var key = (string?)s.Attribute("name") ?? string.Empty;
                var val = (string?)s.Attribute("value") ?? string.Empty;
                if (!string.IsNullOrEmpty(key)) model.Settings[key] = val;
            }

            return model;
        }
    }
}
