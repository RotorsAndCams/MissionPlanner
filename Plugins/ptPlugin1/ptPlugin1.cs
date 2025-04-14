using ClipperLib;
using FastColoredTextBoxNS;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Controls.PreFlight;
using MissionPlanner.Maps;
using MissionPlanner.Plugin;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using static MissionPlanner.Utilities.LTM;

//By Bandi
namespace ptPlugin1
{
    public enum LandState
    {
        None,
        Ready,
        GoToWaiting,
        WaitForSpeed,
        WaitForTangent,
        GoToLand,
        CloseToLand,
        Land
    }

    public enum ChuteState
    {
        AutoOpenDisabled,
        AutoOpenEnabled
    }

    public class Situation
    {
        public int sysid1;
        public PointLatLngAlt pos1;
        public float heading1;
        public float airspeed1;

        public int sysid2;
        public PointLatLngAlt pos2;
        public float heading2;
        public float airspeed2;

        public int sysid3;
        public PointLatLngAlt pos3;
        public float heading3;
        public float airspeed3;
    }

    [PreventTheming]
    public partial class ptPlugin1 : Plugin
    {
        public Situation sit = new Situation();
        
        public static int plane1ID = 0;
        public static int plane2ID = 0;    
        public static int plane3ID = 0;

        public static string plane1Name = "";
        public static string plane2Name = "";
        public static string plane3Name = "";

        public static bool slot1Enabled = false;
        public static bool slot2Enabled = false;
        public static bool slot3Enabled = false;

        annunciator aMain1 = new annunciator(20, new Size(130, 40));
        annunciator aMain2 = new annunciator(20, new Size(130, 40));
        annunciator aMain3 = new annunciator(20, new Size(130, 40));

        public bool SetupDone = false;

        public static FloatingForm annunciatorForm = new FloatingForm();

        public TabPage payloadControlpage = new TabPage();
        public payloadcontrol plControl = new payloadcontrol();
        public List<Payload> payloadSettings = new List<Payload>();
        payloadSetupForm fPs;

        public TabPage engineControlPage = new TabPage();
        public engineControl eCtrl = new engineControl();

        public TabPage colorMessagePage = new TabPage();
        public FastColoredTextBox fctb;
        public DateTime lastDisplayedMessage1 = DateTime.MinValue;
        public DateTime lastDisplayedMessage2 = DateTime.MinValue;
        public DateTime lastDisplayedMessage3 = DateTime.MinValue;

        TextStyle infoStyle = new TextStyle(Brushes.White, null, FontStyle.Regular);
        TextStyle warningStyle = new TextStyle(Brushes.BurlyWood, null, FontStyle.Regular);
        TextStyle errorStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);

        public TabPage connectionControlPage = new TabPage();
        public ConnectionStats connectionStats;

        public TabPage batteryPage = new TabPage();
        public batterypanel bp = new batterypanel();

        public TabPage ekfPage = new TabPage();
        public ekfStatControl ekfStat = new ekfStatControl();

        public TabPage pitotPage = new TabPage();
        public pitotControl pitot = new pitotControl();

        public TabPage fuelPage = new TabPage();
        public fuelControl fuel = new fuelControl();

        public TabPage landingPage = new TabPage();
        public landingControl lc = new landingControl();

        public TabPage overviewPage = new TabPage();
        public TableLayoutPanel tlOw = new TableLayoutPanel();   
        public Dictionary<string, Label> oWlabels = new Dictionary<string, Label>();

        public TabPage ftPage = new TabPage();
        public TableLayoutPanel tlFT = new TableLayoutPanel();
        public Dictionary<string, Label> FTlabels = new Dictionary<string, Label>();

        public TabPage twpPage = new TabPage();

        public ToolStripMenuItem tsLandingPoint = new ToolStripMenuItem();
        public ToolStripMenuItem tsStartSim = new ToolStripMenuItem();
        public ToolStripMenuItem tsSetupFleet = new ToolStripMenuItem();
        public ToolStripMenuItem tsDoAutoConnect = new ToolStripMenuItem();

        string actualPanel = "";

        public int chuteServo;
        private string _chuteServoKey = "chuteServo";
        public int chuteServoOpenPWM;
        private string _chuteServoOpenPWMKey = "chuteServoOpenPWM";

        internal GMapMarker markerLanding;
        internal GMapMarker markerWaiting;
        internal GMapMarker markerTarget;

        internal GMapRoute landingRoute;
        internal static GMapOverlay landingOverlay;

        public LandState landState = LandState.None;

        DateTime lastNonCriticalUpdate = DateTime.MinValue;

        // UDP client for the Flight Termination System Data display
        // IP address and port is hardcoded (192.168.69.99 - FT, 192.168.69.100 - GCS, PORT - 19728
        UdpClient FTudpClient;
        IPEndPoint FTudpEndPoint;
        internal static GMapOverlay FTOverlay = new GMapOverlay();

        private IPAddress _flightTermUDPAddr;
        private string _flightTermUDPAddrKey = "Protar_FTUDPAddr";
        private int _flightTermUDPPort;
        private string _flightTermUDPPortKey = "Protar_FTUDPPort";

        private int _telemetryUdpPort;
        private string _telemetryUdpPortKey = "Protar_TelemUDPPort";

        private int _jetControlChannel;
        private string _jetControlChannelKey = "jetcontrolch";

        private bool _isSupervisor;
        private string _isSupervisorKey = "Protar_Supervisor";

        #region Plugin info

        public override string Name
        {
            get { return "ptPlugin1"; }
        }

        public override string Version
        {
            get { return "1.0"; }
        }

        public override string Author
        {
            get { return "RotorsAndCams"; }
        }

        #endregion

        // Init called when the plugin dll is loaded
        //[DebuggerHidden]
        public override bool Init()
        {
            loopratehz = 5;  // Loop runs every second (The value is in Hertz, so 2 means every 500ms, 0.1f means every 10 second...) 
            return true;	 // If it is false then plugin will not load
        }

        // Loaded called after the plugin dll successfully loaded
        public override bool Loaded()
        {
            // Get supervisor state from config
            _isSupervisor = Host.config.GetBoolean(_isSupervisorKey, false);
            Host.config[_isSupervisorKey] = _isSupervisor.ToString();

            tsLandingPoint.Text = "Set Landing Point";
            tsLandingPoint.Click += TsLandingPoint_Click;
            Host.FDMenuMap.Items.Add(tsLandingPoint);
            landingOverlay = new GMapOverlay("landing");

            tsStartSim.Text = "Start Simulator";
            tsStartSim.Click += TsStartSim_Click;
            Host.FDMenuMap.Items.Add(tsStartSim);

            tsSetupFleet.Text = "Setup Fleet";
            tsSetupFleet.Click += TsSetupFleet_Click;
            Host.FDMenuMap.Items.Add(tsSetupFleet);

            tsDoAutoConnect.Text = "Trigger AutoConnect";
            tsDoAutoConnect.Click += TsDoAutoConnect_Click; 
            Host.FDMenuMap.Items.Add(tsDoAutoConnect);

            Panel panel1 = Host.MainForm.Controls.Find("Panel1", true).FirstOrDefault() as Panel;

            string[] btnLabels = new string[] {
                "PLANE",
                "FLIGHT"+ Environment.NewLine +"DATA",
                "FLIGHT" + Environment.NewLine +"PLAN",
                "GEO"+ Environment.NewLine +"FENCE",
                "SETUP",
                "TUNING",
                "ENGINE",
                "FUEL",
                "BATT",
                "PAY"+ Environment.NewLine +"LOAD",
                "AIR"+ Environment.NewLine +"SPEED",
                "EKF",
                "WEATHER",
                "LAND",
                "LAUNCH",
                "PRE" + Environment.NewLine + "FLGHT",
                "ACTIONS",
                "DATA",
                "MSG",
                "COMMS" };

            string[] btnNames = new string[] {
                "PLANE",
                "FD",
                "FP",
                "GF",
                "SETUP",
                "TUNING",
                "ENGINE",
                "FUEL",
                "BATT",
                "PAYLD",
                "PITOT",
                "EKF",
                "WEATHER",
                "LAND",
                "START",
                "PREFLGHT",
                "ACTIONS",
                "DATA",
                "MSG",
                "COMMS" };

            aMain1.setPanels(btnNames, btnLabels);
            aMain1.Enabled = true;
            aMain1.Location = new Point(0, 0);
            aMain1.Size = new Size(panel1.Width, 47);
            aMain1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            aMain1.undock += annunciator1_undock;
            aMain1.buttonClicked += annunciator1_buttonClicked;
            aMain1.Visible = true;
            aMain1.SysID = 0;
            aMain1.Name = "";
            aMain1.Active = true;
            aMain1.Refresh();

            aMain2.setPanels(btnNames, btnLabels);
            aMain2.Enabled = true;
            aMain2.Location = new Point(0, 47);
            aMain2.Size = new Size(panel1.Width, 47);
            aMain2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            //aMain2.undock += annunciator2_undock;
            aMain2.buttonClicked += annunciator1_buttonClicked;
            aMain2.Visible = true;
            aMain2.SysID = 0;
            aMain2.Name = "";
            aMain2.Active = false;
            aMain2.Refresh();

            aMain3.setPanels(btnNames, btnLabels);
            aMain3.Enabled = true;
            aMain3.Location = new Point(0, 94);
            aMain3.Size = new Size(panel1.Width, 47);
            aMain3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            //aMain3.undock += annunciator2_undock;
            aMain3.buttonClicked += annunciator1_buttonClicked;
            aMain3.Visible = true;
            aMain3.SysID = 0;
            aMain3.Name = "";
            aMain3.Active = false;
            aMain3.Refresh();

            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                MenuStrip MainMenu = Host.MainForm.Controls.Find("MainMenu", true).FirstOrDefault() as MenuStrip;
                if (MainMenu != null) MainMenu.Visible = false;

                panel1.Size = new Size(panel1.Width, 141);
            }));

            // Check undocked status
            if (Host.config.ContainsKey("aMainDocked"))
            {
                bool aMainDocked = Host.config.GetBoolean("aMainDocked");
                if (!aMainDocked)
                {
                    annunciator1_undock(this, new EventArgs());
                }
                else
                {
                    panel1.Controls.Add(aMain1);
                    panel1.Controls.Add(aMain2); 
                    panel1.Controls.Add(aMain3);
                }
            }
            else
            {
                panel1.Controls.Add(aMain1);
                panel1.Controls.Add(aMain2);
                panel1.Controls.Add(aMain3);
            }

            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                //If this is not a supervisor then you have to setup only one ID
                if (!_isSupervisor)
                {
                    aMain1.Active = true;
                    aMain2.Active = false;
                    aMain3.Active = false;
                    panel1.Size = new Size(panel1.Width, 47);
                }
            }));

            colorMessagePage.Text = "ColorMessages";
            colorMessagePage.Name = "colorMsgTab";
            fctb = new FastColoredTextBoxNS.FastColoredTextBox();
            setupFCTB();
            this.fctb.Size = colorMessagePage.ClientSize;
            colorMessagePage.Controls.Add(fctb);
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(colorMessagePage);

            payloadControlpage.Text = "PayloadControl";
            payloadControlpage.Name = "payLoadCTRTab";

            plControl.Name = "plControl";
            plControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            plControl.Location = new Point(0, 0);
            plControl.Size = new Size(payloadControlpage.Width, payloadControlpage.Height);
            plControl.setupClicked += PlControl_setupClicked;
            plControl.igniteClicked += PlControl_igniteClicked;
            payloadControlpage.Controls.Add(plControl);
            plControl.redrawControls();
            plControl.setSafetyStatus(false);
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(payloadControlpage);

            engineControlPage.Text = "Engine Control";
            engineControlPage.Name = "engCtrlTab";

            eCtrl.Name = "engineControl";
            eCtrl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            eCtrl.Location = new Point(0, 0);
            eCtrl.Size = new Size(engineControlPage.Width, engineControlPage.Height);
            engineControlPage.Controls.Add(eCtrl);
            eCtrl.armClicked += ECtrl_armClicked;

            // Get jet control vars from config
            _jetControlChannel = Host.config.GetInt32(_jetControlChannelKey, 10);
            Host.config[_jetControlChannelKey] = _jetControlChannel.ToString();

            // Set up det control button events
            eCtrl.startClicked += ECtrl_startClicked;
            eCtrl.stopClicked += ECtrl_stopClicked;
            eCtrl.emergencyClicked += ECtrl_emergencyClicked;

            Host.MainForm.FlightData.tabControlactions.TabPages.Add(engineControlPage);

            eCtrl.setEngineStatus("Ready to Start", "No error");

            connectionControlPage.Text = "Comms";
            connectionControlPage.Name = "commsTab";

            connectionControlPage.Controls.Add(MainV2._connectionControl);
            ToolStrip ts = new ToolStrip();
            ts.BackColor = Color.Black;
            ts.Items.Add(MainV2.instance.MenuConnect);
            connectionControlPage.Controls.Add(ts);
            MainV2._connectionControl.Location = new Point(0, 0);
            connectionStats = new ConnectionStats(Host.comPort);
            connectionStats.Location = new Point(0, 50);
            connectionControlPage.Controls.Add(connectionStats);
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(connectionControlPage);

            batteryPage.Text = "Battery";
            batteryPage.Name = "battTab";
            bp.Location = new Point(0, 0);
            bp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bp.Size = batteryPage.ClientSize;
            batteryPage.Controls.Add(bp);
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(batteryPage);

            ekfPage.Text = "EKF";
            ekfPage.Name = "ekfTab";
            ekfPage.Controls.Add(ekfStat);
            ekfStat.Size = ekfPage.ClientSize;
            ekfStat.Location = new Point(0, 0);
            ekfStat.Dock = DockStyle.Fill;
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(ekfPage);

            pitotPage.Text = "Airspeed";
            pitotPage.Name = "pitotTab";
            pitotPage.Controls.Add(pitot);
            pitot.Size = pitotPage.ClientSize;
            pitot.Location = new Point(0, 0);
            pitot.Dock = DockStyle.Fill;
            pitot.calibrateClicked += Pitot_calibrateClicked;
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(pitotPage);

            fuelPage.Text = "Fuel";
            fuelPage.Name = "fuelTab";
            fuelPage.Controls.Add(fuel);
            fuel.Size = fuelPage.ClientSize;
            fuel.Location = new Point(0, 0);
            fuel.Dock = DockStyle.Fill;
            fuel.loadedFuelClicked += Fuel_loadedFuelClicked;
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(fuelPage);

            landingPage.Text = "Landing";
            landingPage.Name = "landingTab";
            landingPage.Controls.Add(lc);
            lc.Size = landingPage.ClientSize;
            lc.Location = new Point(0, 0);
            lc.Dock = DockStyle.Fill;
            lc.StartLandingClicked += Lc_startLandingClicked;
            lc.setspeedClicked += Lc_setspeedClicked;
            lc.setCruiseSpeedClicked += Lc_setCruiseSpeedClicked;
            lc.abortLandingClicked += Lc_abortLandingClicked;
            lc.nudgeSpeedClicked += Lc_nudgeSpeedClicked;
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(landingPage);

            initTWPConsole();
            twpPage.Text = "Timed Waypoints";
            twpPage.Name = "twpPage";
            twpPage.Controls.Add(twpPanel);
            twpPanel.Size = twpPage.ClientSize;
            twpPanel.Location = new Point(0, 0);
            twpPanel.Dock = DockStyle.Fill;
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(twpPage);

            overviewPage.Text = "Overview";
            overviewPage.Name = "overViewTab";
            overviewPage.Controls.Add(tlOw);
            tlOw.Size = overviewPage.ClientSize;
            tlOw.Location = new Point(3,3);
            tlOw.Dock = DockStyle.Fill;
            tlOw.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            tlOw.ColumnCount = 4;
            tlOw.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlOw.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlOw.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlOw.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlOw.RowCount = 12;
            tlOw.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            for (int i = 0; i < 12; i++)
            {
                Label l = new Label();
                l.AutoSize = true;
                l.Dock = DockStyle.Fill;
                l.Font = new Font("Arial", 12,FontStyle.Bold);
                tlOw.Controls.Add(l, 0, i);

                Label l1 = new Label();
                l1.Font = new Font("Arial", 12);
                l1.Dock = DockStyle.Fill;
                l1.TextAlign = ContentAlignment.TopCenter;
                l1.AutoSize = true;
                tlOw.Controls.Add(l1, 1, i);

                Label l2 = new Label();
                l2.Font = new Font("Arial", 12);
                l2.Dock = DockStyle.Fill;
                l2.TextAlign = ContentAlignment.TopCenter;
                l2.AutoSize = true;
                tlOw.Controls.Add(l2, 2, i);

                Label l3 = new Label();
                l3.Font = new Font("Arial", 12);
                l3.Dock = DockStyle.Fill;
                l3.TextAlign = ContentAlignment.TopCenter;
                l3.AutoSize = true;
                tlOw.Controls.Add(l3, 2, i);
            }

            // Update Row Names
            ((Label)tlOw.GetControlFromPosition(0, 0)).Text = "SYSID";
            ((Label)tlOw.GetControlFromPosition(0, 1)).Text = "Callsign";
            ((Label)tlOw.GetControlFromPosition(0, 2)).Text = "AirSpeed";
            ((Label)tlOw.GetControlFromPosition(0, 3)).Text = "GroundSpeed";
            ((Label)tlOw.GetControlFromPosition(0, 4)).Text = "Altitude";
            ((Label)tlOw.GetControlFromPosition(0, 5)).Text = "ClimbRate";
            ((Label)tlOw.GetControlFromPosition(0, 6)).Text = "Fuel Consumed";
            ((Label)tlOw.GetControlFromPosition(0, 7)).Text = "Radio Health";
            ((Label)tlOw.GetControlFromPosition(0, 8)).Text = "Distance To Home";
            ((Label)tlOw.GetControlFromPosition(0, 9)).Text = "Engine RPM";
            ((Label)tlOw.GetControlFromPosition(0, 10)).Text = "Engine EGT";
            ((Label)tlOw.GetControlFromPosition(0, 11)).Text = "Engine Thr";

            Host.MainForm.FlightData.tabControlactions.TabPages.Add(overviewPage);

            #region FTDataDisplaySetup

            // Flight termination data display setup

            ftPage.Text = "Flight Termination System";
            ftPage.Name = "FlightTermViewTab";
            ftPage.Controls.Add(tlFT);
            tlFT.Size = ftPage.ClientSize;
            tlFT.Location = new Point(3, 3);
            tlFT.Dock = DockStyle.Fill;
            tlFT.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            tlFT.ColumnCount = 4;
            tlFT.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlFT.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlFT.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlFT.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlFT.RowCount = 11;
            tlFT.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            for (int i = 0; i < 10; i++)
            {
                Label l = new Label();
                l.AutoSize = true;
                l.Dock = DockStyle.Fill;
                l.Font = new Font("Arial", 12, FontStyle.Bold);
                tlFT.Controls.Add(l, 0, i);

                Label l1 = new Label();
                l1.Font = new Font("Arial", 12);
                l1.Dock = DockStyle.Fill;
                l1.TextAlign = ContentAlignment.TopCenter;
                l1.AutoSize = true;
                tlFT.Controls.Add(l1, 1, i);

                Label l2 = new Label();
                l2.Font = new Font("Arial", 12);
                l2.Dock = DockStyle.Fill;
                l2.TextAlign = ContentAlignment.TopCenter;
                l2.AutoSize = true;
                tlFT.Controls.Add(l2, 2, i);

                Label l3 = new Label();
                l3.Font = new Font("Arial", 12);
                l3.Dock = DockStyle.Fill;
                l3.TextAlign = ContentAlignment.TopCenter;
                l3.AutoSize = true;
                tlFT.Controls.Add(l3, 2, i);
            }

            Label la = new Label();
            la.Font = new Font("Arial", 12);
            la.Dock = DockStyle.Fill;
            la.TextAlign = ContentAlignment.TopCenter;
            la.AutoSize = true;
            tlFT.Controls.Add(la, 0, 10);

            CheckBox cb = new CheckBox();
            cb.Dock = DockStyle.Fill;
            cb.AutoSize = true;
            cb.Checked = false;
            cb.AutoSize = true;
            tlFT.Controls.Add(cb, 1, 10);
             
            // Update Row Names
            ((Label)tlFT.GetControlFromPosition(0, 0)).Text = "SYSID";
            ((Label)tlFT.GetControlFromPosition(0, 1)).Text = "FT State";
            ((Label)tlFT.GetControlFromPosition(0, 2)).Text = "AirSpeed";
            ((Label)tlFT.GetControlFromPosition(0, 3)).Text = "POS LAT";
            ((Label)tlFT.GetControlFromPosition(0, 4)).Text = "POS LON";
            ((Label)tlFT.GetControlFromPosition(0, 5)).Text = "Alt AMSL";
            ((Label)tlFT.GetControlFromPosition(0, 6)).Text = "Heading";
            ((Label)tlFT.GetControlFromPosition(0, 7)).Text = "Flight Mode";
            ((Label)tlFT.GetControlFromPosition(0, 8)).Text = "AIR SNR";
            ((Label)tlFT.GetControlFromPosition(0, 9)).Text = "AIR RSSI";
            ((Label)tlFT.GetControlFromPosition(0, 10)).Text = "Show on Map";

            Host.MainForm.FlightData.tabControlactions.TabPages.Add(ftPage);
            
            // End of flight termination data display setup
            
            #endregion

            // Get servo settings from config
            chuteServo = Host.config.GetInt32(_chuteServoKey, 9);
            Host.config[_chuteServoKey] = chuteServo.ToString();
            chuteServoOpenPWM = Host.config.GetInt32(_chuteServoOpenPWMKey, 1100);
            Host.config[_chuteServoOpenPWMKey] = chuteServoOpenPWM.ToString();

            // Get telemetry UDP port from config
            _telemetryUdpPort = Host.config.GetInt32(_telemetryUdpPortKey, 19729);
            Host.config[_telemetryUdpPortKey] = _telemetryUdpPort.ToString();

            // Get Flight Termination UDP address and port from config
            _flightTermUDPAddr = IPAddress.Parse(Host.config[_flightTermUDPAddrKey, "192.168.69.99"]);
            Host.config[_flightTermUDPAddrKey] = _flightTermUDPAddr.ToString();
            _flightTermUDPPort = Host.config.GetInt32(_flightTermUDPPortKey, 19728);
            Host.config[_flightTermUDPPortKey] = _flightTermUDPPort.ToString();

            // Set up Fligh Termination UDP packet receiving 
            FTudpEndPoint = new IPEndPoint(_flightTermUDPAddr, _flightTermUDPPort);
            FTudpClient = new UdpClient(_flightTermUDPPort);
            FTudpClient.BeginReceive(new AsyncCallback(ProcessFTMessage), null);

            // Add Flight Termination Overlay
            FTOverlay.Id = "FTO";
            Host.FDGMapControl.Overlays.Add(FTOverlay);

            return true; // If it is false plugin will not start (loop will not called)
        }

        private void ProcessFTMessage(IAsyncResult result)
        {
            try
            {
                // Get message
                byte[] message = FTudpClient.EndReceive(result, ref FTudpEndPoint);

                // Restart listener
                FTudpClient.BeginReceive(new AsyncCallback(ProcessFTMessage), null);

                // Process the message
                Console.WriteLine("FT Message received, SysID:" + message[0].ToString());

                int sysid = message[0];
                string ftstate;
                switch (message[1])
                {
                    case 0:
                        ftstate = "PRESS TEST";
                        break;
                    case 1:
                        ftstate = "Normal";
                        break;
                    case 2:
                        ftstate = "RTL invoked";
                        break;
                    case 3:
                        ftstate = "TERMINATE";
                        break;
                    default:
                        ftstate = "UNKNOWN";
                        break;
                }

                string fltmode;
                switch (message[15])
                {
                    case 1:
                        fltmode = "Circle";
                        break;
                    case 2:
                        fltmode = "Stabilize";
                        break;
                    case 5:
                        fltmode = "FBW A";
                        break;
                    case 6:
                        fltmode = "FBW B";
                        break;
                    case 10:
                        fltmode = "Auto";
                        break;
                    case 11:
                        fltmode = "RTL";
                        break;
                    case 12:
                        fltmode = "Loiter";
                        break;
                    case 15:
                        fltmode = "Guided";
                        break;
                    default:
                        fltmode = "Mode " + message[14].ToString();
                        break;
                }

                byte[] conv_int32 = { 0, 0, 0, 0 };
                conv_int32[0] = message[2]; conv_int32[1] = message[3]; conv_int32[2] = message[4]; conv_int32[3] = message[5];
                float pos_lat = (float)(BitConverter.ToInt32(conv_int32, 0) / 1e7);
                conv_int32[0] = message[6]; conv_int32[1] = message[7]; conv_int32[2] = message[8]; conv_int32[3] = message[9];
                float pos_lon = (float)(BitConverter.ToInt32(conv_int32, 0) / 1e7);

                int airspeed = message[14];
                byte[] c = { 0, 0 };
                c[0] = message[10]; c[1] = message[11];
                int alt_amsl = BitConverter.ToInt16(c, 0);
                c[0] = message[12]; c[1] = message[13];
                int heading = BitConverter.ToInt16(c, 0);

                MainV2.instance.BeginInvoke((MethodInvoker)(() =>
                {
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 0)).Text = sysid.ToString();
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 1)).Text = ftstate;
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 2)).Text = airspeed.ToString() + " m/s";
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 3)).Text = pos_lat.ToString("F6");
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 4)).Text = pos_lon.ToString("F6");
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 5)).Text = alt_amsl.ToString() + " m";
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 6)).Text = heading.ToString() + " deg";
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 7)).Text = fltmode;
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 8)).Text = message[16].ToString() + "dBm";
                    ((Label)tlFT.GetControlFromPosition(message[18] + 1, 9)).Text = ((sbyte)message[17]).ToString() + "dBm";
                }));

                bool showplanes = ((CheckBox)tlFT.GetControlFromPosition(1, 10)).Checked;
                if (!showplanes)
                {
                    FTOverlay.Markers.Clear();
                }
                else
                {

                    GMapMarkerPlaneSitu marker_to_remove = null;

                    foreach (GMapMarkerPlaneSitu marker in FTOverlay.Markers)
                    {
                        if (marker.which == sysid)
                        {
                            marker_to_remove = marker;
                        }
                    }

                    if (marker_to_remove != null)
                    {
                        FTOverlay.Markers.Remove(marker_to_remove);
                    }
                    GMapMarkerPlaneSitu new_marker = new GMapMarkerPlaneSitu(sysid, new PointLatLngAlt(pos_lat, pos_lon, 0), heading, airspeed);
                    new_marker.ToolTipText = "Alt:" + alt_amsl.ToString("F0") + Environment.NewLine + "IAS:" + airspeed.ToString("F0");
                    new_marker.ToolTipMode = MarkerTooltipMode.Always;
                    FTOverlay.Markers.Add(new_marker);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Flight Termination message parse error: {ex.Message}");
            }
        }

        private void Fuel_loadedFuelClicked(object sender, EventArgs e)
        {
            Host.cs.fuel_loaded = fuel.loadedFuel;
        }

        private void TsDoAutoConnect_Click(object sender, EventArgs e)
        {
            AutoConnect.Start();
        }

        // Setup sysid's and names and number of planes in the air;
        private void TsSetupFleet_Click(object sender, EventArgs e)
        {
            fleetSetup frm = new fleetSetup();
            frm.ShowDialog();

            aMain1.SysID = plane1ID;
            aMain2.SysID = plane2ID;
            aMain3.SysID = plane3ID;    
            aMain1.Name = plane1Name;    
            aMain2.Name = plane2Name;
            aMain3.Name = plane3Name;

            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                // If this is not a supervisor then you have to setup only one ID
                if (!_isSupervisor)
                {
                    aMain1.Active = true;
                    aMain2.Active = false;
                    aMain3.Active = false;
                    Panel panel1 = Host.MainForm.Controls.Find("Panel1", true).FirstOrDefault() as Panel;
                    panel1.Size = new Size(panel1.Width, 47);
                }
                else
                {
                    aMain1.Active = true;
                    aMain2.Active = false;
                    aMain3.Active = false;

                    Panel panel1 = Host.MainForm.Controls.Find("Panel1", true).FirstOrDefault() as Panel;
                    int panelsize = 47 + ((aMain2.SysID != 0) ? 47 : 0) + (((aMain3.SysID != 0)) ? 47 : 0); 
                    panel1.Size = new Size(panel1.Width, panelsize);

                }
            }));

            // Send fleet setup data to the flight termination unit as well
            try
            {
                UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(_flightTermUDPAddr, _flightTermUDPPort);
                byte[] bytes = { 0xaa, 0x55, 0x00, 0x00, 0x00 };
                bytes[2] = (byte)plane1ID;
                bytes[3] = (byte)plane2ID;
                bytes[4] = (byte)plane3ID; 
                client.Send(bytes, bytes.Length, ip);
                client.Close();
            }
            catch
            {
                CustomMessageBox.Show("Unable to send fleet setup to the Flight Termination Unit. Check network connectivity!");
            }
        }

        private void TsStartSim_Click(object sender, EventArgs e)
        {
            MainV2.instance.Simulation.StartProtars(3);
        }

        private void Lc_nudgeSpeedClicked(object sender, EventArgs e)
        {
            if (Host.cs.mode.ToUpper() == "GUIDED")
            {
                PointLatLngAlt target = new PointLatLngAlt(Host.cs.TargetLocation);
                lc.state = LandState.GoToLand;
                Locationwp gotohere = new Locationwp();

                gotohere.id = (ushort)MAVLink.MAV_CMD.WAYPOINT;
                gotohere.alt = (float)Host.cs.TargetLocation.Alt - (float)Host.cs.HomeAlt; // back to m
                gotohere.lat = Host.cs.TargetLocation.Lat;
                gotohere.lng = Host.cs.TargetLocation.Lng;

                Host.comPort.setMode((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, "FBWA");

                try
                {
                    Host.comPort.setGuidedModeWP((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, gotohere);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Unable to switch back to Guided" + ex.Message, "ERROR");
                }
            }
        }

        private void Lc_abortLandingClicked(object sender, EventArgs e)
        {
            // Clean up markers
            try
            {
                landingOverlay.Markers.RemoveAt(3);
                landingOverlay.Markers.RemoveAt(3);
            }
            catch { }
        }

        private void Lc_setCruiseSpeedClicked(object sender, EventArgs e)
        {
            if (Host.cs.connected)
            {
                var speed = Host.comPort.MAV.param["TRIM_ARSPD_CM"].Value / 100;
                if (speed <= 0) return;
                try
                {
                    Host.comPort.doCommandAsync((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent,
                            MAVLink.MAV_CMD.DO_CHANGE_SPEED, 0, (float)speed, 0, 0, 0, 0, 0);
                }
                catch
                {
                    CustomMessageBox.Show("Unable to set speed", "Error");
                }
            }
        }

        public void sendUDPBroadcast(string message)
        {
            try
            {
                UdpClient client = new UdpClient();
                IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, _telemetryUdpPort);
                byte[] bytes = Encoding.ASCII.GetBytes(message);
                client.Send(bytes, bytes.Length, ip);
                client.Close();
            }
            catch { }
        }

        // Loop is called in regular intervalls (set by loopratehz)
        public override bool Loop()
        {
            //The most important thing is to do the landing update, this will run at 5hz
            doLanding();

            //If 500ms ellapsed to the processing
            if (((TimeSpan)(DateTime.Now - lastNonCriticalUpdate)).TotalMilliseconds > 500)
            {
                lastNonCriticalUpdate = DateTime.Now;

                updateNotifications();
                update_gauges();

                if (_isSupervisor) updateOverview();

                if (_isSupervisor) sendUDPBroadcast(sit.ToJSON());

                MainV2.instance.BeginInvoke((MethodInvoker)(() =>
                {
                    lc.updateLabels();
                }));

                #region MessagesBox

                // Message box update

                // Iterate over all ports and put every message to the common messages box

                foreach (var port in MainV2.Comports)
                {
                    //***
                    if (port.sysidcurrent == aMain1.SysID)
                    {
                        if (port.MAV.cs.messages.LastOrDefault().time != lastDisplayedMessage1)
                        {
                            fctb.BeginUpdate();
                            fctb.TextSource.CurrentTB = fctb;

                            port.MAV.cs.messages.ForEach(x =>
                             {
                                 if (x.Item1 > lastDisplayedMessage1)
                                 {

                                     TextStyle displayStyle;
                                     switch ((int)x.Item3)
                                     {
                                         case 0:
                                         case 1:
                                         case 2:
                                             {
                                                 displayStyle = errorStyle;
                                                 aMain1.setStatus("MSG", Stat.ALERT);
                                                 break;
                                             }
                                         case 3:
                                         case 4:
                                             {
                                                 displayStyle = warningStyle;
                                                 aMain1.setStatus("MSG", Stat.WARNING);
                                                 break;
                                             }
                                         default:
                                             {
                                                 displayStyle = infoStyle;
                                                 break;
                                             }
                                     }

                                     fctb.SelectionStart = 0;
                                     fctb.InsertText("(ID " + port.sysidcurrent + ") " + x.Item1 + " : (" + x.Item3 + ") " + x.Item2 + "\r\n", displayStyle);
                                 }
                             });
                            fctb.EndUpdate();
                            lastDisplayedMessage1 = port.MAV.cs.messages.LastOrDefault().time;
                        }
                    }
                    //---
                    //***
                    if (port.sysidcurrent == aMain2.SysID)
                    {
                        if (port.MAV.cs.messages.LastOrDefault().time != lastDisplayedMessage2)
                        {
                            fctb.BeginUpdate();
                            fctb.TextSource.CurrentTB = fctb;

                            port.MAV.cs.messages.ForEach(x =>
                            {
                                if (x.Item1 > lastDisplayedMessage2)
                                {

                                    TextStyle displayStyle;
                                    switch ((int)x.Item3)
                                    {
                                        case 0:
                                        case 1:
                                        case 2:
                                            {
                                                displayStyle = errorStyle;
                                                aMain2.setStatus("MSG", Stat.ALERT);
                                                break;
                                            }
                                        case 3:
                                        case 4:
                                            {
                                                displayStyle = warningStyle;
                                                aMain2.setStatus("MSG", Stat.WARNING);
                                                break;
                                            }
                                        default:
                                            {
                                                displayStyle = infoStyle;
                                                break;
                                            }
                                    }

                                    fctb.SelectionStart = 0;
                                    fctb.InsertText("(ID " + port.sysidcurrent + ") " + x.Item1 + " : (" + x.Item3 + ") " + x.Item2 + "\r\n", displayStyle);
                                }
                            });
                            fctb.EndUpdate();
                            lastDisplayedMessage2 = port.MAV.cs.messages.LastOrDefault().time;
                        }
                    }
                    //---
                    //***
                    if (port.sysidcurrent == aMain3.SysID)
                    {
                        if (port.MAV.cs.messages.LastOrDefault().time != lastDisplayedMessage3)
                        {
                            fctb.BeginUpdate();
                            fctb.TextSource.CurrentTB = fctb;

                            port.MAV.cs.messages.ForEach(x =>
                            {
                                if (x.Item1 > lastDisplayedMessage3)
                                {

                                    TextStyle displayStyle;
                                    switch ((int)x.Item3)
                                    {
                                        case 0:
                                        case 1:
                                        case 2:
                                            {
                                                displayStyle = errorStyle;
                                                aMain3.setStatus("MSG", Stat.ALERT);
                                                break;
                                            }
                                        case 3:
                                        case 4:
                                            {
                                                displayStyle = warningStyle;
                                                aMain3.setStatus("MSG", Stat.WARNING);
                                                break;
                                            }
                                        default:
                                            {
                                                displayStyle = infoStyle;
                                                break;
                                            }
                                    }

                                    fctb.SelectionStart = 0;
                                    fctb.InsertText("(ID " + port.sysidcurrent + ") " + x.Item1 + " : (" + x.Item3 + ") " + x.Item2 + "\r\n", displayStyle);
                                }
                            });
                            fctb.EndUpdate();
                            lastDisplayedMessage3 = port.MAV.cs.messages.LastOrDefault().time;
                        }
                    }
                    //---

                }

                #endregion

                #region GCSPower

                PowerStatus pwr = SystemInformation.PowerStatus;
                bp.setGcsVoltage(pwr.BatteryLifePercent, pwr.PowerLineStatus);
                
                #endregion
            }

            return true;	//Return value is not used
        }

        private void updateOverview()
        {
            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                foreach (var port in MainV2.Comports)
                {
                    if (port.sysidcurrent != 0)
                    {
                        if (port.sysidcurrent == aMain1.SysID)
                        {
                            ((Label)tlOw.GetControlFromPosition(1, 0)).Text = aMain1.SysID.ToString();
                            ((Label)tlOw.GetControlFromPosition(1, 1)).Text = aMain1.Name;
                            ((Label)tlOw.GetControlFromPosition(1, 2)).Text = port.MAV.cs.airspeed.ToString("F0") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(1, 3)).Text = port.MAV.cs.groundspeed.ToString("F0") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(1, 4)).Text = port.MAV.cs.alt.ToString("F0") + " m";
                            ((Label)tlOw.GetControlFromPosition(1, 5)).Text = port.MAV.cs.climbrate.ToString("F1") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(1, 6)).Text = port.MAV.cs.fuel_consumed.ToString("F1") + " L";
                            ((Label)tlOw.GetControlFromPosition(1, 7)).Text = port.MAV.cs.linkqualitygcs.ToString("F0") + " %";
                            ((Label)tlOw.GetControlFromPosition(1, 8)).Text = port.MAV.cs.DistToHome.ToString("F0") + " m";
                            ((Label)tlOw.GetControlFromPosition(1, 9)).Text = port.MAV.cs.eng_rpm.ToString("F0");
                            ((Label)tlOw.GetControlFromPosition(1, 10)).Text = port.MAV.cs.eng_egt.ToString("F0") + " C";
                            ((Label)tlOw.GetControlFromPosition(1, 11)).Text = port.MAV.cs.eng_throttle.ToString("F0") + " %";
                        }

                        if (port.sysidcurrent == aMain2.SysID)
                        {
                            ((Label)tlOw.GetControlFromPosition(2, 0)).Text = aMain2.SysID.ToString();
                            ((Label)tlOw.GetControlFromPosition(2, 1)).Text = aMain2.Name;
                            ((Label)tlOw.GetControlFromPosition(2, 2)).Text = port.MAV.cs.airspeed.ToString("F0") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(2, 3)).Text = port.MAV.cs.groundspeed.ToString("F0") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(2, 4)).Text = port.MAV.cs.alt.ToString("F0") + " m";
                            ((Label)tlOw.GetControlFromPosition(2, 5)).Text = port.MAV.cs.climbrate.ToString("F1") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(2, 6)).Text = port.MAV.cs.fuel_consumed.ToString("F1") + " L";
                            ((Label)tlOw.GetControlFromPosition(2, 7)).Text = port.MAV.cs.linkqualitygcs.ToString("F0") + " %";
                            ((Label)tlOw.GetControlFromPosition(2, 8)).Text = port.MAV.cs.DistToHome.ToString("F0") + " m";
                            ((Label)tlOw.GetControlFromPosition(2, 9)).Text = port.MAV.cs.eng_rpm.ToString("F0");
                            ((Label)tlOw.GetControlFromPosition(2, 10)).Text = port.MAV.cs.eng_egt.ToString("F0") + " C";
                            ((Label)tlOw.GetControlFromPosition(2, 11)).Text = port.MAV.cs.eng_throttle.ToString("F0") + " %";

                        }

                        if (port.sysidcurrent == aMain3.SysID)
                        {
                            ((Label)tlOw.GetControlFromPosition(3, 0)).Text = aMain3.SysID.ToString();
                            ((Label)tlOw.GetControlFromPosition(3, 1)).Text = aMain3.Name;
                            ((Label)tlOw.GetControlFromPosition(3, 2)).Text = port.MAV.cs.airspeed.ToString("F0") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(3, 3)).Text = port.MAV.cs.groundspeed.ToString("F0") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(3, 4)).Text = port.MAV.cs.alt.ToString("F0") + " m";
                            ((Label)tlOw.GetControlFromPosition(3, 5)).Text = port.MAV.cs.climbrate.ToString("F1") + " m/s";
                            ((Label)tlOw.GetControlFromPosition(3, 6)).Text = port.MAV.cs.fuel_consumed.ToString("F1") + " L";
                            ((Label)tlOw.GetControlFromPosition(3, 7)).Text = port.MAV.cs.linkqualitygcs.ToString("F0") + " %";
                            ((Label)tlOw.GetControlFromPosition(3, 8)).Text = port.MAV.cs.DistToHome.ToString("F0") + " m";
                            ((Label)tlOw.GetControlFromPosition(3, 9)).Text = port.MAV.cs.eng_rpm.ToString("F0");
                            ((Label)tlOw.GetControlFromPosition(3, 10)).Text = port.MAV.cs.eng_egt.ToString("F0") + " C";
                            ((Label)tlOw.GetControlFromPosition(3, 11)).Text = port.MAV.cs.eng_throttle.ToString("F0") + " %";
                        }
                    }
                }
            }));
        }

        // Exit called when plugin is terminated (usually when Mission Planner is exiting)
        public override bool Exit()
        {
            // Save position, we need to do it since formclose is not called in plugins
            annunciatorForm?.SaveStartupLocation();

            return true;	// Return value is not used
        }

        // This will handle the landing process
        private void doLanding()
        {
            if (!Host.cs.connected || !Host.cs.armed)
            {
                lc.state = LandState.None;
                return;
            }

            // Check status
            if (lc.state == LandState.None) return;

            if (lc.state == LandState.GoToWaiting)
            {
                // Wait until we started loitering around the waiting point (+50meter need to check for discrepancies between loiter radius and actual radius
                Console.WriteLine(Host.cs.Location.GetDistance(lc.WaitingPoint));
                if (Host.cs.Location.GetDistance(lc.WaitingPoint) <= Host.comPort.MAV.param["WP_LOITER_RAD"].Value + 50
                    && (Host.cs.alt <= lc.LandingAlt + 20))
                {
                    lc.state = LandState.WaitForSpeed;
                    try
                    {
                        Host.comPort.doCommandAsync((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent,
                                MAVLink.MAV_CMD.DO_CHANGE_SPEED, 0, (float)lc.HoldingSpeed, 0, 0, 0, 0, 0);
                    }
                    catch
                    {
                        lc.state = LandState.None;
                        CustomMessageBox.Show("Unable to set speed", "Error");
                    }
                }
            }

            if (lc.state == LandState.WaitForSpeed)
            {
                if (Host.cs.airspeed <= lc.HoldingSpeed + 3)
                {
                    lc.state = LandState.WaitForTangent;
                }
            }
               
            if (lc.state == LandState.WaitForTangent)
            {
                if (Math.Abs(lc.WindDirection - Host.cs.yaw) < 3)
                {
                    // Reduce the airspeed further during approach
                    try
                    {
                        Host.comPort.doCommandAsync((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent,
                                MAVLink.MAV_CMD.DO_CHANGE_SPEED, 0, (float)lc.ApproachSpeed, 0, 0, 0, 0, 0);
                    }
                    catch
                    {
                        lc.state = LandState.None;
                        CustomMessageBox.Show("Unable to set speed", "Error");
                    }

                    lc.state = LandState.GoToLand;

                    Locationwp gotohere = new Locationwp();
                    gotohere.id = (ushort)MAVLink.MAV_CMD.WAYPOINT;
                    gotohere.alt = (float)lc.LandingPoint.Alt; // back to m
                    gotohere.lat = (lc.LandingPoint.Lat);
                    gotohere.lng = (lc.LandingPoint.Lng);

                    try
                    {
                        Host.comPort.setGuidedModeWP((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, gotohere);
                        lc.state = LandState.GoToLand;
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show("Unable go to Landing point" + ex.Message, "ERROR");
                        lc.state = LandState.None;
                    }
                }
            }

            // Switch from landingpoint to target point if we are within loiter radius + 200meter
            if (lc.state == LandState.GoToLand)
            {
                if (Host.cs.Location.GetDistance(lc.LandingPoint) <= Host.comPort.MAV.param["WP_LOITER_RAD"].Value + lc.FinalApproachDistance)
                {
                    // Reduce the airspeed further during final approach
                    try
                    {
                        Host.comPort.doCommandAsync((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent,
                                MAVLink.MAV_CMD.DO_CHANGE_SPEED, 0, (float)lc.FinalApproachSpeed, 0, 0, 0, 0, 0);
                    }
                    catch
                    {
                        lc.state = LandState.None;
                        CustomMessageBox.Show("Unable to set speed", "Error");
                    }

                    Locationwp gotohere = new Locationwp();
                    gotohere.id = (ushort)MAVLink.MAV_CMD.WAYPOINT;
                    gotohere.alt = (float)lc.TargetPoint.Alt; // back to m
                    gotohere.lat = (lc.TargetPoint.Lat);
                    gotohere.lng = (lc.TargetPoint.Lng);

                    try
                    {
                        Host.comPort.setGuidedModeWP((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, gotohere);
                    }
                    catch { }

                    lc.state = LandState.CloseToLand;
                }
            }

            // Landing position calculation

            var speed = Host.cs.airspeed;
            PointLatLngAlt currentpos = new PointLatLngAlt(Host.cs.Location);
            PointLatLngAlt openpos1 = currentpos.newpos(Host.cs.yaw, speed * lc.OpeningTime);

            float wd = 0;

            if (Convert.ToBoolean(Host.config.GetBoolean("reverse_winddir_drag", true)))
            {
                wd = wrap360(lc.WindDirection - 180);
            }
            else
            {
                wd = lc.WindDirection;
            }

            PointLatLngAlt openpos2 = openpos1.newpos(wd, (Host.cs.g_wind_vel * (Host.cs.alt / lc.SinkRate) * lc.WindDrag));

            GMarkerGoogle p1 = new GMarkerGoogle(openpos1, GMarkerGoogleType.white_small);
            p1.Tag = "p1";
            GMarkerGoogle p2 = new GMarkerGoogle(openpos2, GMarkerGoogleType.yellow_small);
            p2.Tag = "p2";

            if (openpos2.GetDistance(lc.LandingPoint) <= 20 )
            {
                if (lc.chute == ChuteState.AutoOpenEnabled)
                {
                    Host.cs.messageHigh = "OPEN OPEN OPEN";
                    Host.comPort.doCommand((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, MAVLink.MAV_CMD.DO_SET_SERVO, chuteServo, chuteServoOpenPWM, 0, 0, 0, 0, 0);
                    SystemSounds.Exclamation.Play();
                }
                else
                {
                    Host.cs.messageHigh = "SIMULTED CHUTE OPEN";
                    SystemSounds.Exclamation.Play();
                }
            }

            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {
                try
                {
                    landingOverlay.Markers.RemoveAt(3);
                    landingOverlay.Markers.RemoveAt(3);
                }
                catch { }

                landingOverlay.Markers.Add(p1);
                landingOverlay.Markers.Add(p2);
                Host.FDGMapControl.Refresh();
            }));
        }

        private void Lc_setspeedClicked(object sender, EventArgs e)
        {
            try
            {
                Host.comPort.doCommandAsync((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent,
                        MAVLink.MAV_CMD.DO_CHANGE_SPEED, 0, (float)lc.HoldingSpeed, 0, 0, 0, 0, 0);
            }
            catch
            {
                CustomMessageBox.Show("Unable to set speed", "Error");
            }
        }

        private void Lc_startLandingClicked(object sender, EventArgs e)
        {
            // Check waiting point distance
            if (Host.cs.Location.GetDistance(lc.WaitingPoint) > 8000)
            {
                CustomMessageBox.Show("Waiting point is more tha 8Km away!", "ERROR");
                return;
            }
            // TODO: Check valid point

            // Reduce the airspeed further before reaching the turn
            try
            {
                Host.comPort.doCommandAsync((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent,
                        MAVLink.MAV_CMD.DO_CHANGE_SPEED, 0, (float)lc.HoldingSpeed, 0, 0, 0, 0, 0);
            }
            catch
            {
                lc.state = LandState.None;
                CustomMessageBox.Show("Unable to set speed", "Error");
            }

            // Send to waiting point in guided
            Locationwp gotohere = new Locationwp()
            {
                id = (ushort)MAVLink.MAV_CMD.WAYPOINT,
                alt = (float)lc.WaitingPoint.Alt, // back to m
                lat = (lc.WaitingPoint.Lat),
                lng = (lc.WaitingPoint.Lng)
            };
            try
            {
                Host.comPort.setGuidedModeWP((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, gotohere);
            }
            catch (Exception ex)
            {
                lc.state = LandState.None;
                CustomMessageBox.Show("Unable go to Waiting point" + ex.Message, "ERROR");
            }

            // Increment state
            lc.state = LandState.GoToWaiting;
        }

        private void ECtrl_emergencyClicked(object sender, EventArgs e)
        {
            Host.comPort.doCommand((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, MAVLink.MAV_CMD.DO_SET_SERVO, (float)Convert.ToInt16(_jetControlChannel), 1000, 0, 0, 0, 0, 0, false);
        }

        private void ECtrl_stopClicked(object sender, EventArgs e)
        {
            Host.comPort.doCommand((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, MAVLink.MAV_CMD.DO_SET_SERVO, (float)Convert.ToInt16(_jetControlChannel), 1500, 0, 0, 0, 0, 0, false);
        }

        private void ECtrl_startClicked(object sender, EventArgs e)
        {
            Host.comPort.doCommand((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, MAVLink.MAV_CMD.DO_SET_SERVO, (float)Convert.ToInt16(_jetControlChannel), 2000, 0, 0, 0, 0, 0, false);
        }

        private void ECtrl_armClicked(object sender, EventArgs e)
        {
            if (!Host.cs.connected)
                return;

            // arm the MAV
            try
            {
                var isitarmed = Host.comPort.MAV.cs.armed;
                var action = Host.comPort.MAV.cs.armed ? "Disarm" : "Arm";

                if (isitarmed)
                    if (CustomMessageBox.Show("Are you sure you want to " + action, action,
                            CustomMessageBox.MessageBoxButtons.YesNo) !=
                        CustomMessageBox.DialogResult.Yes)
                        return;
                StringBuilder sb = new StringBuilder();
                var sub = Host.comPort.SubscribeToPacketType(MAVLink.MAVLINK_MSG_ID.STATUSTEXT, message =>
                {
                    sb.AppendLine(Encoding.ASCII.GetString(((MAVLink.mavlink_statustext_t)message.data).text)
                        .TrimEnd('\0'));
                    return true;
                }, (byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent);
                bool ans = Host.comPort.doARM((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, !isitarmed);
                Host.comPort.UnSubscribeToPacketType(sub);
                if (ans == false)
                {
                    if (CustomMessageBox.Show(
                            action + " failed.\n" + sb.ToString() + "\nForce " + action +
                            " can bypass safety checks,\nwhich can lead to the vehicle crashing\nand causing serious injuries.\n\nDo you wish to Force " +
                            action + "?", "Error", CustomMessageBox.MessageBoxButtons.YesNo,
                            CustomMessageBox.MessageBoxIcon.Exclamation, "Force " + action, "Cancel") ==
                        CustomMessageBox.DialogResult.Yes)
                    {
                        ans = Host.comPort.doARM((byte)Host.comPort.sysidcurrent, (byte)Host.comPort.compidcurrent, !isitarmed, true);
                        if (ans == false)
                        {
                            CustomMessageBox.Show("ARM request rejected by MAV", "Error");
                        }
                    }
                }
            }
            catch
            {
                CustomMessageBox.Show("No response for the ARM command", "Error");
            }
        }

        private void PlControl_igniteClicked(object sender, EventArgs e)
        {

            byte[] c = Encoding.Default.GetBytes("P");
            Host.comPort.sendPacket(new MAVLink.mavlink_named_value_float_t() { name = c, time_boot_ms = 0, value = (float)plControl.igniteMask }, Host.comPort.sysidcurrent, Host.comPort.compidcurrent);

            Console.WriteLine("Payload ignite:{0}", plControl.igniteMask);

        }

        // Set landing point
        private void TsLandingPoint_Click(object sender, EventArgs e)
        {
            // Check wind velocity and if it is below 4 m/s then ask for direction
            var winddir = Host.cs.g_wind_dir;
            string winddir_input = winddir.ToString();

            //if (Host.cs.g_wind_vel < 4)
            //{
            //    InputBox.Show("Low windspeed detected", "You can enter wind direction manually or accept meassured value", ref winddir_input);
            //    try
            //    {
            //        winddir = float.Parse(winddir_input);
            //    }
            //    catch { }

            //    // Sanity checks 
            //    if (winddir < 0 || winddir > 359) winddir = Host.cs.g_wind_dir;
            //}

            TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["tabWeather"];
            if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;

            CustomMessageBox.Show("Check Weather station wind direction and strength before landing!");

            float landReverse = Host.config.GetFloat("LandDirectionReverse", 0);  // Load or default
            Host.config["LandDirectionReverse"] = landReverse.ToString();         // Save
   
            PointLatLngAlt lp = Host.FDMenuMapPosition;

            if (Host.comPort.BaseStream != null && Host.comPort.BaseStream.IsOpen)
                lc.updateLandingData(lp, wrap360(winddir - landReverse), Host.cs.g_wind_vel, (int)Host.comPort.MAV.param["WP_LOITER_RAD"].Value);
            else
            {
                CustomMessageBox.Show("Not connected - landing point is not valid");
            }

            landingOverlay.Markers.Clear();
            landingOverlay.Routes.Clear();

            markerWaiting = new GMarkerGoogle(lc.WaitingPoint, GMarkerGoogleType.lightblue_dot);
            markerWaiting.ToolTipText = "Waiting Point";
            markerLanding = new GMarkerGoogle(lc.LandingPoint, GMarkerGoogleType.green_dot);
            markerLanding.ToolTipText = "Landing Point";
            markerTarget = new GMarkerGoogle(lc.TargetPoint, GMarkerGoogleType.pink_dot);
            markerTarget.ToolTipText = "FlyOver";

            landingRoute = new GMapRoute("landingline");
            landingRoute.Points.Add(lc.WaitingPointTangent);
            landingRoute.Points.Add(lc.LandingPoint);
            landingRoute.Points.Add(lc.TargetPoint);

            landingOverlay.Markers.Add(markerWaiting);
            landingOverlay.Markers.Add(markerLanding);
            landingOverlay.Markers.Add(markerTarget);
            landingOverlay.Routes.Add(landingRoute);

            Host.FDGMapControl.Overlays.Add(landingOverlay);
            Host.FDGMapControl.Position = Host.FDGMapControl.Position;

            lc.LandingPoint.Tag = "LP";
            lc.WaitingPoint.Tag = "WP";
        }

        private void Pitot_calibrateClicked(object sender, EventArgs e)
        {
            if (!Host.comPort.MAV.cs.connected)
            {
                CustomMessageBox.Show("You have to connect first!", "Action", MessageBoxButtons.OK);
                return;
            }

            if (Host.comPort.MAV.cs.armed)
            {
                CustomMessageBox.Show("You cannot do it while aircraft is armed!", "Action", MessageBoxButtons.OK);
                return;
            }

            if (CustomMessageBox.Show("Are you sure you want to do Preflight Calibration ?", "Action", MessageBoxButtons.YesNo) == (int)DialogResult.Yes)
            {
                try
                {
                    ((Control)sender).Enabled = false;

                    int param1 = 0;
                    int param2 = 0;
                    int param3 = 1;

                    // baro/airspeed calibration, but no gyro !
                    param3 = 1; // baro / airspeed
                    var cmd = (MAVLink.MAV_CMD)Enum.Parse(typeof(MAVLink.MAV_CMD), "Preflight_Calibration".ToUpper());

                    if (!Host.comPort.doCommand(cmd, param1, param2, param3, 0, 0, 0, 0))
                    {
                        CustomMessageBox.Show("Calibration Failed" + cmd, "ERROR");
                    }
                }
                catch
                {
                    CustomMessageBox.Show("Calibration failed", "ERROR");
                }

                ((Control)sender).Enabled = true;
            }
        }

        private void PlControl_setupClicked(object sender, EventArgs e)
        {
            fPs = new payloadSetupForm();
            fPs.Show();
            fPs.FormClosing += FPs_FormClosing;
            fPs.updateAll(plControl.payloads);
            plControl.setupEnabled(false);
        }

        private void FPs_FormClosing(object sender, FormClosingEventArgs e)
        {
            plControl.setupEnabled(true);
            plControl.updateAll(fPs.payloadStatus);
            plControl.redrawControls();
        }

        // This updates the gauge data based on the currently selected mavlink datastream;
        public void update_gauges()
        {
            eCtrl.setRpmEgt(Host.cs.eng_rpm, Host.cs.eng_egt);
            eCtrl.setThrFuel(Host.cs.eng_throttle, Host.cs.fuel_flow, Host.cs.fuel_consumed);
            eCtrl.setStatus((byte)Host.cs.eng_status, (byte)Host.cs.eng_error);
            fuel.setData(Host.cs.fuel_level_raw,Host.cs.fuel_flow, Host.cs.fuel_consumed, Host.cs.fuel_loaded);
            pitot.setData(Host.cs.pitot_temp, Host.cs.ambient_temp, Host.cs.pitot_target_temp, Host.cs.pitot_heat_duty);
            bp.setServo1Voltage(Host.cs.bat_servo1);
            bp.setServo2Voltage(Host.cs.bat_servo2);
            bp.setMainVoltage(Host.cs.bat_main);
            bp.setPayloadVoltage(Host.cs.bat_payload);
            plControl.setSafetyStatus(Host.cs.safety_switch);
        }

        // This updates all annunciator panels, based on connected vehicles. (Assuming cs is updated)
        public void updateNotifications()
        {
            foreach (var port in MainV2.Comports)
            {
                if (port.sysidcurrent != 0)
                {
                    if (port.sysidcurrent == aMain1.SysID)
                    {
                        Stat engineStat = Stat.NOMINAL;
                        Stat fuelStat = Stat.NOMINAL;
                        if (port.MAV.cs.eng_rpm < 27000) engineStat = Stat.ALERT;
                        if (port.MAV.cs.eng_egt > 900) engineStat = Stat.ALERT;


                        if (port.MAV.cs.fuel_loaded - port.MAV.cs.fuel_consumed < 8) fuelStat = Stat.WARNING;
                        if (port.MAV.cs.fuel_loaded - port.MAV.cs.fuel_consumed < 5) fuelStat = Stat.ALERT;

                        aMain1.setStatus("ENGINE", engineStat);
                        aMain1.setStatus("FUEL", fuelStat);

                        var lq = port.MAV.cs.linkqualitygcs;
                        if (lq >= 95) aMain1.setStatus("COMMS", Stat.NOMINAL);
                        else if (lq < 95 && lq > 70) aMain1.setStatus("COMMS", Stat.WARNING);
                        else aMain1.setStatus("COMMS", Stat.ALERT);
                        switch (getBatteryStatus(port.MAV.cs))
                        {
                            case 0:
                                aMain1.setStatus("BATT", Stat.NOMINAL);
                                break;
                            case 1:
                                aMain1.setStatus("BATT", Stat.WARNING);
                                break;
                            case 2:
                                aMain1.setStatus("BATT", Stat.ALERT);
                                break;
                            default:
                                break;
                        }

                        sit.sysid1 = port.sysidcurrent;
                        sit.pos1 = new PointLatLngAlt(port.MAV.cs.Location.Lat,port.MAV.cs.Location.Lng,port.MAV.cs.alt);
                        sit.heading1 = port.MAV.cs.yaw;
                        sit.airspeed1 = port.MAV.cs.airspeed;
                    }

                    if (port.sysidcurrent == aMain2.SysID)
                    {
                        Stat engineStat = Stat.NOMINAL;
                        Stat fuelStat = Stat.NOMINAL;
                        if (port.MAV.cs.eng_rpm < 27000) engineStat = Stat.ALERT;
                        if (port.MAV.cs.eng_egt > 900) engineStat = Stat.ALERT;

                        if (port.MAV.cs.fuel_loaded - port.MAV.cs.fuel_consumed < 8) fuelStat = Stat.WARNING;
                        if (port.MAV.cs.fuel_loaded - port.MAV.cs.fuel_consumed < 5) fuelStat = Stat.ALERT;

                        aMain2.setStatus("ENGINE", engineStat);
                        aMain2.setStatus("FUEL", fuelStat);
                        var lq = port.MAV.cs.linkqualitygcs;
                        if (lq >= 95) aMain2.setStatus("COMMS", Stat.NOMINAL);
                        else if (lq < 95 && lq > 70) aMain2.setStatus("COMMS", Stat.WARNING);
                        else aMain2.setStatus("COMMS", Stat.ALERT);
                        switch (getBatteryStatus(port.MAV.cs))
                        {
                            case 0:
                                aMain2.setStatus("BATT", Stat.NOMINAL);
                                break;
                            case 1:
                                aMain2.setStatus("BATT", Stat.WARNING);
                                break;
                            case 2:
                                aMain2.setStatus("BATT", Stat.ALERT);
                                break;
                            default:
                                break;
                        }
                        sit.sysid2 = port.sysidcurrent;
                        sit.pos2 = new PointLatLngAlt(port.MAV.cs.Location.Lat, port.MAV.cs.Location.Lng, port.MAV.cs.alt);
                        sit.heading2 = port.MAV.cs.yaw;
                        sit.airspeed2 = port.MAV.cs.airspeed;
                    }

                    if (port.sysidcurrent == aMain3.SysID)
                    {
                        Stat engineStat = Stat.NOMINAL;
                        Stat fuelStat = Stat.NOMINAL;
                        if (port.MAV.cs.eng_rpm < 27000) engineStat = Stat.ALERT;
                        if (port.MAV.cs.eng_egt > 900) engineStat = Stat.ALERT;

                        if (port.MAV.cs.fuel_loaded - port.MAV.cs.fuel_consumed < 8) fuelStat = Stat.WARNING;
                        if (port.MAV.cs.fuel_loaded - port.MAV.cs.fuel_consumed < 5) fuelStat = Stat.ALERT;

                        aMain3.setStatus("ENGINE", engineStat);
                        aMain3.setStatus("FUEL", fuelStat);
                        var lq = port.MAV.cs.linkqualitygcs;
                        if (lq >= 95) aMain3.setStatus("COMMS", Stat.NOMINAL);
                        else if (lq < 95 && lq > 70) aMain3.setStatus("COMMS", Stat.WARNING);
                        else aMain3.setStatus("COMMS", Stat.ALERT);
                        switch (getBatteryStatus(port.MAV.cs))
                        {
                            case 0:
                                aMain3.setStatus("BATT", Stat.NOMINAL);
                                break;
                            case 1:
                                aMain3.setStatus("BATT", Stat.WARNING);
                                break;
                            case 2:
                                aMain3.setStatus("BATT", Stat.ALERT);
                                break;
                            default:
                                break;
                        }
                        sit.sysid3 = port.sysidcurrent;
                        sit.pos3 = new PointLatLngAlt(port.MAV.cs.Location.Lat, port.MAV.cs.Location.Lng, port.MAV.cs.alt);
                        sit.heading3 = port.MAV.cs.yaw;
                        sit.airspeed3 = port.MAV.cs.airspeed;
                    }
                }
            }
        }

        public byte getBatteryStatus(CurrentState cs)
        {
            byte retval = 0;    // NOMINAL

            switch (cs.bat_servo1)
            {
                case float f when f <= 6:
                    retval = 2; // ALERT
                    break;
                case float f when f > 6 && f <= 6.8:
                    retval = 1; // WARNING
                    break;
                case float f when f > 8.6:
                    retval = 2; // ALERT
                    break;
                default:
                    break;
            }

            if (retval == 2) return retval;

            switch (cs.bat_servo2)
            {
                case float f when f <= 6:
                    retval = 2; // ALERT
                    break;
                case float f when f > 6 && f <= 6.8:
                    retval = 1; // WARNING
                    break;
                case float f when f > 8.6:
                    retval = 2; // ALERT
                    break;
                default:
                    break;
            }

            if (retval == 2) return retval;

            switch (cs.bat_payload)
            {
                case float f when f <= 9:
                    retval = 2; // ALERT
                    break;
                case float f when f > 9 && f <= 10.2:
                    retval = 1; // WARNING
                    break;
                case float f when f > 12.8:
                    retval = 2; // ALERT
                    break;
                default:
                    break;
            }

            if (retval == 2) return retval;

            switch (cs.bat_main)
            {
                case float f when f <= 12:
                    retval = 2; // ALERT
                    break;
                case float f when f > 12 && f <= 13.6:
                    retval = 1; // WARNING
                    break;
                case float f when f > 17:
                    retval = 2; // ALERT
                    break;
                default:
                    break;
            }

            if (retval == 2) return retval;

            PowerStatus pwr = SystemInformation.PowerStatus;
            switch (pwr.BatteryLifePercent)
            {
                case float f when f <= 0.1:
                    retval = 2; // ALERT
                    break;
                case float f when f > 0.1 && f <= 0.2:
                    retval = 1; // WARNING
                    break;
                default:
                    break;
            }
            return retval;
        }

        private void annunciator1_buttonClicked(object sender, EventArgs e)
        {
            // Check if we are in the right vehicle, if not then switch vehicle
            var s = (annunciator)sender;

                if (s.SysID != 0)
                {
                    MainV2.instance.FlightPlanner.updateCurrentWpTable();
                    MainV2.instance.FlightPlanner.updatealltime();
                    // If SysID os zero, then we does not have to update anything.
                    if (s.SysID != Host.comPort.sysidcurrent)
                    {
                        foreach (var port in MainV2.Comports)
                        {
                            if (port.sysidcurrent == s.SysID)
                            {
                                MainV2.comPort = port;
                                MainV2.comPort.sysidcurrent = s.SysID;
                                MainV2.comPort.compidcurrent = 1; // Always do the vehicle  
                                MainV2.instance.FlightPlanner.UpdateVehicleMissionOnScreen(s.SysID);  // Notify FlightPlanner that we changed vehicle
                                MainV2.View.Reload();
                            }

                            aMain1.Active = false;
                            aMain2.Active = false;
                            aMain3.Active = false;
                            s.Active = true;
                        }
                    }
                }
            // We also assume that all three controls are named and sorted on the same way, so no need to differentiate
            switch (s.clickedButtonName)
            {
                case "FD":
                    MainV2.instance.MyView.ShowScreen("FlightData");
                    break;
                
                case "FP":
                    if (!MainV2.instance.FlightData.GetDropoutState("FlightPlanner")) MainV2.instance.MyView.ShowScreen("FlightPlanner");
                    MainV2.instance.FlightPlanner.cmb_missiontype.SelectedIndex = 0;
                    break;
                
                case "GF":
                    if (!MainV2.instance.FlightData.GetDropoutState("FlightPlanner")) MainV2.instance.MyView.ShowScreen("FlightPlanner");
                    MainV2.instance.FlightPlanner.cmb_missiontype.SelectedIndex = 1;
                    break;
                
                case "SETUP":
                    MainV2.instance.MyView.ShowScreen("HWConfig");
                    actualPanel = "SETUP";
                    break;

                case "TUNING":
                    MainV2.instance.MyView.ShowScreen("SWConfig");
                    actualPanel = "TUNING";
                    break;

                case "PAYLD":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["payLoadCTRTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "ENGINE":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["engCtrlTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "COMMS":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["commsTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "MSG":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["colorMsgTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        aMain1.setStatus("MSG", Stat.NOMINAL);
                        aMain2.setStatus("MSG", Stat.NOMINAL);
                        aMain3.setStatus("MSG", Stat.NOMINAL);
                        break;
                    }

                case "BATT":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["battTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "DATA":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["tabQuick"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "ACTIONS":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["tabActions"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "EKF":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["ekfTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "PITOT":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["pitotTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "FUEL":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["fuelTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "PREFLGHT":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["tabPagePreFlight"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "WEATHER":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["tabWeather"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                case "LAND":
                    {
                        TabPage tobeSelected = Host.MainForm.FlightData.tabControlactions.TabPages["landingTab"];
                        if (tobeSelected != null) Host.MainForm.FlightData.tabControlactions.SelectedTab = tobeSelected;
                        break;
                    }

                default: break;
            }
        }

        private void annunciator1_undock(object sender, EventArgs e)
        {
            annunciatorForm = new FloatingForm();
            annunciatorForm.Text = "Annunciator";
            annunciatorForm.RestoreStartupLocation();
            MainV2.instance.panel1.Controls.Remove(aMain1);
            aMain1.isSingleLine = false;
            aMain1.Dock = DockStyle.None;
            aMain1.Visible = true;
            aMain1.contextMenuEnabled = false;
            aMain1.Size = annunciatorForm.Size;
            //annunciator1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            annunciatorForm.Controls.Add(aMain1);
            annunciatorForm.FormClosing += annunciator_FormClosing;
            //annunciatorUndocked = true;
            MainV2.instance.panel1.Dock = DockStyle.Top;
            MainV2.instance.panel1.Visible = false;
            aMain1.Invalidate();
            Host.config["aMainDocked"] = false.ToString();
            Host.config.Save();
            annunciatorForm.Show();
        }

        void annunciator_FormClosing(object sender, FormClosingEventArgs e)
        {
            (sender as Form).SaveStartupLocation();
            aMain1.isSingleLine = true;
            aMain1.contextMenuEnabled = true;
            MainV2.instance.panel1.Controls.Add(aMain1);
            MainV2.instance.panel1.Dock = DockStyle.Top;
            MainV2.instance.panel1.Visible = true;
            aMain1.Size = MainV2.instance.panel1.Size;
            //annunciatorUndocked = false;
            Host.config["aMainDocked"] = true.ToString();
            Host.config.Save();
            (sender as Form).Dispose();
        }

        //************** FCTB

        void setupFCTB()
        {
            // 
            // fctb
            // 
        //    this.fctb.AutoCompleteBracketsList = new char[] {
        //'(',
        //')',
        //'{',
        //'}',
        //'[',
        //']',
        //'\"',
        //'\"',
        //'\'',
        //'\''};
            this.fctb.AutoScrollMinSize = new System.Drawing.Size(25, 15);
            this.fctb.BackBrush = null;
            this.fctb.CharHeight = 15;
            this.fctb.CharWidth = 7;
            this.fctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fctb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fctb.Font = new System.Drawing.Font("Consolas", 11F);
            this.fctb.IsReplaceMode = false;
            this.fctb.Location = new System.Drawing.Point(0, 0);
            this.fctb.Name = "fctb";
            this.fctb.Paddings = new System.Windows.Forms.Padding(1);
            this.fctb.ReadOnly = true;
            this.fctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fctb.TabIndex = 5;
            this.fctb.Zoom = 100;
            this.fctb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.fctb.ShowLineNumbers = false;
            this.fctb.BackColor = Color.Black;
            this.fctb.ShowScrollBars = true;
            this.fctb.BeginUpdate();
            this.fctb.TextSource.CurrentTB = fctb;
            this.fctb.AppendText("Messages starting", infoStyle);
            this.fctb.EndUpdate();
        }

        float wrap360(float noin)
        {
            if (noin < 0)
                return noin + 360;
            return noin;
        }

        #region TimedWPConsole

        //Controls
        private System.Windows.Forms.Button bExecute;
        private System.Windows.Forms.Panel twpPanel;
        private System.Windows.Forms.DataGridView dgV;
        private System.Windows.Forms.Label lTarget;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lActualTime;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.DataGridViewTextBoxColumn WP;
        private System.Windows.Forms.DataGridViewTextBoxColumn Time1;
        private System.Windows.Forms.Button bRefresh;
        private System.Windows.Forms.Label lRemaining;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtWP;

        private void initTWPConsole()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.bExecute = new System.Windows.Forms.Button();
            this.twpPanel = new System.Windows.Forms.Panel();
            this.bRefresh = new System.Windows.Forms.Button();
            this.lRemaining = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lTarget = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lActualTime = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.dgV = new System.Windows.Forms.DataGridView();
            this.WP = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Time1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timer1 = new System.Windows.Forms.Timer();
            this.txtWP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.twpPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgV)).BeginInit();
            // 
            // bExecute
            // 
            this.bExecute.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bExecute.Location = new System.Drawing.Point(198, 237);
            this.bExecute.Name = "bExecute";
            this.bExecute.Size = new System.Drawing.Size(124, 46);
            this.bExecute.TabIndex = 1;
            this.bExecute.Text = "EXECUTE";
            this.bExecute.UseVisualStyleBackColor = true;
            this.bExecute.Click += BExecute_Click;
            // 
            // panel1
            // 
            this.twpPanel.Controls.Add(this.label1);
            this.twpPanel.Controls.Add(this.txtWP);
            this.twpPanel.Controls.Add(this.bRefresh);
            this.twpPanel.Controls.Add(this.lRemaining);
            this.twpPanel.Controls.Add(this.label4);
            this.twpPanel.Controls.Add(this.bExecute);
            this.twpPanel.Controls.Add(this.lTarget);
            this.twpPanel.Controls.Add(this.label2);
            this.twpPanel.Controls.Add(this.lActualTime);
            this.twpPanel.Controls.Add(this.dateTimePicker1);
            this.twpPanel.Controls.Add(this.dgV);
            this.twpPanel.Location = new System.Drawing.Point(12, 12);
            this.twpPanel.Name = "panel1";
            this.twpPanel.Size = new System.Drawing.Size(397, 417);
            this.twpPanel.TabIndex = 2;
            // 
            // bRefresh
            // 
            this.bRefresh.Location = new System.Drawing.Point(3, 289);
            this.bRefresh.Name = "bExit";
            this.bRefresh.Size = new System.Drawing.Size(150, 30);
            this.bRefresh.TabIndex = 3;
            this.bRefresh.Text = "Refresh Data";
            this.bRefresh.UseVisualStyleBackColor = true;
            this.bRefresh.Click += BRefresh_Click;
            // 
            // lRemaining
            // 
            this.lRemaining.AutoSize = true;
            this.lRemaining.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lRemaining.Location = new System.Drawing.Point(198, 136);
            this.lRemaining.Name = "lRemaining";
            this.lRemaining.Size = new System.Drawing.Size(88, 24);
            this.lRemaining.TabIndex = 6;
            this.lRemaining.Text = "00:00:00";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(199, 123);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(133, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Remaining time till Execute";
            // 
            // lTarget
            // 
            this.lTarget.AutoSize = true;
            this.lTarget.Location = new System.Drawing.Point(199, 62);
            this.lTarget.Name = "lTarget";
            this.lTarget.Size = new System.Drawing.Size(98, 13);
            this.lTarget.TabIndex = 4;
            this.lTarget.Text = "WP ## Target time";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(199, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Actual Time";
            // 
            // lActualTime
            // 
            this.lActualTime.AutoSize = true;
            this.lActualTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lActualTime.Location = new System.Drawing.Point(198, 27);
            this.lActualTime.Name = "lActualTime";
            this.lActualTime.Size = new System.Drawing.Size(88, 24);
            this.lActualTime.TabIndex = 2;
            this.lActualTime.Text = "00:00:00";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.CalendarFont = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dateTimePicker1.CustomFormat = "HH:mm:ss";
            this.dateTimePicker1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker1.Location = new System.Drawing.Point(198, 78);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(99, 29);
            this.dateTimePicker1.TabIndex = 1;
            // 
            // dgV
            // 
            this.dgV.AllowUserToAddRows = false;
            this.dgV.AllowUserToDeleteRows = false;
            this.dgV.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgV.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.WP,
            this.Time1});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgV.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgV.Location = new System.Drawing.Point(3, 3);
            this.dgV.Name = "dgV";
            this.dgV.ReadOnly = true;
            this.dgV.Size = new System.Drawing.Size(180, 280);
            this.dgV.TabIndex = 0;
            //this.dgV.SelectionChanged += new System.EventHandler(this.dgV_SelectionChanged);
            this.dgV.CellClick += dgV_SelectionChanged;
            // 
            // WP
            // 
            this.WP.HeaderText = "WP#";
            this.WP.Name = "WP";
            this.WP.ReadOnly = true;
            this.WP.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.WP.Width = 50;
            // 
            // Time1
            // 
            this.Time1.HeaderText = "Total Time";
            this.Time1.Name = "Time1";
            this.Time1.ReadOnly = true;
            this.Time1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Time1.Width = 50;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // txtWP
            // 
            this.txtWP.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtWP.Location = new System.Drawing.Point(198, 187);
            this.txtWP.Name = "txtWP";
            this.txtWP.Size = new System.Drawing.Size(52, 29);
            this.txtWP.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(199, 171);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "WP to Jump";
            // 
            // TimedWpConsole
            // 
            this.twpPanel.ResumeLayout(false);
            this.twpPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgV)).EndInit();
        }

        //Start timed waypoint flight 
        //You have to be in Auto. This will change the next waypoint to the one in the TXT_WP textbox
        //Which is populated from the waypoint list

        private void BExecute_Click(object sender, EventArgs e)
        {
            foreach (var port in MainV2.Comports)
            {
                if ((port.sysidcurrent == aMain1.SysID) || (port.sysidcurrent == aMain2.SysID) || (port.sysidcurrent == aMain3.SysID))
                {
                    try
                    {
                        port.setWPCurrent(port.MAV.sysid, port.MAV.compid, (ushort)Int16.Parse(txtWP.Text)); // set nav to
                        Console.WriteLine($"Set WP {Int16.Parse(txtWP.Text)} at system {port.MAV.sysid}");
                    }
                    catch
                    {
                        Console.WriteLine($"Unable to set waypoint on {port.MAV.sysid}");
                    }
                }
            }
        }

        private void BRefresh_Click(object sender, EventArgs e)
        {
            dgV.Rows.Clear();
            //Get the index of the first vehicle!
            var idx = MainV2.instance.FlightPlanner.getSysIdIndex(aMain1.SysID);
            int totaltime = 0;

            for (int i = 0; i < MainV2.instance.FlightPlanner.wplists[idx].Count; i++)
            {
                if (i > 0)
                {
                    Locationwp item = MainV2.instance.FlightPlanner.wplists[idx][i];
                    if (item.id == (ushort)MAVLink.MAV_CMD.DO_SEND_SCRIPT_MESSAGE)
                    {
                        totaltime += (int)item.p2;
                        twpAddRow(i, totaltime);
                    }
                }
            }
        }

        public int selectedWaypoint = 0;
        public int routetime = 0;

        public void twpAddRow(int wp, int time)
        {
            var r = dgV.Rows.Add();
            dgV.Rows[r].Cells[WP.Index].Value = (wp + 1).ToString();
            dgV.Rows[r].Cells[Time1.Index].Value = time.ToString();

            txtWP.Text = dgV.Rows[0].Cells[WP.Index].Value.ToString();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime time = DateTime.Now;
            TimeSpan route = TimeSpan.FromSeconds(routetime);
            lActualTime.Text = time.ToString("HH:mm:ss");
            TimeSpan remaining = dateTimePicker1.Value.Subtract(time) - route;
            lRemaining.Text = $"{remaining.Hours.ToString("00")}:{remaining.Minutes.ToString("00")}:{remaining.Seconds.ToString("00")}";
        }

        private void dgV_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                selectedWaypoint = Int16.Parse((string)dgV.CurrentRow.Cells[WP.Index].Value.ToString());
                routetime = Int16.Parse((string)dgV.CurrentRow.Cells[Time1.Index].Value.ToString());
                lTarget.Text = $"WP {selectedWaypoint} reach time";
            }
            catch { }
        }

        #endregion
    }
}
