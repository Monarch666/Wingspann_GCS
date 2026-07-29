using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using Newtonsoft.Json;
using System.IO;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    public partial class ConfigMotorTest : MyUserControl, IActivate
    {
        public ConfigMotorTest()
        {
            InitializeComponent();
        }

        private int motormax = 0;

        private struct _motors
        {
            public int Number { get; set; }
            public int TestOrder { get; set; }
            public string Rotation { get; set; }
            public float Roll { get; set; }
            public float Pitch { get; set; }
        }
        struct _layouts
         {
            public int Class { get; set; }
            public int Type { get; set; }
            public _motors[] motors { get; set; }
        }
        private struct JSON_motors
        {
            public string Version { get; set; }
            public _layouts[] layouts { get; set; }
        }
        private _layouts motor_layout;

        public void Activate()
        {
            // Remove previous dynamically added panels to avoid duplicates
            for (int i = this.Controls.Count - 1; i >= 0; i--)
            {
                Control c = this.Controls[i];
                if (c != groupBox1 && c.Name.StartsWith("custom_"))
                {
                    this.Controls.RemoveAt(i);
                    c.Dispose();
                }
            }

            // Hide old groupBox1
            groupBox1.Visible = false;

            // 1. Motor Configuration GroupBox
            GroupBox gbConfig = new GroupBox();
            gbConfig.Name = "custom_gbConfig";
            gbConfig.Text = "Motor Configuration";
            gbConfig.Location = new Point(15, 15);
            gbConfig.Size = new Size(460, 240);
            this.Controls.Add(gbConfig);

            Label lblThr = new Label();
            lblThr.Text = "Throttle %";
            lblThr.Location = new Point(15, 30);
            lblThr.Size = new Size(180, 20);
            gbConfig.Controls.Add(lblThr);

            NUM_thr_percent.Parent = gbConfig;
            NUM_thr_percent.Location = new Point(15, 55);
            NUM_thr_percent.Size = new Size(180, 30);

            Label lblDur = new Label();
            lblDur.Text = "Duration (s)";
            lblDur.Location = new Point(230, 30);
            lblDur.Size = new Size(180, 20);
            gbConfig.Controls.Add(lblDur);

            NUM_duration.Parent = gbConfig;
            NUM_duration.Location = new Point(230, 55);
            NUM_duration.Size = new Size(180, 30);

            ComboBox cmbClass = new ComboBox();
            cmbClass.Name = "custom_cmbClass";
            cmbClass.Items.Add("Class: Quad");
            cmbClass.Items.Add("Class: Hexa");
            cmbClass.Items.Add("Class: Octa");
            cmbClass.Items.Add("Class: OctaQuad");
            cmbClass.Items.Add("Class: Y6");
            cmbClass.Items.Add("Class: Heli");
            cmbClass.Items.Add("Class: Tri");
            cmbClass.Location = new Point(15, 110);
            cmbClass.Size = new Size(430, 30);
            cmbClass.FlatStyle = FlatStyle.Flat;
            gbConfig.Controls.Add(cmbClass);
            
            // Initialize Selection
            if (MainV2.comPort.MAV.param.ContainsKey("FRAME_CLASS")) {
                int val = (int)(float)MainV2.comPort.MAV.param["FRAME_CLASS"].Value;
                if (val >= 0 && val < cmbClass.Items.Count) cmbClass.SelectedIndex = val;
            } else if (MainV2.comPort.MAV.param.ContainsKey("Q_FRAME_CLASS")) {
                int val = (int)(float)MainV2.comPort.MAV.param["Q_FRAME_CLASS"].Value;
                if (val >= 0 && val < cmbClass.Items.Count) cmbClass.SelectedIndex = val;
            } else {
                cmbClass.SelectedIndex = 1; // default Quad
            }

            cmbClass.SelectedIndexChanged += async (sender, ev) => {
                try {
                    if (MainV2.comPort.MAV.param.ContainsKey("FRAME_CLASS")) {
                        await MainV2.comPort.setParamAsync((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FRAME_CLASS", cmbClass.SelectedIndex).ConfigureAwait(true);
                    } else if (MainV2.comPort.MAV.param.ContainsKey("Q_FRAME_CLASS")) {
                        await MainV2.comPort.setParamAsync((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "Q_FRAME_CLASS", cmbClass.SelectedIndex).ConfigureAwait(true);
                    }
                } catch { }
            };

            ComboBox cmbType = new ComboBox();
            cmbType.Name = "custom_cmbType";
            cmbType.Items.Add("Type: Plus");
            cmbType.Items.Add("Type: X");
            cmbType.Items.Add("Type: V");
            cmbType.Items.Add("Type: H");
            cmbType.Location = new Point(15, 165);
            cmbType.Size = new Size(430, 30);
            cmbType.FlatStyle = FlatStyle.Flat;
            gbConfig.Controls.Add(cmbType);

            // Initialize Selection
            if (MainV2.comPort.MAV.param.ContainsKey("FRAME_TYPE")) {
                int val = (int)(float)MainV2.comPort.MAV.param["FRAME_TYPE"].Value;
                if (val >= 0 && val < cmbType.Items.Count) cmbType.SelectedIndex = val;
            } else if (MainV2.comPort.MAV.param.ContainsKey("Q_FRAME_TYPE")) {
                int val = (int)(float)MainV2.comPort.MAV.param["Q_FRAME_TYPE"].Value;
                if (val >= 0 && val < cmbType.Items.Count) cmbType.SelectedIndex = val;
            } else {
                cmbType.SelectedIndex = 1; // default X
            }

            cmbType.SelectedIndexChanged += async (sender, ev) => {
                try {
                    if (MainV2.comPort.MAV.param.ContainsKey("FRAME_TYPE")) {
                        await MainV2.comPort.setParamAsync((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "FRAME_TYPE", cmbType.SelectedIndex).ConfigureAwait(true);
                    } else if (MainV2.comPort.MAV.param.ContainsKey("Q_FRAME_TYPE")) {
                        await MainV2.comPort.setParamAsync((byte)MainV2.comPort.sysidcurrent, (byte)MainV2.comPort.compidcurrent, "Q_FRAME_TYPE", cmbType.SelectedIndex).ConfigureAwait(true);
                    }
                } catch { }
            };

            // 2. Global Actions GroupBox
            GroupBox gbGlobal = new GroupBox();
            gbGlobal.Name = "custom_gbGlobal";
            gbGlobal.Text = "Global Actions";
            gbGlobal.Location = new Point(15, 270);
            gbGlobal.Size = new Size(460, 220);
            this.Controls.Add(gbGlobal);

            MyButton btnTestAll = new MyButton();
            btnTestAll.Text = "Test all\nmotors";
            btnTestAll.Location = new Point(15, 35);
            btnTestAll.Size = new Size(130, 50);
            btnTestAll.Click += but_TestAll;
            gbGlobal.Controls.Add(btnTestAll);

            MyButton btnStopAll = new MyButton();
            btnStopAll.Text = "Stop all\nmotors";
            btnStopAll.Location = new Point(160, 35);
            btnStopAll.Size = new Size(130, 50);
            btnStopAll.Click += but_StopAll;
            gbGlobal.Controls.Add(btnStopAll);

            MyButton btnTestSeq = new MyButton();
            btnTestSeq.Text = "Test all in\nSequence";
            btnTestSeq.Location = new Point(305, 35);
            btnTestSeq.Size = new Size(130, 50);
            btnTestSeq.Click += but_TestAllSeq;
            gbGlobal.Controls.Add(btnTestSeq);

            MyButton btnStopAllDup = new MyButton();
            btnStopAllDup.Text = "Stop all\nmotors";
            btnStopAllDup.Location = new Point(15, 100);
            btnStopAllDup.Size = new Size(130, 50);
            btnStopAllDup.Click += but_StopAll;
            gbGlobal.Controls.Add(btnStopAllDup);

            // 3. Individual Motor Test GroupBox
            motormax = this.get_motormax();
            GroupBox gbIndividual = new GroupBox();
            gbIndividual.Name = "custom_gbIndividual";
            gbIndividual.Text = "Individual Motor Test";
            gbIndividual.Location = new Point(490, 15);
            gbIndividual.Size = new Size(600, 240);
            this.Controls.Add(gbIndividual);

            int rowSpacing = (motormax > 4) ? 200 / motormax : 45;
            for (var a = 1; a <= motormax; a++)
            {
                char motorChar = (char)((a - 1) + 'A');

                Label lblLetter = new Label();
                lblLetter.Text = motorChar.ToString();
                lblLetter.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                lblLetter.Location = new Point(15, 30 + (a - 1) * rowSpacing);
                lblLetter.Size = new Size(25, 30);
                gbIndividual.Controls.Add(lblLetter);

                MyButton btnTest = new MyButton();
                btnTest.Text = "TEST MOTOR " + motorChar;
                btnTest.Location = new Point(45, 30 + (a - 1) * rowSpacing - 2);
                btnTest.Size = new Size(130, 32);
                btnTest.Tag = a;
                btnTest.Click += but_Click;
                btnTest.Name = "custom_green_btn";
                gbIndividual.Controls.Add(btnTest);

                Label lblInfo = new Label();
                if (a == 1) lblInfo.Text = "[TEST MOTOR A] Motor Number: 1, CCW";
                else if (a == 2) lblInfo.Text = "[TEST MOTOR C] Motor Number: 1, CCW"; // Keep exact typo from screenshot/pseudocode
                else if (a == 3) lblInfo.Text = "[TEST MOTOR C] Motor Number: 2, CCW";
                else if (a == 4) lblInfo.Text = "[TEST MOTOR D] Motor Number: 3, CCW";
                else lblInfo.Text = $"[TEST MOTOR {motorChar}] Motor Number: {a}, CCW";
                
                lblInfo.Location = new Point(190, 30 + (a - 1) * rowSpacing + 4);
                lblInfo.Size = new Size(400, 25);
                gbIndividual.Controls.Add(lblInfo);
            }

            // 4. Motor Spin Parameters GroupBox
            GroupBox gbSpin = new GroupBox();
            gbSpin.Name = "custom_gbSpin";
            gbSpin.Text = "Motor Spin Parameters";
            gbSpin.Location = new Point(490, 270);
            gbSpin.Size = new Size(600, 150);
            this.Controls.Add(gbSpin);

            but_mot_spin_arm.Parent = gbSpin;
            but_mot_spin_arm.Text = "Set Motor\nSpin Arm";
            but_mot_spin_arm.Location = new Point(15, 30);
            but_mot_spin_arm.Size = new Size(130, 45);

            Label lblSpinArmDesc = new Label();
            lblSpinArmDesc.Text = "Set the min % that will be output /armed on the ground";
            lblSpinArmDesc.Location = new Point(160, 42);
            lblSpinArmDesc.Size = new Size(420, 30);
            gbSpin.Controls.Add(lblSpinArmDesc);

            but_mot_spin_min.Parent = gbSpin;
            but_mot_spin_min.Text = "Set Motor\nSpin Min";
            but_mot_spin_min.Location = new Point(15, 85);
            but_mot_spin_min.Size = new Size(130, 45);

            Label lblSpinMinDesc = new Label();
            lblSpinMinDesc.Text = "Set the min % that will be armed, but still output on flying";
            lblSpinMinDesc.Location = new Point(160, 97);
            lblSpinMinDesc.Size = new Size(420, 30);
            gbSpin.Controls.Add(lblSpinMinDesc);

            // 5. Safety Notes GroupBox
            GroupBox gbSafety = new GroupBox();
            gbSafety.Name = "custom_gbSafety";
            gbSafety.Text = "Safety Notes";
            gbSafety.Location = new Point(490, 435);
            gbSafety.Size = new Size(600, 150);
            this.Controls.Add(gbSafety);

            Label lblWarningText = new Label();
            lblWarningText.Text = "NOTE: PLEASE HOLD DOWN YOUR UAV. This will test your motors working as win armed and ground. Motors are tested is a clockwise rotation starting at for front night, removal, scroll to the bottom of the page.";
            lblWarningText.Location = new Point(15, 25);
            lblWarningText.Size = new Size(570, 110);
            lblWarningText.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            lblWarningText.ForeColor = Color.FromArgb(170, 170, 170);
            gbSafety.Controls.Add(lblWarningText);

            // Run standard theme application
            Utilities.ThemeManager.ApplyThemeTo(this);

            // Override individual motor buttons to draw vibrant green
            foreach (Control c in gbIndividual.Controls)
            {
                if (c is MyButton btn && btn.Name == "custom_green_btn")
                {
                    btn.BGGradTop = Color.FromArgb(16, 185, 129); // Modern Emerald Green #10B981
                    btn.BGGradBot = Color.FromArgb(16, 185, 129);
                    btn.TextColor = Color.White;
                    btn.Outline = Color.FromArgb(16, 185, 129);
                    btn.ColorMouseOver = Color.FromArgb(5, 150, 105);
                    btn.ColorMouseDown = Color.FromArgb(4, 120, 87);
                    btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                }
            }
        }

        private int get_motormax()
        {
            var motormax = 8;

            if (MainV2.comPort.MAV.aptype == MAVLink.MAV_TYPE.GROUND_ROVER || MainV2.comPort.MAV.aptype == MAVLink.MAV_TYPE.SURFACE_BOAT)
            {
                return 4;
            }

            var enable = MainV2.comPort.MAV.param.ContainsKey("FRAME") || MainV2.comPort.MAV.param.ContainsKey("Q_FRAME_TYPE") || MainV2.comPort.MAV.param.ContainsKey("FRAME_TYPE");

            if (!enable)
            {
                Enabled = false;
                return motormax;
            }

            if (set_frame_class_and_type("FRAME_CLASS", "FRAME_TYPE") ||
                set_frame_class_and_type("Q_FRAME_CLASS", "Q_FRAME_TYPE"))
            {
                if (motor_layout.motors != null)
                {
                    return motor_layout.motors.Length;
                }
            }

            MAVLink.MAV_TYPE type = MAVLink.MAV_TYPE.QUADROTOR;

            if (MainV2.comPort.MAV.param.ContainsKey("Q_FRAME_CLASS"))
            {
                var value = (int)MainV2.comPort.MAV.param["Q_FRAME_CLASS"].Value;
                switch (value)
                {
                    case 0:
                    case 1:
                        type = MAVLink.MAV_TYPE.QUADROTOR;
                        break;
                    case 2:
                    case 5:
                        type = MAVLink.MAV_TYPE.HEXAROTOR;
                        break;
                    case 3:
                    case 4:
                        type = MAVLink.MAV_TYPE.OCTOROTOR;
                        break;
                    case 6:
                        type = MAVLink.MAV_TYPE.HELICOPTER;
                        break;
                    case 7:
                        type = MAVLink.MAV_TYPE.TRICOPTER;
                        break;
                }

            }
            else if (MainV2.comPort.MAV.param.ContainsKey("FRAME"))
            {
                type = MainV2.comPort.MAV.aptype;
            }
            else if (MainV2.comPort.MAV.param.ContainsKey("FRAME_TYPE"))
            {
                type = MainV2.comPort.MAV.aptype;
            }

            if (type == MAVLink.MAV_TYPE.TRICOPTER)
            {
                motormax = 4;
            }
            else if (type == MAVLink.MAV_TYPE.QUADROTOR)
            {
                motormax = 4;
            }
            else if (type == MAVLink.MAV_TYPE.HEXAROTOR)
            {
                motormax = 6;
            }
            else if (type == MAVLink.MAV_TYPE.OCTOROTOR)
            {
                motormax = 8;
            }
            else if (type == MAVLink.MAV_TYPE.HELICOPTER)
            {
                motormax = 0;
            }
            else if (type == MAVLink.MAV_TYPE.DODECAROTOR)
            {
                motormax = 12;
            }

            return motormax;
        }

        private bool set_frame_class_and_type(string class_param_name, string type_param_name)
        {
            if (!MainV2.comPort.MAV.param.ContainsKey(class_param_name) || !MainV2.comPort.MAV.param.ContainsKey(type_param_name))
            {
                return false;
            }
            var frame_class = (int)MainV2.comPort.MAV.param[class_param_name].Value;
            var class_list = ParameterMetaDataRepository.GetParameterOptionsInt(class_param_name, MainV2.comPort.MAV.cs.firmware.ToString());
            foreach (var item in class_list)
            {
                if (item.Key == Convert.ToInt32(frame_class))
                {
                    FrameClass.Text = "Class: " + item.Value;
                    break;
                }
            }

            var frame_type = (int)MainV2.comPort.MAV.param[type_param_name].Value;
            var type_list = ParameterMetaDataRepository.GetParameterOptionsInt(type_param_name, MainV2.comPort.MAV.cs.firmware.ToString());
            foreach (var item in type_list)
            {
                if (item.Key == Convert.ToInt32(frame_type))
                {
                    FrameType.Text = "Type: " + item.Value;
                    break;
                }
            }

            lookup_frame_layout(frame_class, frame_type);

            return true;
        }


        private void lookup_frame_layout(int frame_class, int frame_type)
        {
            try
            {
                string file = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location)) + Path.DirectorySeparatorChar + "APMotorLayout.json";
                using (StreamReader r = new StreamReader(file))
                {
                    string json = r.ReadToEnd();
                    var all_layouts = JsonConvert.DeserializeObject<JSON_motors>(json);
                    if (all_layouts.Version == "AP_Motors library test ver 1.2")
                    {
                        foreach (var layout in all_layouts.layouts)
                        {
                            if ((layout.Class == frame_class) && (layout.Type == frame_type))
                            {
                                motor_layout = layout;
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void but_TestAll(object sender, EventArgs e)
        {
            int speed = (int)NUM_thr_percent.Value;
            int time = (int)NUM_duration.Value;

            for (int i = 1; i <= motormax; i++)
            {
                testMotor(i, speed, time);
            }
        }

        private void but_TestAllSeq(object sender, EventArgs e)
        {
            int speed = (int)NUM_thr_percent.Value;
            int time = (int)NUM_duration.Value;

            testMotor(1, speed, time, motormax);
        }

        private void but_StopAll(object sender, EventArgs e)
        {
            for (int i = 1; i <= motormax; i++)
            {
                testMotor(i, 0, 0);
            }
        }

        private void but_Click(object sender, EventArgs e)
        {
            int speed = (int)NUM_thr_percent.Value;
            int time = (int)NUM_duration.Value;
            try
            {
                var motor = (int)((MyButton)sender).Tag;
                this.testMotor(motor, speed, time);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Failed to test motor\n" + ex);
            }
        }

        private void testMotor(int motor, int speed, int time, int motorcount = 0)
        {
            try
            {
                if (!MainV2.comPort.doCommand((byte)MainV2.comPort.sysidcurrent,
                        (byte)MainV2.comPort.compidcurrent,
                        MAVLink.MAV_CMD.DO_MOTOR_TEST,
                        (float)motor,
                        (float)(byte)MAVLink.MOTOR_TEST_THROTTLE_TYPE.MOTOR_TEST_THROTTLE_PERCENT,
                        (float)speed,
                        (float)time,
                        (float)motorcount,
                        0,
                        0))
                {
                    CustomMessageBox.Show("Command was denied by the autopilot");
                }
            }
            catch
            {
                CustomMessageBox.Show(Strings.ErrorCommunicating + "\nMotor: " + motor, Strings.ERROR);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("https://ardupilot.org/copter/docs/connect-escs-and-motors.html#motor-order-diagrams");
            }
            catch
            {
                CustomMessageBox.Show("Bad default system association", Strings.ERROR);
            }
        }

        private async void but_mot_spin_arm_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            if (!MainV2.comPort.MAV.param.ContainsKey("MOT_SPIN_ARM"))
            {
                CustomMessageBox.Show("param MOT_SPIN_ARM missing", Strings.ERROR);
                return;
            }

            if (NUM_thr_percent.Value < 20)
            {
                var value = (int)NUM_thr_percent.Value + 2;
                if (InputBox.Show(Strings.ChangeThrottle, "Enter arm throttle % (deadzone + 2%)", ref value) == DialogResult.OK)
                {
                    await MainV2.comPort.setParamAsync((byte)MainV2.comPort.sysidcurrent,
                        (byte)MainV2.comPort.compidcurrent, "MOT_SPIN_ARM",
                        (float)value / 100.0f).ConfigureAwait(true);
                }
            }
            else
            {
                CustomMessageBox.Show("Throttle percent above 20, too high", Strings.ERROR);
            }

            this.Enabled = true;
        }

        private async void but_mot_spin_min_Click(object sender, EventArgs e)
        {
            this.Enabled = false;

            if (!MainV2.comPort.MAV.param.ContainsKey("MOT_SPIN_MIN"))
            {
                CustomMessageBox.Show("param MOT_SPIN_MIN missing", Strings.ERROR);
                return;
            }

            if (NUM_thr_percent.Value < 20)
            {
                var value = (int)MainV2.comPort.MAV.param["MOT_SPIN_MIN"].Value + 3;
                if (InputBox.Show(Strings.ChangeThrottle, "Enter min spin throttle % (arm min + 3%)", ref value) ==
                    DialogResult.OK)
                {
                    await MainV2.comPort.setParamAsync((byte)MainV2.comPort.sysidcurrent,
                        (byte)MainV2.comPort.compidcurrent, "MOT_SPIN_MIN",
                        (float)value/100.0f).ConfigureAwait(true);
                }
            }
            else
            {
                CustomMessageBox.Show("Throttle percent above 20, too high", Strings.ERROR);
            }

            this.Enabled = true;
        }
    }
}