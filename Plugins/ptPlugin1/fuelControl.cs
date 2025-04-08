using MissionPlanner.Utilities;
using MissionPlanner.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ptPlugin1
{
    public partial class fuelControl : UserControl
    {
        public int loadedFuel = 0;

        public float remainingFuel = 0;

        public event EventHandler loadedFuelClicked;

        public fuelControl()
        {
            InitializeComponent();
        }

        protected virtual void OnLoadedFuelClicked(EventArgs e)
        {
            EventHandler handler = this.loadedFuelClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void setData(float fuelLevel, float flow, float consumed, float loaded)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                remainingFuel = loaded - consumed;
                if (remainingFuel < 0) remainingFuel = 0;

                //range check
                gaugeFuel.Value0 = remainingFuel;
                gaugeFuel.Cap_Idx = 0;
                gaugeFuel.CapText = remainingFuel.ToString("F2") + "L";

                lLoaded.Text = "Loaded : " + loaded.ToString() + " l";
                IRaw.Text = "Raw level : " + fuelLevel.ToString("F0");
                lConsumed.Text = "Consumed : " + consumed.ToString("F2") + " liters";

            }));
        }

        private void bSetLoadedFuel_Click(object sender, EventArgs e)
        {
            var input = "27";
            if (InputBox.Show("Enter Loaded Fuel", "Please enter loaded fuel", ref input) == System.Windows.Forms.DialogResult.OK)
            {
                if (!Int32.TryParse(input, out loadedFuel))
                {
                    loadedFuel = 0;
                }
                if (loadedFuel > 30) loadedFuel = 30;
            }

            this.OnLoadedFuelClicked(EventArgs.Empty);
        }
    }
}
