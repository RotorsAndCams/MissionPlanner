using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ptPlugin1
{
    [PreventTheming]
    public partial class ModandSet : UserControl
    {
        [System.ComponentModel.Browsable(false)]
        [PreventTheming]
        public NumericUpDown NumericUpDown
        {
            get { return numericUpDown1; }
        }
        [System.ComponentModel.Browsable(false)]
        public Label unitLabel
        {
            get { return lUnit; }
        }

        [System.ComponentModel.Browsable(false)]
        public MyButton Button
        {
            get { return myButton1; }
        }

        [System.ComponentModel.Browsable(true)]
        public String UnitText
        {
            get { return unitLabel.Text; }
            set { unitLabel.Text = value; }
        }

        [System.ComponentModel.Browsable(true)]
        public String ButtonText
        {
            get { return Button.Text; }
            set { Button.Text = value; }
        }

        [System.ComponentModel.Browsable(true)]
        public Decimal Increment
        {
            get { return NumericUpDown.Increment; }
            set { NumericUpDown.Increment = value; }
        }

        [System.ComponentModel.Browsable(true)]
        public int DecimalPlaces
        {
            get { return NumericUpDown.DecimalPlaces; }
            set { NumericUpDown.DecimalPlaces = value; }
        }

        [System.ComponentModel.Browsable(true)]
        public Decimal Value
        {
            get { return NumericUpDown.Value; }
            set { NumericUpDown.Value = value; }
        }

        [System.ComponentModel.Browsable(true)]
        public Decimal Minimum
        {
            get { return NumericUpDown.Minimum; }
            set { NumericUpDown.Minimum = value; }
        }

        [System.ComponentModel.Browsable(true)]
        public Decimal Maximum
        {
            get { return NumericUpDown.Maximum; }
            set { NumericUpDown.Maximum = value; }
        }

        public new event EventHandler Click;
        public event EventHandler ValueChanged;

        public ModandSet()
        {
            InitializeComponent();
        }

        private void myButton1_Click(object sender, EventArgs e)
        {
            if (Click != null)
                Click(sender, e);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (ValueChanged != null)
                ValueChanged(sender, e);
        }
    }
}
