using MissionPlanner;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using MissionPlanner.Controls.PreFlight;
using MissionPlanner.Controls;
using System.Linq;
using FastColoredTextBoxNS;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MissionPlanner.Maps;
using System.Text;
using System.Media;
using ClipperLib;
using System.Net.Sockets;
using System.Net;

namespace ptPlugin1
{
    public partial class fleetSetup : Form
    {
        List<int> idList = new List<int>();

        public fleetSetup()
        {
            InitializeComponent();

            lConnectedUAV.Text = "Connected UAV ID's : ";
            foreach (var p in MainV2.Comports)
            {
                lConnectedUAV.Text += p.sysidcurrent.ToString() + " / ";
                idList.Add(p.sysidcurrent);
                p.printbps = false;
            }

            tTail1.Text = "0";
            tTail2.Text = "0";
            tTail3.Text = "0";
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            int t1 = 0, t2 = 0, t3 = 0;
            string c1, c2, c3;

            try
            {
                t1 = Convert.ToInt16(tTail1.Text);
                t2 = Convert.ToInt16(tTail2.Text);
                t3 = Convert.ToInt16(tTail3.Text);  
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Input Error" + ex.Message, "ERROR");
                this.Close();
            }

            if ((cbEnable1.Checked == true) && (!idList.Contains(t1)))
            {
                CustomMessageBox.Show("SLOT 1 allocated an invalid Tail Number", "ERROR");
                this.Close();
            }
            if ((cbEnable2.Checked == true) && (!idList.Contains(t2)))
            {
                CustomMessageBox.Show("SLOT 2 allocated an invalid Tail Number", "ERROR");
                this.Close();
            }
            if ((cbEnable3.Checked == true) && (!idList.Contains(t3)))
            {
                CustomMessageBox.Show("SLOT 3 allocated an invalid Tail Number", "ERROR");
                this.Close();
            }

            ptPlugin1.slot1Enabled = cbEnable1.Checked;
            ptPlugin1.slot2Enabled = cbEnable2.Checked;
            ptPlugin1.slot3Enabled = cbEnable3.Checked;

            ptPlugin1.plane1ID = t1;
            ptPlugin1.plane2ID = t2;
            ptPlugin1.plane3ID = t3;

            ptPlugin1.plane1Name = tCallSign1.Text;
            ptPlugin1.plane2Name = tCallSign2.Text;
            ptPlugin1.plane3Name = tCallSign3.Text;

            //Call MainV2
            MainV2.instance.FlightPlanner.UpdateVehicleIdList(t1, t2, t3);

            this.Close();
        }
    }
}
