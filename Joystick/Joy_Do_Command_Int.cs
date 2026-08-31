using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissionPlanner.Joystick
{
    public partial class Joy_Do_Command_Int : Form
    {
        private JoyButton _joyButton;
        private ModeStringParams _modeString = new ModeStringParams();
        private bool _initializing = true;

        public Joy_Do_Command_Int(string name)
        {
            // Init form
            InitializeComponent();
            Utilities.ThemeManager.ApplyThemeTo(this);
            this.Tag = name;

            // Get JoyButton instance
            _joyButton = MainV2.joystick.getButton(int.Parse(name));

            // Load previous values from mode string
            if (_joyButton.function == buttonfunction.Do_Command_Int)
            {
                _modeString = new ModeStringParams(_joyButton.mode);
            }

            // Init controls
            comboBox_comp.Tag = 0;
            comboBox_comp.DataSource = Enum.GetValues(typeof(MAVLink.MAV_COMPONENT));
            if (_joyButton.function == buttonfunction.Do_Command_Int)
            {
                comboBox_comp.SelectedItem = (MAVLink.MAV_COMPONENT)_modeString.CompID;
            }
            else
            {
                comboBox_comp.SelectedItem = MAVLink.MAV_COMPONENT.MAV_COMP_ID_AUTOPILOT1;
            }

            comboBox_command.Tag = 1;
            comboBox_command.DataSource = Enum.GetValues(typeof(MAVLink.MAV_CMD));
            if (_joyButton.function == buttonfunction.Do_Command_Int)
            {
                comboBox_command.SelectedItem = (MAVLink.MAV_CMD)_modeString.Command;
            }
            else
            {
                comboBox_command.SelectedItem = MAVLink.MAV_CMD.WAYPOINT;
            }
            
            numericUpDown_param1.Tag = 2;
            numericUpDown_param1.Minimum = decimal.MinValue;
            numericUpDown_param1.Maximum = decimal.MaxValue;
            numericUpDown_param1.Value = (decimal)_joyButton.p1;

            numericUpDown_param2.Tag = 3;
            numericUpDown_param2.Minimum = decimal.MinValue;
            numericUpDown_param2.Maximum = decimal.MaxValue;
            numericUpDown_param2.Value = (decimal)_joyButton.p2;

            numericUpDown_param3.Tag = 4;
            numericUpDown_param3.Minimum = decimal.MinValue;
            numericUpDown_param3.Maximum = decimal.MaxValue;
            numericUpDown_param3.Value = (decimal)_joyButton.p3;

            numericUpDown_param4.Tag = 5;
            numericUpDown_param4.Minimum = decimal.MinValue;
            numericUpDown_param4.Maximum = decimal.MaxValue;
            numericUpDown_param4.Value = (decimal)_joyButton.p4;

            numericUpDown_x.Tag = 6;
            numericUpDown_x.Minimum = Int32.MinValue;
            numericUpDown_x.Maximum = Int32.MaxValue;
            if (_joyButton.function == buttonfunction.Do_Command_Int)
            {
                numericUpDown_x.Value = _modeString.X;
            }

            numericUpDown_y.Tag = 7;
            numericUpDown_y.Minimum = Int32.MinValue;
            numericUpDown_y.Maximum = Int32.MaxValue;
            if (_joyButton.function == buttonfunction.Do_Command_Int)
            {
                numericUpDown_y.Value = _modeString.Y;
            }

            numericUpDown_z.Tag = 8;
            numericUpDown_z.Minimum = Int32.MinValue;
            numericUpDown_z.Maximum = Int32.MaxValue;
            if (_joyButton.function == buttonfunction.Do_Command_Int)
            {
                numericUpDown_z.Value = _modeString.Z;
            }

            _initializing = false;
        }

        private void _Update_Button()
        {
            MainV2.joystick.setButton(int.Parse(this.Tag.ToString()), _joyButton);
        }

        private void comboBox_SelectedIndexChaged(object sender, EventArgs e)
        {
            if (_initializing) return;

            ComboBox control = sender as ComboBox;

            switch (control.Tag)
            {
                case 0:
                    _modeString.CompID = (int)(MAVLink.MAV_COMPONENT)control.SelectedItem;
                    break;
                case 1:
                    _modeString.Command = (int)(MAVLink.MAV_CMD)control.SelectedItem;
                    break;
                default: break;
            }

            _joyButton.mode = _modeString.ToString();
            _Update_Button();
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            if (_initializing) return;

            NumericUpDown control = sender as NumericUpDown;

            switch (control.Tag)
            {
                // COMMAND_INT.param1-4 are stored in JoyButton.p1-4
                case 2:
                    _joyButton.p1 = (float)control.Value;
                    break;
                case 3:
                    _joyButton.p2 = (float)control.Value;
                    break;
                case 4:
                    _joyButton.p3 = (float)control.Value;
                    break;
                case 5:
                    _joyButton.p4 = (float)control.Value;
                    break;

                // COMMAND_INT.x-z are stored in JoyButton.mode
                case 6:
                    _modeString.X = (int)control.Value;
                    _joyButton.mode = _modeString.ToString();
                    break;
                case 7:
                    _modeString.Y = (int)control.Value;
                    _joyButton.mode = _modeString.ToString();
                    break;
                case 8:
                    _modeString.Z = (int)control.Value;
                    _joyButton.mode = _modeString.ToString();
                    break;

                default: break;
            }

            _Update_Button();
        }
    }

    internal class ModeStringParams
    {
        public int CompID { get; set; }
        public int Command { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public ModeStringParams(){}

        public ModeStringParams(string mode)
        {
            if (mode != null
                && mode.Length > 0
                && mode.Count(c => c == ';') == 4)
            {
                string[] parts = mode.Split(';');
                CompID = int.Parse(parts[0]);
                Command = int.Parse(parts[1]);
                X = int.Parse(parts[2]);
                Y = int.Parse(parts[3]);
                Z = int.Parse(parts[4]);
            }
        }

        public override string ToString()
        {
            int[] items = { CompID, Command, X, Y, Z };
            return string.Join(";", items);
        }
    }
}
