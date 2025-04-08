using MissionPlanner.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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
    public partial class TimedWpConsole : Form
    {
        public int selectedWaypoint = 0;
        public int routetime = 0;

        public TimedWpConsole()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dgV.Rows.Add();
        }

        public void addRow(int wp, int time)
        {
            var r = dgV.Rows.Add();
            dgV.Rows[r].Cells[WP.Index].Value = (wp+1).ToString();
            dgV.Rows[r].Cells[Time1.Index].Value = time.ToString();

            txtWP.Text = dgV.Rows[0].Cells[WP.Index].Value.ToString();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime time = DateTime.Now;
            TimeSpan route = TimeSpan.FromSeconds(routetime);
            lActualTime.Text = time.ToString("HH:mm:ss");
            TimeSpan remaining =  dateTimePicker1.Value.Subtract(time) - route;
            lRemaining.Text = $"{remaining.Hours.ToString("00")}:{remaining.Minutes.ToString("00")}:{remaining.Seconds.ToString("00")}";
        }

        private void dgV_SelectionChanged(object sender, EventArgs e)
        {
            selectedWaypoint = Int16.Parse((string)dgV.CurrentRow.Cells[WP.Index].Value.ToString());
            routetime = Int16.Parse((string)dgV.CurrentRow.Cells[Time1.Index].Value.ToString());
            lTarget.Text = $"WP {selectedWaypoint} reach time";
        }
    }
}
