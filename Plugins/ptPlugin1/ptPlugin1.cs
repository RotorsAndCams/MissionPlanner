﻿using MissionPlanner;
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


//By Bandi
namespace ptPlugin1
{
    [PreventTheming]
    public class ptPlugin1 : Plugin
    {

        annunciator aMain = new annunciator(19, new Size(130,40));
        public static FloatingForm annunciatorForm = new FloatingForm();



        public TabPage payloadControlpage = new TabPage();
        public payloadcontrol plControl = new payloadcontrol();
        public List<Payload> payloadSettings = new List<Payload>();
        payloadSetupForm fPs;



        public TabPage engineControlPage = new TabPage();
        public engineControl eCtrl = new engineControl();


        public TabPage colorMessagePage = new TabPage();
        public FastColoredTextBox fctb;
        public DateTime lastDisplayedMessage = DateTime.MinValue;

        

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


        public ToolStripMenuItem tsLandingPoint = new ToolStripMenuItem();


        string actualPanel = "";



        //DEV TEST AREA

        internal GMapMarker markerLanding;
        internal GMapMarker markerWaiting;
        internal GMapMarker markerTarget;

        internal GMapRoute landingRoute;
        internal static GMapOverlay landingOverlay;
        //**************



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
            get { return "Schaffer Andras"; }
        }

        //[DebuggerHidden]
        public override bool Init()
		//Init called when the plugin dll is loaded
        {
            loopratehz = 2;  //Loop runs every second (The value is in Hertz, so 2 means every 500ms, 0.1f means every 10 second...) 

            return true;	 // If it is false then plugin will not load
        }

        public override bool Loaded()
		//Loaded called after the plugin dll successfully loaded
        {

            tsLandingPoint.Text = "Set Landing Point";
            tsLandingPoint.Click += TsLandingPoint_Click;
            Host.FDMenuMap.Items.Add(tsLandingPoint);
            landingOverlay = new GMapOverlay("landing");



            Panel panel1 = Host.MainForm.Controls.Find("Panel1", true).FirstOrDefault() as Panel;


            string[] btnLabels = new string[] { 
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
            aMain.setPanels(btnNames, btnLabels);

            aMain.Enabled = true;
            aMain.Location = new Point(0, 0);
            aMain.Size = new Size(panel1.Width, 47);
            aMain.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
            aMain.undock += annunciator1_undock;
            aMain.buttonClicked += annunciator1_buttonClicked;

            aMain.Visible = true;
            aMain.Refresh();

            MainV2.instance.BeginInvoke((MethodInvoker)(() =>
            {

                MenuStrip MainMenu = Host.MainForm.Controls.Find("MainMenu", true).FirstOrDefault() as MenuStrip;
                if (MainMenu != null) MainMenu.Visible = false;

            }));

            //Check undocked status
            if (Settings.Instance["aMainDocked"] != null)
            {
                bool aMainDocked = Settings.Instance.GetBoolean("aMainDocked");
                if (!aMainDocked)
                {
                    annunciator1_undock(this, new EventArgs());
                }
                else
                {
                    panel1.Controls.Add(aMain);

                }
            }
            else
            {
                panel1.Controls.Add(aMain);
            }


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
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(fuelPage);


            landingPage.Text = "Landing";
            landingPage.Name = "landingTab";
            landingPage.Controls.Add(lc);
            lc.Size = landingPage.ClientSize;
            lc.Location = new Point(0, 0);
            lc.Dock = DockStyle.Fill;
            Host.MainForm.FlightData.tabControlactions.TabPages.Add(landingPage);




            //Setup mavlink receiving
            Host.comPort.OnPacketReceived += MavOnOnPacketReceivedHandler;
            return true;     //If it is false plugin will not start (loop will not called)
        }

        private void ECtrl_emergencyClicked(object sender, EventArgs e)
        {
            MainV2.comPort.doCommand(MAVLink.MAV_CMD.DO_SET_SERVO, (float)Convert.ToInt16(Host.config["throttlech","10"]), 1000, 0, 0, 0, 0, 0, false);
        }

        private void ECtrl_stopClicked(object sender, EventArgs e)
        {
            MainV2.comPort.doCommand(MAVLink.MAV_CMD.DO_SET_SERVO, (float)Convert.ToInt16(Host.config["throttlech", "10"]), 1500, 0, 0, 0, 0, 0, false);
        }

        private void ECtrl_startClicked(object sender, EventArgs e)
        {
            MainV2.comPort.doCommand(MAVLink.MAV_CMD.DO_SET_SERVO, (float)Convert.ToInt16(Host.config["throttlech", "10"]), 2000, 0, 0, 0, 0, 0, false);
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
                }, (byte)Host.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent);
                bool ans = Host.comPort.doARM(!isitarmed);
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
                        ans = Host.comPort.doARM(!isitarmed, true);
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
            MainV2.comPort.sendPacket(new MAVLink.mavlink_named_value_float_t() { name = c, time_boot_ms = 0, value = (float)plControl.igniteMask }, MainV2.comPort.sysidcurrent, MainV2.comPort.compidcurrent);

            Console.WriteLine("Payload ignite:{0}", plControl.igniteMask);

        }

        private void TsLandingPoint_Click(object sender, EventArgs e)
        {
            PointLatLngAlt lp = Host.FDMenuMapPosition;
            lc.updateLandingData(lp, 230, 8.0f);            //Todo get it from real wind data and add a possibility to land upwind
            landingOverlay.Markers.Clear();
            landingOverlay.Routes.Clear();

            markerWaiting = new GMarkerGoogle(lc.WaitingPoint, GMarkerGoogleType.lightblue_dot);
            markerLanding = new GMarkerGoogle(lc.LandingPoint, GMarkerGoogleType.green_dot);
            markerTarget = new GMarkerGoogle(lc.TargetPoint, GMarkerGoogleType.pink_dot);

            landingRoute = new GMapRoute("landingline");
            landingRoute.Points.Add(lc.WaitingPoint);
            landingRoute.Points.Add(lc.LandingPoint);
            landingRoute.Points.Add(lc.TargetPoint);



            landingOverlay.Markers.Add(markerWaiting);
            landingOverlay.Markers.Add(markerLanding);
            landingOverlay.Markers.Add(markerTarget);
            landingOverlay.Routes.Add(landingRoute);

            Host.FDGMapControl.Overlays.Add(landingOverlay);

            



        }

        private void Pitot_calibrateClicked(object sender, EventArgs e)
        {
            if (!MainV2.comPort.MAV.cs.connected)
            {
                CustomMessageBox.Show("You have to connect first!", "Action", MessageBoxButtons.OK);
                return;
            }

            if (MainV2.comPort.MAV.cs.armed)
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

                    //baro/airspeed calibration, but no gyro !
                    param3 = 1; // baro / airspeed
                    var cmd = (MAVLink.MAV_CMD)Enum.Parse(typeof(MAVLink.MAV_CMD), "Preflight_Calibration".ToUpper());

                    if (MainV2.comPort.doCommand(cmd, param1, param2, param3, 0, 0, 0, 0))
                    {
                    }
                    else
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

        public override bool Loop()
		//Loop is called in regular intervalls (set by loopratehz)
        {

            {
                var lq = Host.cs.linkqualitygcs;
                if (lq >= 95) aMain.setStatus("COMMS", Stat.NOMINAL);
                else if (lq < 95 && lq > 70) aMain.setStatus("COMMS", Stat.WARNING);
                else aMain.setStatus("COMMS", Stat.ALERT);
            }

            {
                var bs = bp.getGenericStatus();
                switch (bs)
                {
                    case 0: aMain.setStatus("BATT", Stat.NOMINAL);
                        break;
                    case 1: aMain.setStatus("BATT", Stat.WARNING);
                        break;
                    case 2: aMain.setStatus("BATT", Stat.ALERT);
                        break;
                    default:
                        break;
                }
            }

            #region MessagesBox

            //Message box update
            
            DateTime lastIncomingMessage = MainV2.comPort.MAV.cs.messages.LastOrDefault().time;
            if (lastIncomingMessage != lastDisplayedMessage)
            {

                fctb.BeginUpdate();
                fctb.TextSource.CurrentTB = fctb;

                MainV2.comPort.MAV.cs.messages.ForEach(x =>
                {
                    if (x.Item1 > lastDisplayedMessage)
                    {

                        TextStyle displayStyle;
                        switch ((int)x.Item3)
                        {
                            case 0:
                            case 1:
                            case 2:
                                {
                                    displayStyle = errorStyle;
                                    aMain.setStatus("MSG", Stat.ALERT);
                                    break;
                                }
                            case 3:
                            case 4:
                                {
                                    displayStyle = warningStyle;
                                    aMain.setStatus("MSG", Stat.WARNING);
                                    break;
                                }
                            default:
                                {
                                    displayStyle = infoStyle;
                                    break;
                                }
                        }

                        fctb.SelectionStart = 0;
                        fctb.InsertText(x.Item1 + " : (" + x.Item3 + ") " + x.Item2 + "\r\n", displayStyle);
                    }

                });
                fctb.EndUpdate();
                lastDisplayedMessage = lastIncomingMessage;
            }

            #endregion


            #region BatteryVoltages

            PowerStatus pwr = SystemInformation.PowerStatus;
            bp.setGcsVoltage(pwr.BatteryLifePercent, pwr.PowerLineStatus);


            #endregion


            return true;	//Return value is not used
        }

        public override bool Exit()
		//Exit called when plugin is terminated (usually when Mission Planner is exiting)
        {


            //Save position, we need to do it since formclose is not called in plugins

            annunciatorForm?.SaveStartupLocation();
            return true;	//Return value is not used
        }



        private void MavOnOnPacketReceivedHandler(object o, MAVLink.MAVLinkMessage linkMessage)
        {

            // Motor status and fuel level sensor data

            if ((MAVLink.MAVLINK_MSG_ID)linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.EFI_STATUS)
            {

                Stat engineStat = Stat.NOMINAL;
                Stat fuelStat = Stat.NOMINAL;


                MAVLink.mavlink_efi_status_t s = linkMessage.ToStructure<MAVLink.mavlink_efi_status_t>();

                eCtrl.setRpmEgt(s.rpm, s.exhaust_gas_temperature);
                eCtrl.setThrFuel(s.throttle_position, s.fuel_flow, s.fuel_consumed);
                eCtrl.setStatus((byte)s.health, (byte)s.ecu_index);
                fuel.setData(s.fuel_consumed, 0);


                if (s.rpm < 31000) engineStat = Stat.ALERT;
                if (s.exhaust_gas_temperature > 900) engineStat = Stat.ALERT;

                if (s.fuel_consumed < 150) fuelStat = Stat.WARNING;
                if (s.fuel_consumed < 50) fuelStat = Stat.ALERT;

                aMain.setStatus("ENGINE", engineStat);
                aMain.setStatus("FUEL", fuelStat);
            }



            //Pitot heating data
            if ((MAVLink.MAVLINK_MSG_ID)linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.GENERATOR_STATUS)
            {
                MAVLink.mavlink_generator_status_t s = linkMessage.ToStructure<MAVLink.mavlink_generator_status_t>();
                pitot.setData(s.battery_current, s.load_current, s.power_generated, s.bus_voltage);

            }


            //Named values (voltage and payload status

            if ((MAVLink.MAVLINK_MSG_ID)linkMessage.msgid == MAVLink.MAVLINK_MSG_ID.NAMED_VALUE_FLOAT)
            {

                MAVLink.mavlink_named_value_float_t s = linkMessage.ToStructure<MAVLink.mavlink_named_value_float_t>();

                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();


                Console.WriteLine(enc.GetString(s.name) + " : " + s.value.ToString());

                if (enc.GetString(s.name).Contains("servo1"))
                {
                    bp.setServo1Voltage(s.value);
                }

                if (enc.GetString(s.name).Contains("servo2"))
                {
                    bp.setServo2Voltage(s.value);
                }

                if (enc.GetString(s.name).Contains("main"))
                {
                    bp.setMainVoltage(s.value);
                }

                if (enc.GetString(s.name).Contains("payload"))
                {
                    bp.setPayloadVoltage(s.value);
                }

                if (s.name[0] == 'S')
                {
                    //Set status pin TODO: Need to check
                    if (s.value == 0) plControl.setSafetyStatus(true);
                    else plControl.setSafetyStatus(false);
                }


            }


        }

        private void annunciator1_buttonClicked(object sender, EventArgs e)
        {

            switch (aMain.clickedButtonName)
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
                        aMain.setStatus("MSG", Stat.NOMINAL);
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
                default:
                    break;
            }
        }



        private void annunciator1_undock(object sender, EventArgs e)
        {
            annunciatorForm = new FloatingForm();
            annunciatorForm.Text = "Annunciator";
            annunciatorForm.RestoreStartupLocation();
            MainV2.instance.panel1.Controls.Remove(aMain);
            aMain.isSingleLine = false;
            aMain.Dock = DockStyle.None;
            aMain.Visible = true;
            aMain.contextMenuEnabled = false;
            aMain.Size = annunciatorForm.Size;
            //annunciator1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            annunciatorForm.Controls.Add(aMain);
            annunciatorForm.FormClosing += annunciator_FormClosing;
            //annunciatorUndocked = true;
            MainV2.instance.panel1.Dock = DockStyle.Top;
            MainV2.instance.panel1.Visible = false;
            aMain.Invalidate();
            Settings.Instance["aMainDocked"] = "false";
            Settings.Instance.Save();
            annunciatorForm.Show();
        }

        void annunciator_FormClosing(object sender, FormClosingEventArgs e)
        {
            (sender as Form).SaveStartupLocation();
            aMain.isSingleLine = true;
            aMain.contextMenuEnabled = true;
            MainV2.instance.panel1.Controls.Add(aMain);
            MainV2.instance.panel1.Dock = DockStyle.Top;
            MainV2.instance.panel1.Visible = true;
            aMain.Size = MainV2.instance.panel1.Size;
            //annunciatorUndocked = false;
            Settings.Instance["aMainDocked"] = "true";
            Settings.Instance.Save();
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
            

        }
        float wrap360(float noin)
        {
            if (noin < 0)
                return noin + 360;
            return noin;
        }


    }
}