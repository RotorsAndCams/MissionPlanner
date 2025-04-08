using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MissionPlanner.Controls;

namespace ptPlugin1
{
    public partial class payloadSetupForm : Form
    {
        public List<Payload> payloadStatus;

        public payloadSetupForm()
        {
            InitializeComponent();
            this.CenterToScreen();
            payloadStatus = new List<Payload>();
        }

        private void payloadsetup1_updateClicked(object sender, EventArgs e)
        {
            payloadStatus = payloadsetup1.payloadSetup;
            this.Close();
        }

        public void updateAll(List<Payload> pl)
        {
            payloadsetup1.updateAll(pl);
        }
    }
}
