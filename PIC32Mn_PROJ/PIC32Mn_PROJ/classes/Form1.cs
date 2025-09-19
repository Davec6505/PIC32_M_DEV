/*
 * 	Common conversion fixes when porting .ftl to .tt
 * 	=======================================================================
 *  If your .c.ftl has dynamic parts, here’s how to translate them:
 *  •	FreeMarker variable: ${NAME} -> T4: <#= NAME #>
 *  •	FreeMarker if:
 *  •	<#if cond> ... </#if> -> T4: <# if (cond) { #> ... <# } #>
 *  •	FreeMarker loops:
 *  •	<#list items as it> ... </#list> -> T4: <# foreach (var it in items) { #> ... <# } #>
 *  •	Literal “<#” or “#>” in output: escape with control blocks or split string; avoid raw “<#” in output because T4 treats it as a directive.
 * 4.	Why these two .tt files work immediately
 *  •	They consume the same Session["config"] object you already pass for config_bits.
 *  •	plib_clk.c.tt reads PREFEN, PFMWS, ECCCON from JSON so the code matches ProjectSettings.json.
 *  •	Paths align with your generator and tree refresh. After clicking Generate, both files appear under srcs and incs.
 * */


using ICSharpCode.AvalonEdit;
using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;
using PIC32Mn_PROJ.classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;


namespace PIC32Mn_PROJ
{
    public partial class Form1
    {
        #region Form properties an fields
        private ToolTip toolTip1 = new ToolTip();

        // calculation properties for PLL and clock diagram
        private string pllclock = String.Empty;
        private Int32 sysclk = 0;
        private Int32 indiv = 0;
        private Int32 mult = 0;
        private Int32 outdiv = 0;


        // positions in percent of the panel size
        private readonly float poscmod_x = 0.385f;
        private readonly float poscmod_y = 0.385f;
        private readonly float pllclk_x = 0.14f;
        private readonly float pllclk_y = 0.272f;
        private readonly float fpllidiv_x = 0.21f;
        private readonly float fpllidiv_y = 0.247f;
        private readonly float upllfsel_x = 0.221f;
        private readonly float upllfsel_y = 0.0975f;
        private readonly float fpllmult_x = 0.295f;
        private readonly float fpllmult_y = 0.272f;
        private readonly float fpllrng_x = 0.385f;
        private readonly float fpllrng_y = 0.255f;
        private readonly float fpllodiv_x = 0.47f;
        private readonly float fpllodiv_y = 0.22f;
        private readonly float frcdiv_x = 0.53f;
        private readonly float frcdiv_y = 0.58f;
        private readonly float fsoscen_x = 0.248f;
        private readonly float fsoscen_y = 0.788f;
        private readonly float fnosc_x = 0.65f;
        private readonly float fnosc_y = 0.792f;
        private readonly float fcksm_x = 0.90f;
        private readonly float fcksm_y = 0.847f;
        private readonly float foscio_x = 0.168f;
        private readonly float foscio_y = 0.465f;
        private readonly float lbl0scio_x = 0.122f;
        private readonly float lbl0scio_y = 0.465f;
        private readonly float poscclk_x = 0.51f;
        private readonly float poscclk_y = 0.362f;
        private readonly float soscclk_x = 0.42f;
        private readonly float soscclk_y = 0.745f;
        private readonly float sysclk_x = 0.89f;
        private readonly float sysclk_y = 0.55f;
        private readonly float clkOE_x = 0.901f;
        private readonly float clkOE_y = 0.15f;
        private readonly float clkOEON_x = 0.726f;
        private readonly float clkOEON_y = 0.182f;
        private readonly float lblPOSCO_x = 0.63f;
        private readonly float lblPOSCO_y = 0.382f;
        private readonly float lblFRC_x = 0.3f;
        private readonly float lblFRC_y = 0.532f;
        private readonly float lblFRCDIV_x = 0.63f;
        private readonly float lblFRCDIV_y = 0.532f;
        private readonly float lblBFRC_x = 0.63f;
        private readonly float lblBFRC_y = 0.618f;
        private readonly float lblLPRC_x = 0.6235f;
        private readonly float lblLPRC_y = 0.672f;
        private readonly float lblSOSC_x = 0.6135f;
        private readonly float lblSOSC_y = 0.735f;
        private readonly float lblSPLL_x = 0.621f;
        private readonly float lblSPLL_y = 0.290f;

        #endregion


        /// <summary>
        /// Handles the <see cref="Control.Resize"/> event for the <c>panel_ClockDiagram</c> control. Dynamically
        /// adjusts the positions of various UI elements within the panel based on its current size.
        /// </summary>
        /// <remarks>This method recalculates the positions of multiple controls, such as combo boxes,
        /// labels, and checkboxes, to ensure they remain centered at specific relative positions within the
        /// <c>panel_ClockDiagram</c>. The positions are determined using predefined relative coordinates and the
        /// current dimensions of the panel.</remarks>
        /// <param name="sender">The source of the event, typically the <c>panel_ClockDiagram</c> control.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void Panel_ClockDiagram_Resize(object? sender, EventArgs e)
        {
            panel_ClockDiagram.BeginInvoke((MethodInvoker)delegate
            {
                int x = (int)(panel_ClockDiagram.Width * poscmod_x) - (comboBox_POSCMOD.Width / 2);
                int y = (int)(panel_ClockDiagram.Height * poscmod_y) - (comboBox_POSCMOD.Height / 2);
                comboBox_POSCMOD.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * pllclk_x) - (comboBox_FPLLICLK.Width / 2);
                y = (int)(panel_ClockDiagram.Height * pllclk_y) - (comboBox_FPLLICLK.Height / 2);
                comboBox_FPLLICLK.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fpllidiv_x) - (comboBox_FPLLIDIV.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fpllidiv_y) - (comboBox_FPLLIDIV.Height / 2);
                comboBox_FPLLIDIV.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * upllfsel_x) - (comboBox_FPLLIDIV.Width / 2);
                y = (int)(panel_ClockDiagram.Height * upllfsel_y) - (comboBox_UPLLFSEL.Height / 2);
                comboBox_UPLLFSEL.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fpllmult_x) - (comboBox_FPLLMULT.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fpllmult_y) - (comboBox_FPLLMULT.Height / 2);
                comboBox_FPLLMULT.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fpllrng_x) - (comboBox_FPLLRNG.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fpllrng_y) - (comboBox_FPLLRNG.Height / 2);
                comboBox_FPLLRNG.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fpllodiv_x) - (comboBox_FPLLODIV.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fpllodiv_y) - (comboBox_FPLLODIV.Height / 2);
                comboBox_FPLLODIV.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * frcdiv_x) - (comboBox_FRCDIV.Width / 2);
                y = (int)(panel_ClockDiagram.Height * frcdiv_y) - (comboBox_FRCDIV.Height / 2);
                comboBox_FRCDIV.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fsoscen_x) - (comboBox_FSOSCEN.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fsoscen_y) - (comboBox_FSOSCEN.Height / 2);
                comboBox_FSOSCEN.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fnosc_x) - (comboBox_FNOSC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fnosc_y) - (comboBox_FNOSC.Height / 2);
                comboBox_FNOSC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * fcksm_x) - (comboBox_FCKSM.Width / 2);
                y = (int)(panel_ClockDiagram.Height * fcksm_y) - (comboBox_FCKSM.Height / 2);
                comboBox_FCKSM.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * foscio_x) - (comboBox_OSCIOFNC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * foscio_y) - (comboBox_OSCIOFNC.Height / 2);
                comboBox_OSCIOFNC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lbl0scio_x) - (label_OSCIO.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lbl0scio_y) - (label_OSCIO.Height / 2);
                label_OSCIO.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * poscclk_x) - (numericUpDown_POSC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * poscclk_y) - (numericUpDown_POSC.Height / 2);
                numericUpDown_POSC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * soscclk_x) - (numericUpDown_SOSC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * soscclk_y) - (numericUpDown_SOSC.Height / 2);
                numericUpDown_SOSC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * sysclk_x) - (label_SySClock.Width / 2);
                y = (int)(panel_ClockDiagram.Height * sysclk_y) - (label_SySClock.Height / 2);
                label_SySClock.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * clkOE_x) - (checkBox_OE.Width / 2);
                y = (int)(panel_ClockDiagram.Height * clkOE_y) - (checkBox_OE.Height / 2);
                checkBox_OE.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * clkOEON_x) - (checkBox_OutOscON.Width / 2);
                y = (int)(panel_ClockDiagram.Height * clkOEON_y) - (checkBox_OutOscON.Height / 2);
                checkBox_OutOscON.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblPOSCO_x) - (label_POSCO.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblPOSCO_y) - (label_POSCO.Height / 2);
                label_POSCO.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblFRC_x) - (label_FRCOSC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblFRC_y) - (label_FRCOSC.Height / 2);
                label_FRCOSC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblFRCDIV_x) - (label_FRCOSCDIV.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblFRCDIV_y) - (label_FRCOSCDIV.Height / 2);
                label_FRCOSCDIV.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblBFRC_x) - (label_BFRC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblBFRC_y) - (label_BFRC.Height / 2);
                label_BFRC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblLPRC_x) - (label_LPRC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblLPRC_y) - (label_LPRC.Height / 2);
                label_LPRC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblSOSC_x) - (label_SOSC.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblSOSC_y) - (label_SOSC.Height / 2);
                label_SOSC.Location = new Point(x, y);
                x = (int)(panel_ClockDiagram.Width * lblSPLL_x) - (label_SPLL.Width / 2);
                y = (int)(panel_ClockDiagram.Height * lblSPLL_y) - (label_SPLL.Height / 2);
                label_SPLL.Location = new Point(x, y);

            });
        }

        /// <summary>
        /// Sets the tooltips for the clock diagram controls
        /// </summary>
        private void tooltips_clockdiagram()
        {
            toolTip1.SetToolTip(comboBox_POSCMOD, "Primary Oscillator Configuration\n" +
                "FRC: Fast RC Oscillator\n" +
                "FRCPLL: Fast RC Oscillator with PLL\n" +
                "PRI: Primary Oscillator (POSC)\n" +
                "PRIPLL: Primary Oscillator with PLL\n" +
                "SOSC: Secondary Oscillator (SOSC)\n" +
                "LPRC: Low Power RC Oscillator\n" +
                "FRCDIV: Fast RC Oscillator divided by 16\n" +
                "FRCDIVPLL: Fast RC Oscillator divided by 16 with PLL");
            toolTip1.SetToolTip(comboBox_FPLLICLK, "PLL Input Clock Source\n" +
                "POSC: Primary Oscillator\n" +
                "FRC: Fast RC Oscillator");
            toolTip1.SetToolTip(comboBox_FPLLIDIV, "PLL Input Divider\n" +
                "Divides the input clock to the PLL by the selected value.");
            toolTip1.SetToolTip(comboBox_UPLLFSEL, "USB PLL Input Frequency Selection\n" +
                "Selects the input frequency range for the USB PLL.");
            toolTip1.SetToolTip(comboBox_FPLLMULT, "PLL Multiplier\n" +
                "Multiplies the divided input clock to the PLL by the selected value.");
            toolTip1.SetToolTip(comboBox_FPLLRNG, "PLL Frequency Range Selection\n" +
                "Selects the frequency range for the PLL output.\n " +
                "Always match  to the actual frequency entering the PLL after FPLLIDIV.\n" +
                "This ensures the PLL’s internal control loop is tuned correctly.");
            toolTip1.SetToolTip(comboBox_FPLLODIV, "PLL Output Divider\n" +
                "Divides the PLL output clock by the selected value.");
            toolTip1.SetToolTip(comboBox_FRCDIV, "FRC Divider\n" +
                "Divides the Fast RC Oscillator frequency by the selected value.");
            toolTip1.SetToolTip(comboBox_FSOSCEN, "Secondary Oscillator Enable\n" +
                "Enables or disables the Secondary Oscillator (SOSC).");
            toolTip1.SetToolTip(comboBox_FNOSC, "Initial Oscillator Selection\n" +
                "Selects the initial oscillator source after a reset.");
            toolTip1.SetToolTip(comboBox_FCKSM, "Clock Switching and Monitor Selection\n" +
                "Enables or disables clock switching and monitor features.");
            toolTip1.SetToolTip(comboBox_OSCIOFNC, "Oscillator I/O Function\n" +
                "Configures the function of the OSCIO pin.");
            toolTip1.SetToolTip(numericUpDown_POSC, "Primary Oscillator Frequency\n" +
                "Sets the frequency of the Primary Oscillator (POSC) in Hertz.");
            toolTip1.SetToolTip(numericUpDown_SOSC, "Secondary Oscillator Frequency\n" +
                "Sets the frequency of the Secondary Oscillator (SOSC) in Hertz.");
            toolTip1.SetToolTip(label_SySClock, "System Clock Frequency\n" +
                "Displays the calculated system clock frequency based on the selected settings.");
            toolTip1.SetToolTip(checkBox_OE, "Clock Output Enable\n" +
                "Enables or disables the clock output on the OSCIO pin.");
            toolTip1.SetToolTip(checkBox_OutOscON, "Oscillator Output Enable\n" +
                "Enables or disables the oscillator output function on the OSCIO pin.");
            toolTip1.SetToolTip(label_POSCO, "Primary Oscillator Output Frequency\n" +
                "Displays the frequency of the Primary Oscillator (POSC).");
            toolTip1.SetToolTip(label_FRCOSC, "Fast RC Oscillator Frequency\n" +
                "Displays the frequency of the Fast RC Oscillator (FRC).");
            toolTip1.SetToolTip(label_FRCOSCDIV, "FRC Divided Frequency\n" +
                "Displays the frequency of the Fast RC Oscillator after division.");
            toolTip1.SetToolTip(label_BFRC, "Backup Fast RC Oscillator Frequency\n" +
                "Displays the frequency of the Backup Fast RC Oscillator (BFRC).");
            toolTip1.SetToolTip(label_LPRC, "Low Power RC Oscillator Frequency\n" +
                "Displays the frequency of the Low Power RC Oscillator (LPRC).");
            toolTip1.SetToolTip(label_SOSC, "Secondary Oscillator Frequency\n" +
                "Displays the frequency of the Secondary Oscillator (SOSC).");
        }

        /// <summary>
        /// Assigns event handlers to the clock diagram controls to update labels dynamically based on user input.
        /// </summary>
        private void assign_events_clockdiagram()
        {
            comboBox_POSCMOD.SelectedIndexChanged += (s, e) => { CalculateSysClock(); };
            numericUpDown_POSC.ValueChanged += (s, e) => { CalculateSysClock(); };
            comboBox_FRCDIV.SelectedIndexChanged += (s, e) => { label_FRCOSCDIV.Text = calculate_frcdiv(label_FRCOSC.Text, comboBox_FRCDIV.SelectedItem as string ?? "ERR"); };
            comboBox_FSOSCEN.SelectedIndexChanged += (s, e) => { update_label_SOSC(); };
            numericUpDown_SOSC.ValueChanged += (s, e) => { update_label_SOSC(); };
            checkBox_OE.CheckedChanged += (s, e) => { /* Future implementation can be added here */ };
            checkBox_OutOscON.CheckedChanged += (s, e) => { /* Future implementation can be added here */ };

            //PLL related events
            comboBox_FPLLICLK.SelectedIndexChanged += (s, e) => { pllclock = comboBox_FPLLICLK.SelectedItem as string ?? String.Empty; };
            comboBox_FPLLIDIV.SelectedIndexChanged += (s, e) => { CalculateSysClock(); };
            comboBox_FPLLODIV.SelectedIndexChanged += (s, e) => { CalculateSysClock(); };
            comboBox_FPLLMULT.SelectedIndexChanged += (s, e) => { CalculateSysClock(); };
            comboBox_FPLLRNG.SelectedIndexChanged += (s, e) => { CalculateSysClock(); };

            update_labels_clockdiagram();
        }


        /// <summary>
        /// Updates the labels in the clock diagram based on current settings.
        /// </summary>
        private void update_labels_clockdiagram()
        {

            label_FRCOSCDIV.Text = calculate_frcdiv(label_FRCOSC.Text, comboBox_FRCDIV.SelectedItem as string ?? "ERR");
            CalculateSysClock();
            update_label_SOSC();
        }

        /// <summary>
        /// Not yet implemented
        /// </summary>
        /// <returns>The selected item from the <see cref="comboBox_POSCMOD"/> as an <see cref="object"/>.  Returns <see
        /// langword="null"/> if no item is selected.</returns>
        private void CalculateSysClock()
        {
            if (comboBox_POSCMOD.SelectedItem == null)
            {
                label_POSCO.Text = numericUpDown_POSC.Value.ToString() + "ERR";
                return;
            }

            if (comboBox_POSCMOD.SelectedItem?.ToString() == "OFF")
            {
                label_POSCO.Text = "0 Hz";
                return;
            }
            else
            {
                label_POSCO.Text = numericUpDown_POSC.Value.ToString() + " Hz";
            }

            sysclk = Convert.ToInt32(numericUpDown_POSC.Value);

            if (!TryParseDiv(comboBox_FPLLIDIV.SelectedItem as string, out indiv) ||
                !TryParseDiv(comboBox_FPLLMULT.SelectedItem as string, out mult) ||
                !TryParseDiv(comboBox_FPLLODIV.SelectedItem as string, out outdiv) ||
                indiv == 0 || outdiv == 0)
            {
                label_SPLL.Text = "N/A";
                label_SPLL.ForeColor = Color.Black;
                return;
            }

            sysclk = (sysclk / indiv) * mult / outdiv;
            label_SPLL.Text = sysclk.ToString("0") + " Hz";
            if (sysclk > 200_000_000)
                label_SPLL.ForeColor = Color.Red;
            else if (sysclk == 200_000_000)
                label_SPLL.ForeColor = Color.Blue;
            else
                label_SPLL.ForeColor = Color.Black;
        }

        private static bool TryParseDiv(string? text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var parts = text.Split('_');
            return parts.Length >= 2 && int.TryParse(parts[1], out value);
        }

        /// <summary>
        /// calculate frcdiv output frequeny based on frc input frequency
        /// </summary>
        /// <param name="text">frc label holds the const value of the internal oscilator</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private string calculate_frcdiv(string text, string Div)
        {
            string result = "ERR";
            string[] divs = null;
            int frc = 0; // default value
            int intfrc = 0;


            if (text.EndsWith("Hz"))
            {
                text = text.Replace(" Hz", "").Trim();
            }

            if (string.IsNullOrEmpty(Div) || Div == "ERR")
            {
                return "ERR";
            }
            else
            {
                if (Div.Contains('_'))
                {
                    divs = Div.Split('_');

                    if (divs == null || divs.Length < 2)
                        return "ERR in arg 2 because divisor not present!";

                    divs[1] = divs[1].Trim();
                    if (int.TryParse(divs[1], out frc))
                    {

                        if (int.TryParse(text, out int freq))
                        {
                            int frcdiv = freq / (frc != 0 ? frc : 1);
                            if (frcdiv > 0)
                            {
                                intfrc = (int)frcdiv;
                            }
                        }
                        else
                        {
                            return "No value in arg1";
                        }
                    }
                }
                else
                {
                    return "No divisor in arg2";
                }
            }

            return intfrc.ToString("0") + " Hz";
        }

        /// <summary>
        /// Is called when FSOSCEN combobox is changed to enable/disable the SOSC frequency numericupdown
        /// </summary>
        private void update_label_SOSC()
        {
            if (comboBox_FSOSCEN.SelectedItem as string == "ON")
            {
                numericUpDown_SOSC.Enabled = true;
                label_SOSC.Text = numericUpDown_SOSC.Value.ToString() + " Hz";
            }
            else
            {
                numericUpDown_SOSC.Enabled = false;
                numericUpDown_SOSC.Value = 32768;
                label_SOSC.Text = "0 Hz";
            }
        }


        private async Task<bool> GenerateFromTemplateAsync(string ttPath, string outPath, IDictionary<string, object> model)
        {
            var host = new TemplateGenerator();
            var session = new TextTemplatingSession();
            session["config"] = model;

            if (host is ITextTemplatingSessionHost sh)
                sh.Session = session;

            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            var ok = await host.ProcessTemplateAsync(ttPath, outPath);
            if (!ok)
            {
                var errs = host.Errors != null ? string.Join(Environment.NewLine, host.Errors.Cast<object>()) : "Unknown T4 error";
                MessageBox.Show($"T4 generation failed for:\n{ttPath}\n\n{errs}", "T4 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return ok;
        }

        private async Task project_generate_fromttfiles()
        {
            // Build model (unchanged)
            ProjectSettingsManager.SettingsFileName_ = "ProjectSettings.json";
            var rawSettings = ProjectSettingsManager.Load(projectDirPath);
            string deviceName = rawSettings.Device ?? ProjectSettingsManager.GetDevice(projectDirPath);

            Dictionary<string, string>? deviceConfig = null;
            if (!string.IsNullOrEmpty(deviceName) && rawSettings.ExtensionData != null && rawSettings.ExtensionData.ContainsKey(deviceName))
            {
                var deviceElement = rawSettings.ExtensionData[deviceName];
                deviceConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                static string ToStringValue(JsonElement el)
                    => el.ValueKind == JsonValueKind.String ? (el.GetString() ?? string.Empty) : (el.GetRawText() ?? string.Empty).Trim();

                foreach (var prop in deviceElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var sub in prop.Value.EnumerateObject())
                            deviceConfig[sub.Name] = ToStringValue(sub.Value);
                    }
                    else
                    {
                        deviceConfig[prop.Name] = ToStringValue(prop.Value);
                    }
                }
            }

            var model = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            model["Device"] = deviceName ?? "PIC32MZ1024EFH144";
            model["Config"] = deviceConfig ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Paths
            var cTtPath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.c.tt");
            var hTtPath = Path.Combine(rootPath, "dependancies", "templates", "config_bits.h.tt");
            var cOutPath = Path.Combine(projectDirPath, "srcs", "config_bits.c");
            var hOutPath = Path.Combine(projectDirPath, "incs", "config_bits.h");


            var clkCTtPath = Path.Combine(rootPath, "dependancies", "templates", "plib_clk.c.tt");
            var clkHTtPath = Path.Combine(rootPath, "dependancies", "templates", "plib_clk.h.tt");
            var clkCOut = Path.Combine(projectDirPath, "srcs", "plib_clk.c");
            var clkHOut = Path.Combine(projectDirPath, "incs", "plib_clk.h");


            var gpioCTtPath = Path.Combine(rootPath, "dependancies", "templates", "plib_gpio.c.tt");
            var gpioHTtPath = Path.Combine(rootPath, "dependancies", "templates", "plib_gpio.h.tt");
            var gpioCOut = Path.Combine(projectDirPath, "srcs", "plib_gpio.c");
            var gpioHOut = Path.Combine(projectDirPath, "incs", "plib_gpio.h");

            // Generate all available templates
            if (File.Exists(clkCTtPath)) await GenerateFromTemplateAsync(clkCTtPath, clkCOut, model);
            if (File.Exists(clkHTtPath)) await GenerateFromTemplateAsync(clkHTtPath, clkHOut, model);
            if (File.Exists(gpioCTtPath)) await GenerateFromTemplateAsync(gpioCTtPath, gpioCOut, model);
            if (File.Exists(gpioHTtPath)) await GenerateFromTemplateAsync(gpioHTtPath, gpioHOut, model);

            // Config bits are required
            if (!File.Exists(cTtPath) || !File.Exists(hTtPath))
                return;

            await GenerateFromTemplateAsync(cTtPath, cOutPath, model);
            await GenerateFromTemplateAsync(hTtPath, hOutPath, model);
        }
    }
}


































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































































