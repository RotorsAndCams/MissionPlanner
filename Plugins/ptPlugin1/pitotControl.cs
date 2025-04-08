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
    public partial class pitotControl : UserControl
    {
        public event EventHandler calibrateClicked;

        public pitotControl()
        {
            InitializeComponent();
        }

        // Set Data
        public void setData(float pitotTemp, float ambientTemp, float targetTemp, float duty)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                // Range check
                if (duty < 0) duty = 0;
                else if (duty > 255) duty = 255;

                gaugePitotTemp.Value0 = pitotTemp;
                gaugePitotTemp.Cap_Idx = 0;
                gaugePitotTemp.CapText = pitotTemp.ToString("F0") + "C°";
                gaugePitotTemp.Value1 = targetTemp;
                gaugeAmbientTemp.Cap_Idx = 0;
                gaugeAmbientTemp.CapText = ambientTemp.ToString("F0") + "C°";
                gaugeAmbientTemp.Value0 = ambientTemp;
                heatPower.Value = (int)duty;
            }));
        }

        protected virtual void OncalibrateClicked(EventArgs e)
        {
            EventHandler handler = this.calibrateClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void calAirspeed_Click(object sender, EventArgs e)
        {
            this.OncalibrateClicked(EventArgs.Empty);
        }
    }
}
