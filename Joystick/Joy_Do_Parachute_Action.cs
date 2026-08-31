using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static IronPython.Modules._ast;

namespace MissionPlanner.Joystick
{
    public partial class Joy_Do_Parachute_Action : Form
    {
        private JoyButton _joyButton;
        private bool _initializing = true;

        public Joy_Do_Parachute_Action(string id)
        {
            InitializeComponent();
            this.Tag = id;

            _joyButton = MainV2.joystick.getButton(int.Parse(id));

            comboBox_paraAction.DataSource = Enum.GetValues(typeof(MAVLink.PARACHUTE_ACTION));
            if (_joyButton.function == buttonfunction.Do_Parachute_Action)
            {
                comboBox_paraAction.SelectedItem = (MAVLink.PARACHUTE_ACTION)(int)_joyButton.p1;
            }
            else
            {
                comboBox_paraAction.SelectedItem = MAVLink.PARACHUTE_ACTION.PARACHUTE_RELEASE;
            }

            _initializing = false;
        }

        private void comboBox_paraAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;

            _joyButton.p1 = (float)(int)comboBox_paraAction.SelectedItem;
            MainV2.joystick.setButton(int.Parse(this.Tag.ToString()), _joyButton);
        }
    }
}
