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
    public partial class batterypanel : UserControl
    {
        private float servo1 = 0f;
        private float servo2 = 0;
        private float payload = 0;
        private float main = 0;
        private float gcs = 0;

        public batterypanel()
        {
            InitializeComponent();

            setServo1Voltage(servo1);
            setServo2Voltage(servo2);
            setPayloadVoltage(payload);
            setMainVoltage(main);
            setGcsVoltage(gcs,PowerLineStatus.Offline);
        }

        public void setServo1Voltage(float v)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                servo1 = v;
                gaugeServo1.Cap_Idx = 0;
                gaugeServo1.CapText = v.ToString("F1") + " V";

                if (v < 5) v = 5;
                if (v > 10) v = 10;

                gaugeServo1.Value0 = v;
            }));
        }

        public void setServo2Voltage(float v)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                servo2 = v;
                gaugeServo2.Cap_Idx = 0;
                gaugeServo2.CapText = v.ToString("F1") + " V";

                if (v < 5) v = 5;
                if (v > 10) v = 10;

                gaugeServo2.Value0 = v;
            }));
        }

        public void setPayloadVoltage(float v)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                payload = v;
                gaugePayload.Cap_Idx = 0;
                gaugePayload.CapText = v.ToString("F1") + " V";

                if (v < 7.5) v = 7.5f;
                if (v > 15) v = 15;

                gaugePayload.Value0 = v;
            }));
        }

        public void setMainVoltage(float v)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                main = v;
                gaugeMain.Cap_Idx = 0;
                gaugeMain.CapText = v.ToString("F1") + " V";

                if (v < 10) v = 10f;
                if (v > 20) v = 20;

                gaugeMain.Value0 = v;
            }));
        }

        public void setGcsVoltage(float v, PowerLineStatus lineStat)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                if (lineStat == PowerLineStatus.Online)
                {
                    gaugeGCS.Cap_Idx = 1;
                    gaugeGCS.CapText = "GCS Ext power";
                    v = 100;
                }
                else
                {
                    gaugeGCS.Cap_Idx = 1;
                    gaugeGCS.CapText = "GCS Batt";

                }

                gcs = v;

                gaugeGCS.Cap_Idx = 0;
                gaugeGCS.CapText = v.ToString("F0") + "%";

                if (v < 0) v = 0;
                if (v > 100) v = 100;

                gaugeGCS.Value0 = v;
            }));
        }

        //Returns   0 for NOMINAL
        //          1 for WARNING
        //          2 for ERROR
        public byte getGenericStatus()
        {
            byte retval = 0;

            switch (servo1)
            {
                case float f when f <= 6:
                        retval = 2;
                    break;
                case float f when f > 6 && f <= 6.8:
                    retval = 1;
                    break;
                case float f when f > 8.4:
                    retval = 2;
                    break;
                default:
                    break;
            }

            if (retval == 2) return retval;

            switch (servo2)
            {
                case float f when f <= 6:
                    retval = 2;
                    break;
                case float f when f > 6 && f <= 6.8:
                    retval = 1;
                    break;
                case float f when f > 8.4:
                    retval = 2;
                    break;
                default:
                    break;
            }

            if (retval == 2) return retval;

            switch (payload)
            {
                case float f when f <= 9:
                    retval = 2;
                    break;
                case float f when f > 9 && f <= 10.2:
                    retval = 1;
                    break;
                case float f when f > 12.6:
                    retval = 2;
                    break;
                default:
                    break;
            }
            if (retval == 2) return retval;

            switch (main)
            {
                case float f when f <= 12:
                    retval = 2;
                    break;
                case float f when f > 12 && f <= 13.6:
                    retval = 1;
                    break;
                case float f when f > 16.8:
                    retval = 2;
                    break;
                default:
                    break;
            }
            if (retval == 2) return retval;

            switch (gcs)
            {
                case float f when f <= 10:
                    retval = 2;
                    break;
                case float f when f > 10 && f <= 20:
                    retval = 1;
                    break;
                default:
                    break;
            }
            return retval;
        }
    }
}
