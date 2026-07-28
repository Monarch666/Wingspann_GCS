using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigHWCompass : MyUserControl, IActivate
    {
        private const int THRESHOLD_OFS_RED = 600;
        private const int THRESHOLD_OFS_YELLOW = 400;
        private bool startup;

        private enum CompassNumber
        {
            Compass1 = 0,
            Compass2,
            Compass3
        };

        public ConfigHWCompass()
        {
            InitializeComponent();
        }

        public void Activate()
        {
            Enabled = true;
            startup = true;

            this.Controls.Clear();
            if (pnlHorizonContainer == null)
            {
                CreateHorizonLayout();
            }
            this.Controls.Add(pnlHorizonContainer);

            try
            {
                if (MainV2.comPort.MAV.param["COMPASS_CAL_FIT"] != null)
                {
                    float fit = (float)MainV2.comPort.MAV.param["COMPASS_CAL_FIT"].Value;
                    chkFitnessStrict.Checked = (fit >= 16);
                }
            }
            catch { }

            UpdateHorizonUI();
            startup = false;
        }

        // Find the maximum absolute value of three values. Used to detect abnormally high or
        // low compass offsets.
        private int absmax(int val1, int val2, int val3)
        {
            return Math.Max(Math.Max(Math.Abs(val1), Math.Abs(val2)), Math.Abs(val3));
        }

        public void Deactivate()
        {
            timer1.Stop();
        }

        private void BUT_MagCalibration_Click(object sender, EventArgs e)
        {
            MagCalib.DoGUIMagCalib();
            Activate(); // Necessary to refresh offset values displayed on form
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //System.Diagnostics.Process.Start("http://www.ngdc.noaa.gov/geomagmodels/Declination.jsp");
                Process.Start("http://www.magnetic-declination.com/");
            }
            catch
            {
                CustomMessageBox.Show(
                    "Webpage open failed... do you have a virus?\nhttp://www.magnetic-declination.com/", "Mag");
            }
        }

        private void TXT_declination_Validating(object sender, CancelEventArgs e)
        {
            float ans = 0;
            e.Cancel = !float.TryParse(TXT_declination_deg.Text, out ans);
        }

        private void TXT_declination_Validated(object sender, EventArgs e)
        {
            if (startup)
                return;
            try
            {
                if (MainV2.comPort.MAV.param["COMPASS_DEC"] == null)
                {
                    CustomMessageBox.Show(Strings.ErrorFeatureNotEnabled, Strings.ERROR);
                }
                else
                {
                    var dec = 0.0f;
                    try
                    {
                        var deg = TXT_declination_deg.Text;

                        var min = TXT_declination_min.Text;

                        dec = float.Parse(deg);

                        if (dec < 0)
                            dec -= (float.Parse(min) / 60);
                        else
                            dec += (float.Parse(min) / 60);
                    }
                    catch
                    {
                        CustomMessageBox.Show(Strings.InvalidNumberEntered, Strings.ERROR);
                        return;
                    }

                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_DEC", dec * MathHelper.deg2rad);
                }
            }
            catch
            {
                CustomMessageBox.Show(string.Format(Strings.ErrorSetValueFailed, "COMPASS_DEC"), Strings.ERROR);
            }
        }

        private void CHK_enablecompass_CheckedChanged(object sender, EventArgs e)
        {
            // I am commenting this out with caution. I don't see why
            // enabling/disabling the compass shoudl change whether or
            // not autodec is enabled, but am keeping code here and commented
            // just in case I'm missing something.
            //if (((CheckBox) sender).Checked)
            //{
            //    CHK_autodec.Enabled = true;
            //    TXT_declination_deg.Enabled = true;
            //    TXT_declination_min.Enabled = true;
            // }
            //else
            //{
            //    CHK_autodec.Enabled = false;
            //    TXT_declination_deg.Enabled = false;
            //    TXT_declination_min.Enabled = false;
            //}

            if (startup)
                return;
            try
            {
                if (MainV2.comPort.MAV.param["MAG_ENABLE"] == null)
                {
                    CustomMessageBox.Show(Strings.ErrorFeatureNotEnabled, Strings.ERROR);
                }
                else
                {
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "MAG_ENABLE", ((CheckBox)sender).Checked ? 1 : 0);
                }
            }
            catch
            {
                CustomMessageBox.Show(string.Format(Strings.ErrorSetValueFailed, "MAG_ENABLE"), Strings.ERROR);
            }
        }

        private async void BUT_MagCalibrationLog_Click(object sender, EventArgs e)
        {
            var minthro = "30";
            if (DialogResult.Cancel ==
                InputBox.Show("Min Throttle", "Use only data above this throttle percent.", ref minthro))
                return;

            var ans = 0;
            int.TryParse(minthro, out ans);

            await MagCalib.ProcessLog(ans).ConfigureAwait(true);
        }

        private void CHK_autodec_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                TXT_declination_deg.Enabled = false;

                TXT_declination_min.Enabled = false;
            }
            else
            {
                TXT_declination_deg.Enabled = true;
                TXT_declination_min.Enabled = true;
            }

            if (startup)
                return;
            try
            {
                if (MainV2.comPort.MAV.param["COMPASS_AUTODEC"] == null)
                {
                    CustomMessageBox.Show("Not Available on " + MainV2.comPort.MAV.cs.firmware);
                }
                else
                {
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_AUTODEC", ((CheckBox)sender).Checked ? 1 : 0);
                }
            }
            catch
            {
                CustomMessageBox.Show("Set COMPASS_AUTODEC Failed");
            }
        }

        private void linkLabel1_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://www.youtube.com/watch?v=DmsueBS0J3E");
            }
            catch
            {
                CustomMessageBox.Show(Strings.ERROR + " https://www.youtube.com/watch?v=DmsueBS0J3E");
            }
        }

        private List<MAVLink.MAVLinkMessage> mprog = new List<MAVLink.MAVLinkMessage>();
        private List<MAVLink.MAVLinkMessage> mrep = new List<MAVLink.MAVLinkMessage>();

        private bool ReceviedPacket(MAVLink.MAVLinkMessage packet)
        {
            if (System.Diagnostics.Debugger.IsAttached)
                MainV2.comPort.DebugPacket(packet, true);

            if (packet.msgid == (byte)MAVLink.MAVLINK_MSG_ID.MAG_CAL_PROGRESS)
            {
                lock (this.mprog)
                {
                    this.mprog.Add(packet);
                }

                return true;
            }
            else if (packet.msgid == (byte)MAVLink.MAVLINK_MSG_ID.MAG_CAL_REPORT)
            {
                lock (this.mrep)
                {
                    this.mrep.Add(packet);
                }

                return true;
            }

            return true;
        }

        private int packetsub1;
        private int packetsub2;

        private void BUT_OBmagcalstart_Click(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, MAVLink.MAV_CMD.DO_START_MAG_CAL, 0, 1, 1, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                this.LogError(ex);
                CustomMessageBox.Show("Failed to start MAG CAL, check the autopilot is still responding.\n" + ex.ToString(), Strings.ERROR);
                return;
            }

            mprog.Clear();
            mrep.Clear();
            horizontalProgressBar1.Value = 0;
            horizontalProgressBar2.Value = 0;
            horizontalProgressBar3.Value = 0;

            packetsub1 = MainV2.comPort.SubscribeToPacketType(MAVLink.MAVLINK_MSG_ID.MAG_CAL_PROGRESS, ReceviedPacket, (byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent);
            packetsub2 = MainV2.comPort.SubscribeToPacketType(MAVLink.MAVLINK_MSG_ID.MAG_CAL_REPORT, ReceviedPacket, (byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent);

            BUT_OBmagcalaccept.Enabled = true;
            BUT_OBmagcalcancel.Enabled = true;
            timer1.Start();
        }

        private void BUT_OBmagcalaccept_Click(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, MAVLink.MAV_CMD.DO_ACCEPT_MAG_CAL, 0, 0, 1, 0, 0, 0, 0);

            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.ToString(), Strings.ERROR, MessageBoxButtons.OK);
            }

            MainV2.comPort.UnSubscribeToPacketType(packetsub1);
            MainV2.comPort.UnSubscribeToPacketType(packetsub2);

            timer1.Stop();
        }

        private void BUT_OBmagcalcancel_Click(object sender, EventArgs e)
        {
            try
            {
                MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, MAVLink.MAV_CMD.DO_CANCEL_MAG_CAL, 0, 0, 1, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.ToString(), Strings.ERROR, MessageBoxButtons.OK);
            }

            MainV2.comPort.UnSubscribeToPacketType(packetsub1);
            MainV2.comPort.UnSubscribeToPacketType(packetsub2);

            timer1.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            lbl_obmagresult.Clear();
            int compasscount = 0;
            int completecount = 0;
            lock (mprog)
            {
                // somewhere to save our %
                Dictionary<byte, MAVLink.MAVLinkMessage> status = new Dictionary<byte, MAVLink.MAVLinkMessage>();
                foreach (var item in mprog)
                {
                    status[((MAVLink.mavlink_mag_cal_progress_t)item.data).compass_id] = item;
                }

                // message for user
                string message = "";
                foreach (var item in status)
                {
                    var obj = (MAVLink.mavlink_mag_cal_progress_t)item.Value.data;

                    try
                    {
                        if (item.Key == 0)
                            horizontalProgressBar1.Value = obj.completion_pct;
                        if (item.Key == 1)
                            horizontalProgressBar2.Value = obj.completion_pct;
                        if (item.Key == 2)
                            horizontalProgressBar3.Value = obj.completion_pct;
                    }
                    catch { }

                    message += "id:" + item.Key + " " + obj.completion_pct.ToString() + "% ";
                    compasscount++;
                }
                lbl_obmagresult.AppendText(message + "\n");
            }

            lock (mrep)
            {
                // somewhere to save our answer
                Dictionary<byte, MAVLink.MAVLinkMessage> status = new Dictionary<byte, MAVLink.MAVLinkMessage>();
                foreach (var item in mrep)
                {
                    var obj = (MAVLink.mavlink_mag_cal_report_t)item.data;

                    if (obj.compass_id == 0 && obj.ofs_x == 0)
                        continue;

                    status[obj.compass_id] = item;
                }

                // message for user
                foreach (var item in status.Values)
                {
                    var obj = (MAVLink.mavlink_mag_cal_report_t)item.data;

                    lbl_obmagresult.AppendText("id:" + obj.compass_id + " x:" + obj.ofs_x.ToString("0.0") + " y:" +
                                               obj.ofs_y.ToString("0.0") + " z:" +
                                               obj.ofs_z.ToString("0.0") + " fit:" + obj.fitness.ToString("0.0") + " " +
                                               (MAVLink.MAG_CAL_STATUS)obj.cal_status + "\n");

                    try
                    {
                        if (obj.compass_id == 0)
                            horizontalProgressBar1.Value = 100;
                        if (obj.compass_id == 1)
                            horizontalProgressBar2.Value = 100;
                        if (obj.compass_id == 2)
                            horizontalProgressBar3.Value = 100;
                    }
                    catch
                    {
                    }

                    if ((MAVLink.MAG_CAL_STATUS)obj.cal_status != MAVLink.MAG_CAL_STATUS.MAG_CAL_SUCCESS)
                    {
                        //CustomMessageBox.Show(Strings.CommandFailed);
                    }

                    if (obj.autosaved == 1)
                    {
                        completecount++;
                        timer1.Interval = 1000;
                    }
                }
            }

            if (compasscount == completecount && compasscount != 0)
            {
                BUT_OBmagcalcancel.Enabled = false;
                BUT_OBmagcalaccept.Enabled = false;
                timer1.Stop();
                CustomMessageBox.Show("Please reboot the autopilot");
            }

            UpdateHorizonUI();
        }

        private void buttonQuickPixhawk_Click(object sender, EventArgs e)
        {
            if (!MainV2.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.ErrorNotConnected);
                MainV2.View.Reload();
                return;
            }

            try
            {
                // TODO: check this code against the original. I don't understand what the original does
                // with the different firmware versions, and I changed something about the externality
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE", 1);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE2", 1);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE3", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERNAL", 1);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERN2", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERN3", 0);

                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_PRIMARY", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_LEARN", 1);

                if (
                    CustomMessageBox.Show("is the FW version greater than APM:copter 3.01 or APM:Plane 2.74?", "",
                        MessageBoxButtons.YesNo) == (int)DialogResult.Yes)
                {
                    CMB_compass1_orient.SelectedIndex = (int)Rotation.ROTATION_NONE;
                }
                else
                {
                    CMB_compass1_orient.SelectedIndex = (int)Rotation.ROTATION_ROLL_180;
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERNAL", 0);
                }
            }
            catch (Exception)
            {
                CustomMessageBox.Show(Strings.ErrorSettingParameter, Strings.ERROR);
            }
            Activate();
        }

        private void QuickAPM25_Click(object sender, EventArgs e)
        {
            if (!MainV2.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.ErrorNotConnected);
                MainV2.View.Reload();
                return;
            }
            try
            {
                CMB_compass1_orient.SelectedIndex = (int)Rotation.ROTATION_NONE;
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE1", 1);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE2", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE3", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERNAL", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERN2", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERN3", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_PRIMARY", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_LEARN", 1);
            }
            catch (Exception)
            {
                CustomMessageBox.Show(Strings.ErrorSettingParameter, Strings.ERROR);
            }
            Activate();
        }

        private void buttonAPMExternal_Click(object sender, EventArgs e)
        {
            if (!MainV2.comPort.BaseStream.IsOpen)
            {
                CustomMessageBox.Show(Strings.ErrorNotConnected);
                MainV2.View.Reload();
                return;
            }
            try
            {
                CMB_compass1_orient.SelectedIndex = (int)Rotation.ROTATION_ROLL_180;
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERNAL", 1);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERN2", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_EXTERN3", 0);

                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE1", 1);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE2", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_USE3", 0);

                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_PRIMARY", 0);
                MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_LEARN", 1);
            }
            catch (Exception)
            {
                CustomMessageBox.Show(Strings.ErrorSettingParameter, Strings.ERROR);
            }
            Activate();
        }

        private void CHK_compasslearn_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (MainV2.comPort.MAV.param["COMPASS_LEARN"] == null)
                {
                    CustomMessageBox.Show("Not Available on " + MainV2.comPort.MAV.cs.firmware);
                }
                else
                {
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_LEARN", ((CheckBox)sender).Checked ? 1 : 0);
                }
            }
            catch
            {
                CustomMessageBox.Show("Set COMPASS_LEARN Failed");
            }
        }


        private void CHK_compass(object sender, EventArgs e)
        {
            ShowRelevantFields();
        }

        private void ShowRelevantFields()
        {
            TXT_declination_deg.Enabled = !CHK_autodec.Checked;
            TXT_declination_min.Enabled = !CHK_autodec.Checked;

            CMB_compass1_orient.Visible = CHK_compass1_external.Checked;
            CMB_compass2_orient.Visible = CHK_compass2_external.Checked;
            CMB_compass3_orient.Visible = CHK_compass3_external.Checked;

            LBL_compass1_mot.Visible = CHK_compass1_use.Checked;
            LBL_compass1_offset.Visible = CHK_compass1_use.Checked;

            LBL_compass2_mot.Visible = CHK_compass2_use.Checked;
            LBL_compass2_offset.Visible = CHK_compass2_use.Checked;

            LBL_compass3_mot.Visible = CHK_compass3_use.Checked;
            LBL_compass3_offset.Visible = CHK_compass3_use.Checked;

            // Toggle primary compass controls as appropriate
            CMB_primary_compass.Visible = MainV2.comPort.MAV.param.ContainsKey("COMPASS_PRIMARY");
            LBL_primary_compass.Visible = MainV2.comPort.MAV.param.ContainsKey("COMPASS_PRIMARY");
        }

        // Horizon Compass Layout Fields
        private Panel pnlHorizonContainer;
        private Label lblHorizonTitle;
        private Label lblHorizonSubtitle;
        private Label lblStatusBadge;

        private Panel pnlProcedure;
        private Panel pnlVisualizer;
        private Panel pnlOffsets;
        private Panel pnlWhy;
        private Panel pnlProgress;

        // Steps
        private Label lblStep1Circle, lblStep1Title, lblStep1Desc;
        private Label lblStep2Circle, lblStep2Title, lblStep2Desc;
        private Label lblStep3Circle, lblStep3Title, lblStep3Desc;

        // Visualizer
        private Label lblVisualizerStatus;

        // Offsets Table Labels
        private Label lblComp1Header;
        private Label lblComp1LblX, lblComp1ValX;
        private Label lblComp1LblY, lblComp1ValY;
        private Label lblComp1LblZ, lblComp1ValZ;

        private Label lblComp2Header;
        private Label lblComp2LblX, lblComp2ValX;
        private Label lblComp2LblY, lblComp2ValY;
        private Label lblComp2LblZ, lblComp2ValZ;

        private Label lblSamplesHeader;
        private Label lblSamplesCollected;

        // Progress controls
        private Label lblProgressHeader;
        private Panel pnlProgressBarTrack;
        private Panel pnlProgressBarFill;
        private Label lblProgressPct;
        private CheckBox chkFitnessStrict;
        private Button btnHorizonCancel;
        private Button btnHorizonStart;

        private void CreateHorizonLayout()
        {
            pnlHorizonContainer = new Panel();
            pnlHorizonContainer.BackColor = Theme.Background;
            pnlHorizonContainer.ForeColor = Theme.White;

            // Title
            lblHorizonTitle = new Label();
            lblHorizonTitle.Text = "Compass Calibration";
            StyleHelper.ApplyLabelStyle(lblHorizonTitle, Theme.HeaderFont, Theme.White);
            lblHorizonTitle.AutoSize = true;
            pnlHorizonContainer.Controls.Add(lblHorizonTitle);

            // Subtitle
            lblHorizonSubtitle = new Label();
            lblHorizonSubtitle.Text = "Align the internal and external magnetometers to ensure accurate heading estimation during flight.\nRequired after hardware changes or significant location shifts.";
            StyleHelper.ApplyLabelStyle(lblHorizonSubtitle, Theme.SubtitleFont, Theme.GreyText);
            lblHorizonSubtitle.AutoSize = true;
            pnlHorizonContainer.Controls.Add(lblHorizonSubtitle);

            // Status Badge
            lblStatusBadge = new Label();
            lblStatusBadge.Text = "STATUS: CALIBRATION REQUIRED";
            lblStatusBadge.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStatusBadge.BackColor = Color.FromArgb(60, 45, 0);
            lblStatusBadge.ForeColor = Theme.AccentOrange;
            lblStatusBadge.BorderStyle = BorderStyle.FixedSingle;
            lblStatusBadge.TextAlign = ContentAlignment.MiddleCenter;
            lblStatusBadge.Size = new Size(240, 32);
            pnlHorizonContainer.Controls.Add(lblStatusBadge);

            // Panels
            pnlProcedure = CreatePanel("Procedure|SYS_GUIDE");
            pnlVisualizer = CreatePanel("Viz Attitude|VIZ_ATTITUDE");
            pnlOffsets = CreatePanel("Mag Offsets (Raw)|DATA_STREAM");
            pnlWhy = CreatePanel("Why this matters|INFO_TIP");
            pnlProgress = CreatePanel("Calibration Progress|CALIB_STATUS");

            // Name visualizer panel so Paint handler can identify it
            pnlVisualizer.Name = "pnlVisualizer";

            // --- Procedure Panel Child Controls ---
            lblStep1Circle = CreateCircleLabel("lblCircle1");
            lblStep1Title = CreateTitleLabel("Clear Area");
            lblStep1Desc = CreateDescLabel("Move away from large metal objects,\nspeakers, or power lines.");
            pnlProcedure.Controls.AddRange(new Control[] { lblStep1Circle, lblStep1Title, lblStep1Desc });

            lblStep2Circle = CreateCircleLabel("lblCircle2");
            lblStep2Title = CreateTitleLabel("Initiate");
            lblStep2Desc = CreateDescLabel("Click 'Start Calibration' below to\nbegin gathering samples.");
            pnlProcedure.Controls.AddRange(new Control[] { lblStep2Circle, lblStep2Title, lblStep2Desc });

            lblStep3Circle = CreateCircleLabel("lblCircle3");
            lblStep3Title = CreateTitleLabel("Rotate Vehicle");
            lblStep3Desc = CreateDescLabel("Rotate the drone smoothly on all\naxes until the progress bar\ncompletes.");
            pnlProcedure.Controls.AddRange(new Control[] { lblStep3Circle, lblStep3Title, lblStep3Desc });

            // --- Visualizer Panel Child Controls ---
            lblVisualizerStatus = new Label();
            lblVisualizerStatus.Text = "AWAITING ROTATION";
            lblVisualizerStatus.Font = new Font("Consolas", 11F, FontStyle.Bold);
            lblVisualizerStatus.ForeColor = Color.FromArgb(120, 130, 140);
            lblVisualizerStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblVisualizerStatus.Dock = DockStyle.Fill;
            pnlVisualizer.Controls.Add(lblVisualizerStatus);

            // --- Offsets Panel Child Controls ---
            lblComp1Header = CreateSectionHeaderLabel("COMPASS 1 (Primary)");
            lblComp1LblX = CreateOffsetLabel("X:", false); lblComp1ValX = CreateOffsetLabel("----", true);
            lblComp1LblY = CreateOffsetLabel("Y:", false); lblComp1ValY = CreateOffsetLabel("----", true);
            lblComp1LblZ = CreateOffsetLabel("Z:", false); lblComp1ValZ = CreateOffsetLabel("----", true);
            pnlOffsets.Controls.AddRange(new Control[] { lblComp1Header, lblComp1LblX, lblComp1ValX, lblComp1LblY, lblComp1ValY, lblComp1LblZ, lblComp1ValZ });

            lblComp2Header = CreateSectionHeaderLabel("COMPASS 2 (External)");
            lblComp2LblX = CreateOffsetLabel("X:", false); lblComp2ValX = CreateOffsetLabel("----", true);
            lblComp2LblY = CreateOffsetLabel("Y:", false); lblComp2ValY = CreateOffsetLabel("----", true);
            lblComp2LblZ = CreateOffsetLabel("Z:", false); lblComp2ValZ = CreateOffsetLabel("----", true);
            pnlOffsets.Controls.AddRange(new Control[] { lblComp2Header, lblComp2LblX, lblComp2ValX, lblComp2LblY, lblComp2ValY, lblComp2LblZ, lblComp2ValZ });

            lblSamplesHeader = CreateSectionHeaderLabel("SAMPLES COLLECTED");
            lblSamplesCollected = new Label();
            lblSamplesCollected.Text = "0 / 400";
            lblSamplesCollected.Font = new Font("Consolas", 18F, FontStyle.Bold);
            lblSamplesCollected.ForeColor = Theme.White;
            lblSamplesCollected.AutoSize = true;
            pnlOffsets.Controls.Add(lblSamplesHeader);
            pnlOffsets.Controls.Add(lblSamplesCollected);

            // --- Why child controls ---
            var lblWhyText = new Label();
            lblWhyText.Text = "An uncalibrated compass can lead to erratic flight behavior, 'toilet-bowling' in Loiter mode, or complete loss of autonomous navigation capability.";
            StyleHelper.ApplyLabelStyle(lblWhyText, Theme.RegularFont, Theme.GreyText);
            lblWhyText.Location = new Point(16, 45);
            lblWhyText.Size = new Size(240, 100);
            pnlWhy.Controls.Add(lblWhyText);

            // --- Progress child controls ---
            lblProgressHeader = new Label();
            lblProgressHeader.Text = "CALIBRATION PROGRESS";
            lblProgressHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblProgressHeader.ForeColor = Color.FromArgb(120, 125, 130);
            lblProgressHeader.AutoSize = true;
            pnlProgress.Controls.Add(lblProgressHeader);

            pnlProgressBarTrack = new Panel();
            pnlProgressBarTrack.BackColor = Color.FromArgb(28, 30, 34);
            pnlProgressBarTrack.Height = 10;
            pnlProgress.Controls.Add(pnlProgressBarTrack);

            pnlProgressBarFill = new Panel();
            pnlProgressBarFill.BackColor = Theme.AccentOrange;
            pnlProgressBarFill.Height = 10;
            pnlProgressBarTrack.Controls.Add(pnlProgressBarFill);

            lblProgressPct = new Label();
            lblProgressPct.Text = "0%";
            lblProgressPct.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblProgressPct.ForeColor = Theme.AccentOrange;
            lblProgressPct.AutoSize = true;
            pnlProgress.Controls.Add(lblProgressPct);

            chkFitnessStrict = new CheckBox();
            chkFitnessStrict.Text = "Fitness: Strict";
            StyleHelper.ApplyCheckboxStyle(chkFitnessStrict);
            chkFitnessStrict.CheckedChanged += ChkFitnessStrict_CheckedChanged;
            pnlProgress.Controls.Add(chkFitnessStrict);

            btnHorizonCancel = new Button();
            btnHorizonCancel.Text = "CANCEL";
            StyleHelper.ApplyButtonStyle(btnHorizonCancel, false);
            btnHorizonCancel.Size = new Size(110, 32);
            btnHorizonCancel.Click += (s, e) => BUT_OBmagcalcancel_Click(null, null);
            pnlProgress.Controls.Add(btnHorizonCancel);

            btnHorizonStart = new Button();
            btnHorizonStart.Text = "START CALIBRATION";
            StyleHelper.ApplyButtonStyle(btnHorizonStart, true);
            btnHorizonStart.Size = new Size(190, 32);
            btnHorizonStart.Click += BtnHorizonStart_Click;
            pnlProgress.Controls.Add(btnHorizonStart);

            pnlHorizonContainer.Resize += (s, e) => LayoutHorizon();
            LayoutHorizon();
        }

        private void BtnHorizonStart_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                BUT_OBmagcalaccept_Click(null, null);
            }
            else
            {
                BUT_OBmagcalstart_Click(null, null);
            }
        }

        private void ChkFitnessStrict_CheckedChanged(object sender, EventArgs e)
        {
            if (startup) return;
            try
            {
                if (MainV2.comPort.MAV.param["COMPASS_CAL_FIT"] != null)
                {
                    MainV2.comPort.setParam((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "COMPASS_CAL_FIT", chkFitnessStrict.Checked ? 16 : 8);
                }
            }
            catch { }
        }

        private Panel CreatePanel(string panelTag)
        {
            Panel p = new Panel();
            string[] tags = panelTag.Split('|');
            string title = tags.Length > 0 ? tags[0] : "";
            string sysTag = tags.Length > 1 ? tags[1] : "";
            
            StyleHelper.ApplyPanelStyle(p, title, sysTag);
            pnlHorizonContainer.Controls.Add(p);
            return p;
        }

        private Label CreateCircleLabel(string name)
        {
            Label lbl = new Label();
            lbl.Name = name;
            lbl.Size = new Size(24, 24);
            lbl.BackColor = Color.Transparent;
            lbl.Paint += PaintStepCircle;
            return lbl;
        }

        private Label CreateTitleLabel(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            StyleHelper.ApplyLabelStyle(lbl, Theme.TitleFont, Theme.White);
            lbl.AutoSize = true;
            return lbl;
        }

        private Label CreateDescLabel(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            StyleHelper.ApplyLabelStyle(lbl, Theme.RegularFont, Color.FromArgb(140, 145, 150));
            lbl.AutoSize = true;
            return lbl;
        }

        private Label CreateSectionHeaderLabel(string text)
        {
            Label lbl = new Label();
            lbl.Text = text;
            StyleHelper.ApplyLabelStyle(lbl, Theme.RegularFont, Color.FromArgb(120, 130, 140));
            lbl.AutoSize = true;
            return lbl;
        }

        private Label CreateOffsetLabel(string text, bool isVal)
        {
            Label lbl = new Label();
            lbl.Text = text;
            StyleHelper.ApplyLabelStyle(lbl, Theme.TechnicalFont, isVal ? Theme.AccentOrange : Color.FromArgb(140, 145, 150));
            lbl.AutoSize = true;
            return lbl;
        }

        private void PaintStepCircle(object sender, PaintEventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            bool isActive = (lbl.Name == "lblCircle1" && !timer1.Enabled) || (lbl.Name == "lblCircle3" && timer1.Enabled);
            Color circleColor = isActive ? Theme.AccentOrange : Color.FromArgb(40, 45, 50);

            using (var brush = new SolidBrush(circleColor))
            {
                g.FillEllipse(brush, 0, 0, lbl.Width - 1, lbl.Height - 1);
            }

            string num = lbl.Name.Substring(lbl.Name.Length - 1);
            using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var textBrush = new SolidBrush(isActive ? Color.Black : Color.White))
            {
                SizeF size = g.MeasureString(num, font);
                g.DrawString(num, font, textBrush, lbl.Width / 2 - size.Width / 2, lbl.Height / 2 - size.Height / 2);
            }
        }

        private void LayoutHorizon()
        {
            if (pnlHorizonContainer == null) return;

            pnlHorizonContainer.Bounds = this.ClientRectangle;

            // Title
            lblHorizonTitle.Location = new Point(20, 20);
            lblHorizonSubtitle.Location = new Point(22, 55);

            // Status Badge
            lblStatusBadge.Location = new Point(pnlHorizonContainer.Width - lblStatusBadge.Width - 30, 20);

            // Columns layout
            int startY = 110;
            int bottomHeight = 150;
            int colHeight = pnlHorizonContainer.Height - startY - bottomHeight - 30;
            if (colHeight < 150) colHeight = 150;

            int colWidth = (pnlHorizonContainer.Width - 50) / 3;
            if (colWidth < 180) colWidth = 180;

            pnlProcedure.Bounds = new Rectangle(20, startY, colWidth, colHeight);
            pnlVisualizer.Bounds = new Rectangle(20 + colWidth + 10, startY, colWidth, colHeight);
            pnlOffsets.Bounds = new Rectangle(20 + (colWidth + 10) * 2, startY, colWidth, colHeight);

            // Bottom panels layout
            int bottomY = startY + colHeight + 12;
            pnlWhy.Bounds = new Rectangle(20, bottomY, colWidth, bottomHeight);
            pnlProgress.Bounds = new Rectangle(20 + colWidth + 10, bottomY, colWidth * 2 + 10, bottomHeight);

            // Step layout inside Procedure
            int stepGap = colHeight / 4;
            if (stepGap < 45) stepGap = 45;
            LayoutStep(lblStep1Circle, lblStep1Title, lblStep1Desc, 16, 45);
            LayoutStep(lblStep2Circle, lblStep2Title, lblStep2Desc, 16, 45 + stepGap);
            LayoutStep(lblStep3Circle, lblStep3Title, lblStep3Desc, 16, 45 + stepGap * 2);

            // Offset fields layout inside Offsets Panel
            lblComp1Header.Location = new Point(16, 40);
            LayoutOffsetRow(lblComp1LblX, lblComp1ValX, lblComp1LblY, lblComp1ValY, lblComp1LblZ, lblComp1ValZ, 16, 62);

            lblComp2Header.Location = new Point(16, 95);
            LayoutOffsetRow(lblComp2LblX, lblComp2ValX, lblComp2LblY, lblComp2ValY, lblComp2LblZ, lblComp2ValZ, 16, 117);

            lblSamplesHeader.Location = new Point(16, 155);
            lblSamplesCollected.Location = new Point(16, 175);

            // Progress Panel controls layout
            lblProgressHeader.Location = new Point(16, 35);
            pnlProgressBarTrack.Location = new Point(16, 55);
            pnlProgressBarTrack.Width = pnlProgress.Width - 100;
            lblProgressPct.Location = new Point(pnlProgressBarTrack.Right + 12, 52);

            chkFitnessStrict.Location = new Point(16, 95);
            btnHorizonCancel.Location = new Point(pnlProgress.Width - 320, 90);
            btnHorizonStart.Location = new Point(pnlProgress.Width - 206, 90);
        }

        private void LayoutStep(Label circle, Label title, Label desc, int x, int y)
        {
            circle.Location = new Point(x, y);
            title.Location = new Point(x + 32, y + 2);
            desc.Location = new Point(x + 32, y + 24);
        }

        private void LayoutOffsetRow(Label lx, Label vx, Label ly, Label vy, Label lz, Label vz, int x, int y)
        {
            lx.Location = new Point(x, y);
            vx.Location = new Point(x + 18, y);
            ly.Location = new Point(x + 65, y);
            vy.Location = new Point(x + 83, y);
            lz.Location = new Point(x + 130, y);
            vz.Location = new Point(x + 148, y);
        }

        private void UpdateHorizonUI()
        {
            if (pnlHorizonContainer == null) return;

            // Strict fitness parameter
            if (MainV2.comPort.MAV.param["COMPASS_CAL_FIT"] != null)
            {
                try
                {
                    float fit = (float)MainV2.comPort.MAV.param["COMPASS_CAL_FIT"].Value;
                    chkFitnessStrict.Checked = (fit >= 16);
                }
                catch { }
            }

            int maxPct = 0;
            lock (mprog)
            {
                foreach (var item in mprog)
                {
                    var obj = (MAVLink.mavlink_mag_cal_progress_t)item.data;
                    if (obj.completion_pct > maxPct)
                        maxPct = obj.completion_pct;
                }
            }

            // Sync offsets report
            lock (mrep)
            {
                foreach (var item in mrep)
                {
                    var obj = (MAVLink.mavlink_mag_cal_report_t)item.data;
                    if (obj.compass_id == 0)
                    {
                        lblComp1ValX.Text = obj.ofs_x.ToString("0");
                        lblComp1ValY.Text = obj.ofs_y.ToString("0");
                        lblComp1ValZ.Text = obj.ofs_z.ToString("0");
                    }
                    else if (obj.compass_id == 1)
                    {
                        lblComp2ValX.Text = obj.ofs_x.ToString("0");
                        lblComp2ValY.Text = obj.ofs_y.ToString("0");
                        lblComp2ValZ.Text = obj.ofs_z.ToString("0");
                    }
                }
            }

            // Fallback: Read parameter offsets if reports are empty
            if (lblComp1ValX.Text == "----" && MainV2.comPort.MAV.param.ContainsKey("COMPASS_OFS_X"))
            {
                try
                {
                    lblComp1ValX.Text = ((int)MainV2.comPort.MAV.param["COMPASS_OFS_X"]).ToString();
                    lblComp1ValY.Text = ((int)MainV2.comPort.MAV.param["COMPASS_OFS_Y"]).ToString();
                    lblComp1ValZ.Text = ((int)MainV2.comPort.MAV.param["COMPASS_OFS_Z"]).ToString();
                }
                catch { }
            }
            if (lblComp2ValX.Text == "----" && MainV2.comPort.MAV.param.ContainsKey("COMPASS_OFS2_X"))
            {
                try
                {
                    lblComp2ValX.Text = ((int)MainV2.comPort.MAV.param["COMPASS_OFS2_X"]).ToString();
                    lblComp2ValY.Text = ((int)MainV2.comPort.MAV.param["COMPASS_OFS2_Y"]).ToString();
                    lblComp2ValZ.Text = ((int)MainV2.comPort.MAV.param["COMPASS_OFS2_Z"]).ToString();
                }
                catch { }
            }

            // Toggles between Awaiting and Active Calibrating States
            if (timer1.Enabled)
            {
                lblStatusBadge.Text = "STATUS: CALIBRATING";
                lblStatusBadge.BackColor = Color.FromArgb(60, 45, 0);
                lblStatusBadge.ForeColor = Theme.AccentOrange;

                lblVisualizerStatus.Text = "CALIBRATING...\nROTATING VEHICLE";
                lblVisualizerStatus.ForeColor = Theme.AccentOrange;

                btnHorizonStart.Text = "ACCEPT CALIBRATION";
                bool hasReports = false;
                lock (mrep)
                {
                    hasReports = mrep.Count > 0;
                }
                btnHorizonStart.Enabled = hasReports;
                btnHorizonStart.BackColor = hasReports ? Theme.AccentOrange : Color.FromArgb(80, 20, 20, 20);
                btnHorizonCancel.Enabled = true;

                // Sync progress bar
                int progressWidth = (pnlProgressBarTrack.Width * maxPct) / 100;
                pnlProgressBarFill.Width = progressWidth;
                pnlProgressBarFill.BackColor = Theme.AccentOrange;
                lblProgressPct.Text = $"{maxPct}%";

                int samples = (maxPct * 400) / 100;
                lblSamplesCollected.Text = $"{samples} / 400";
            }
            else
            {
                bool isCalibrated = false;
                if (MainV2.comPort.MAV.param.ContainsKey("COMPASS_OFS_X"))
                {
                    int ox = (int)MainV2.comPort.MAV.param["COMPASS_OFS_X"];
                    int oy = (int)MainV2.comPort.MAV.param["COMPASS_OFS_Y"];
                    int oz = (int)MainV2.comPort.MAV.param["COMPASS_OFS_Z"];
                    isCalibrated = (ox != 0 || oy != 0 || oz != 0);
                }

                if (isCalibrated)
                {
                    lblStatusBadge.Text = "STATUS: CALIBRATED";
                    lblStatusBadge.BackColor = Color.FromArgb(12, 35, 12);
                    lblStatusBadge.ForeColor = Theme.Green;
                }
                else
                {
                    lblStatusBadge.Text = "STATUS: CALIBRATION REQUIRED";
                    lblStatusBadge.BackColor = Color.FromArgb(60, 45, 0);
                    lblStatusBadge.ForeColor = Theme.AccentOrange;
                }

                lblVisualizerStatus.Text = "AWAITING ROTATION";
                lblVisualizerStatus.ForeColor = Color.FromArgb(120, 130, 140);

                btnHorizonStart.Text = "START CALIBRATION";
                btnHorizonStart.Enabled = true;
                btnHorizonStart.BackColor = Theme.AccentOrange;
                btnHorizonCancel.Enabled = false;

                pnlProgressBarFill.Width = 0;
                lblProgressPct.Text = "0%";
                lblSamplesCollected.Text = "0 / 400";
            }

            // Force repaint on procedure circles to toggle colors
            lblStep1Circle.Invalidate();
            lblStep2Circle.Invalidate();
            lblStep3Circle.Invalidate();
        }

        private void but_largemagcal_Click(object sender, EventArgs e)
        {
        }
    }
}