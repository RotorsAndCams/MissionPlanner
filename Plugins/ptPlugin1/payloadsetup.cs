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
    public partial class payloadsetup : UserControl
    {
        public List<Payload> payloadSetup;

        public event EventHandler updateClicked;

        public payloadsetup()
        {
            InitializeComponent();

            payloadSetup = new List<Payload>();
            payloadSetup.Add(new Payload(PayloadPos.left1, l1, 64));
            payloadSetup.Add(new Payload(PayloadPos.left2, l2, 128));
            payloadSetup.Add(new Payload(PayloadPos.left3, l3, 256));
            payloadSetup.Add(new Payload(PayloadPos.left4, l4, 512));

            payloadSetup.Add(new Payload(PayloadPos.right1, r1, 4));
            payloadSetup.Add(new Payload(PayloadPos.right2, r2, 8));
            payloadSetup.Add(new Payload(PayloadPos.right3, r3, 16));
            payloadSetup.Add(new Payload(PayloadPos.right4, r4, 32));

            payloadSetup.Add(new Payload(PayloadPos.rear1, rear1, 2));
            payloadSetup.Add(new Payload(PayloadPos.rear2, rear2, 1));

            redrawControls();
        }

        private void payload_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            foreach (Payload p in payloadSetup)
            {
                if (p.btn == btn)
                {
                    switch (p.type)
                    {
                        case PayloadType.empty:
                            p.type = PayloadType.smoke;
                            break;
                        case PayloadType.smoke:
                            p.type = PayloadType.flare;
                            break;
                        case PayloadType.flare:
                            p.type = PayloadType.empty;
                            break;
                        default:
                            break;
                    }
                }
            }
            redrawControls();
        }

        public void updateAll(List<Payload> u)
        {
            foreach (Payload from in u)
            {
                foreach (Payload to in payloadSetup)
                {
                    if (from.pos == to.pos)
                    {
                        to.state = PayloadState.ready;
                        to.type = from.type;
                    }
                }
            }
            redrawControls();
        }

        public void setPayloads(PayloadPos position, PayloadType type)
        {
            foreach (Payload p in payloadSetup)
            {
                if (p.pos == position) p.type = type;
            }
            redrawControls();
        }

        public void redrawControls()
        {
            foreach (Payload p in payloadSetup)
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

            btnUpdate.ForeColor = Color.DarkSlateGray;
            btnUpdate.BackColor = Color.Red;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            redrawControls();
            this.OnUpdateClicked(EventArgs.Empty);
        }

        protected virtual void OnUpdateClicked(EventArgs e)
        {
            EventHandler handler = this.updateClicked;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
