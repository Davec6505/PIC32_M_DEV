using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace PIC32Mn_PROJ
{
    public partial class Form1
    {
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
        private readonly float lblFRC_y = 0.53f;
        private readonly float lblFRCDIV_x = 0.63f;
        private readonly float lblFRCDIV_y = 0.53f;

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


            });
        }


        private void assign_events_clockdiagram()
        {
           numericUpDown_POSC.ValueChanged += (s, e) => { label_POSCO.Text = numericUpDown_POSC.Value.ToString() + " Hz"; };
           comboBox_FRCDIV.SelectedIndexChanged += (s, e) => { label_FRCOSCDIV.Text = calculate_frcdiv(label_FRCOSC.Text, comboBox_FRCDIV.SelectedItem as string ?? "ERR"); };

            update_labels_clockdiagram();
        }



        private void update_labels_clockdiagram()
        {
            label_POSCO.Text = numericUpDown_POSC.Value.ToString() + " Hz";
            label_FRCOSCDIV.Text = calculate_frcdiv(label_FRCOSC.Text, comboBox_FRCDIV.SelectedItem as string ?? "ERR");
        }

        private object CalculateSysClock()
        {
            object poscmod = comboBox_POSCMOD.SelectedItem;
            return poscmod;
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
    }
}










