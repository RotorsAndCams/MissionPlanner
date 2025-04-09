using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    [PreventTheming]
    public partial class payloadcontrol : UserControl
    {
        public List<Payload> payloads;

        public event EventHandler igniteClicked;
        public event EventHandler setupClicked;

        public int igniteMask;

        public payloadcontrol()
        {
            InitializeComponent();

            payloads = new List<Payload>();
            payloads.Add(new Payload(PayloadPos.left1, l1, 64));
            payloads.Add(new Payload(PayloadPos.left2, l2, 128));
            payloads.Add(new Payload(PayloadPos.left3, l3, 256));
            payloads.Add(new Payload(PayloadPos.left4, l4, 512));

            payloads.Add(new Payload(PayloadPos.right1, r1, 4));
            payloads.Add(new Payload(PayloadPos.right2, r2, 8));
            payloads.Add(new Payload(PayloadPos.right3, r3, 16));
            payloads.Add(new Payload(PayloadPos.right4, r4, 32));

            payloads.Add(new Payload(PayloadPos.rear1, rear1, 2));
            payloads.Add(new Payload(PayloadPos.rear2, rear2, 1));

            
            foreach (Payload p in payloads)
            {
                p.type = PayloadType.empty;
            }

            redrawControls();
            
            //setPayloadType(PayloadPos.right1, PayloadType.flare);
            //setPayloadType(PayloadPos.rear2, PayloadType.smoke);
            //setPayloadType(PayloadPos.left1, PayloadType.flare);
        }

        private void payload_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            foreach (Payload p in payloads)
            {
                if (p.btn == btn)
                {
                    if ((p.state == PayloadState.ready) && (p.type != PayloadType.empty))
                    {
                        p.state = PayloadState.selected;
                    }
                    else if (p.state == PayloadState.selected)
                    {
                        p.state = PayloadState.ready;
                    }
                }
            }
            redrawControls();
        }

        public void updateAll(List<Payload> u)
        {
            foreach (Payload from in u)
            {
                foreach (Payload to in payloads)
                {
                    if (from.pos == to.pos)
                    {
                        to.state = from.state;
                        to.type = from.type;
                    }
                }
            }
            redrawControls();
        }

        public void setPayloadStatus(PayloadPos position, PayloadState state)
        {
            foreach (Payload p in payloads)
            {
                if (p.pos == position) p.state = state;
            }
            redrawControls();
        }


        public void setPayloadType(PayloadPos position, PayloadType type)
        {

            foreach (Payload p in payloads)
            {
                if (p.pos == position) p.type = type;
            }
            redrawControls();
        }

        public void redrawControls()
        {

            foreach (Payload p in payloads)
            {
                switch (p.type)
                {
                    case PayloadType.empty:
                        p.btn.Text = "EMPTY";
                        p.btn.BackColor = Color.Gray;
                        break;
                    case PayloadType.smoke:
                        p.btn.Text = "SMOKE";
                        p.btn.BackColor = Color.Lime;
                        break;
                    case PayloadType.flare:
                        p.btn.Text = "FLARE";
                        p.btn.BackColor = Color.Green;
                        break;
                    default:
                        break;
                }
                switch (p.state)
                {
                    case PayloadState.ready:
                        break;
                    case PayloadState.selected:
                        p.btn.BackColor = Color.Yellow;
                        break;
                    case PayloadState.ignited:
                        p.btn.BackColor = Color.Maroon;
                        break;
                    default:
                        break;
                }

            }

            btnIgnite.ForeColor = Color.DarkSlateGray;
            btnIgnite.BackColor = Color.Red;
        }

        private void btnIgnite_Click(object sender, EventArgs e)
        {
            igniteMask = 0;
            foreach (Payload p in payloads)
            {
                if (p.state == PayloadState.selected)
                {
                    igniteMask += p.bitMask;
                    p.state = PayloadState.ignited;
                }
            }
            redrawControls();
            this.OnIgniteClicked(EventArgs.Empty);
        }
        protected virtual void OnSetupClicked(EventArgs e)
        {
            EventHandler handler = this.setupClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected virtual void OnIgniteClicked(EventArgs e)
        {
            EventHandler handler = this.igniteClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void btnSetup_Click(object sender, EventArgs e)
        {
            this.OnSetupClicked(EventArgs.Empty);
        }


        public void setupEnabled(bool enabled)
        {
            btnSetup.Enabled = enabled;
        }

        public void setSafetyStatus(bool status)
        {
            MissionPlanner.MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                bWarning.Visible = status;
            }));
        }
    }

    public enum PayloadPos { none = -1, left1 = 0, left2, left3, left4, right1, right2, right3, right4, rear1, rear2 }
    public enum PayloadType { empty, smoke, flare }
    public enum PayloadState { ready, selected, ignited };

    public class Payload
    {
        public PayloadPos pos { get; set; }
        public PayloadType type { get; set; }
        public PayloadState state { get; set; }
        public Button btn { get; set; }
        public int bitMask { get; set; }

        public Payload()
        {
            pos = PayloadPos.none;
            type = PayloadType.empty;
            state = PayloadState.ready;
            btn = null;
            bitMask = 0;
        }

        public Payload(PayloadPos p)
        {
            pos = p;
            type = PayloadType.empty;
            state = PayloadState.ready;
            btn = null;
            bitMask = 0;
        }

        public Payload(PayloadPos p, Button b, int bit)
        {
            pos = p;
            type = PayloadType.empty;
            state = PayloadState.ready;
            btn = b;
            bitMask = bit;
        }

        public void ignite()
        {
            state = PayloadState.ignited;
        }
    }
}
