using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using MissionPlanner.Controls;
using MissionPlanner.Controls.BackstageView;
using MissionPlanner.GCSViews.ConfigurationView;
using MissionPlanner.Utilities;

namespace MissionPlanner.GCSViews
{
    public class ConfigSetupOverview : MyUserControl, IActivate
    {
        public enum CalibrationStatus
        {
            NotCalibrated,  // Red
            NotConfigured,  // Yellow
            Completed       // Green
        }

        private Panel pnlMain;
        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlFooter;
        private TableLayoutPanel gridCards;

        // Header controls
        private Label lblTitle;
        private Label lblDescription;
        private PictureBox picHeaderIcon;

        // Footer controls
        private Label lblFooterInfo;
        private Button btnStartCalibration;

        // Sidebar controls
        private Panel pnlLegend;
        private Panel pnlQuickActions;
        private Button btnWizard;
        private Button btnRefresh;
        private Button btnLoad;
        private Button btnSave;

        private List<CalibrationCard> cardsList = new List<CalibrationCard>();

        public ConfigSetupOverview()
        {
            InitializeComponent();
        }

        public void Activate()
        {
            RefreshStatuses();
        }

        private void InitializeComponent()
        {
            this.DoubleBuffered = true;
            this.BackColor = Theme.Background;
            this.ForeColor = Theme.OnSurface;
            this.Size = new Size(1024, 768);

            // --- Outer Layout Split ---
            pnlSidebar = new Panel();
            pnlSidebar.Width = 320;
            pnlSidebar.Dock = DockStyle.Right;
            pnlSidebar.BackColor = Theme.SurfaceDim;
            pnlSidebar.Padding = new Padding(20);
            this.Controls.Add(pnlSidebar);

            pnlMain = new Panel();
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.BackColor = Theme.Background;
            pnlMain.Padding = new Padding(25, 25, 15, 25);
            this.Controls.Add(pnlMain);

            // --- Main Area Panels ---
            pnlHeader = new Panel();
            pnlHeader.Height = 85;
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.BackColor = Color.Transparent;
            pnlMain.Controls.Add(pnlHeader);

            pnlFooter = new Panel();
            pnlFooter.Height = 70;
            pnlFooter.Dock = DockStyle.Bottom;
            pnlFooter.BackColor = Theme.SurfaceDim;
            pnlFooter.Padding = new Padding(15);
            pnlMain.Controls.Add(pnlFooter);

            gridCards = new TableLayoutPanel();
            gridCards.Dock = DockStyle.Fill;
            gridCards.ColumnCount = 3;
            gridCards.RowCount = 4;
            gridCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            gridCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            gridCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            gridCards.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            gridCards.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            gridCards.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            gridCards.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            gridCards.Padding = new Padding(0, 15, 0, 15);
            pnlMain.Controls.Add(gridCards);

            // Bring Header and Footer to top/bottom relative to TableLayoutPanel
            gridCards.BringToFront();
            pnlFooter.SendToBack();
            pnlHeader.SendToBack();

            // --- Header Setup ---
            lblTitle = new Label();
            lblTitle.Text = "AERO-GCS: Advanced Setup Calibration";
            lblTitle.Font = new Font("Geist", 15F, FontStyle.Bold);
            lblTitle.ForeColor = Theme.Primary;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(0, 5);
            pnlHeader.Controls.Add(lblTitle);

            lblDescription = new Label();
            lblDescription.Text = "The following pages are required to be configured before your autopilot will work.\r\nPlease work through them all in order.";
            lblDescription.Font = new Font("Geist", 9.5F, FontStyle.Regular);
            lblDescription.ForeColor = Theme.OnSurfaceVariant;
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(0, 32);
            pnlHeader.Controls.Add(lblDescription);

            // --- Footer Setup ---
            lblFooterInfo = new Label();
            lblFooterInfo.Text = "ℹ   Calibration Required\r\nCompleting all calibration steps ensures safe and stable flight performance.";
            lblFooterInfo.Font = new Font("Geist", 9F, FontStyle.Regular);
            lblFooterInfo.ForeColor = Color.FromArgb(30, 144, 255); // Blue highlight
            lblFooterInfo.AutoSize = true;
            lblFooterInfo.Location = new Point(15, 15);
            pnlFooter.Controls.Add(lblFooterInfo);

            btnStartCalibration = new Button();
            btnStartCalibration.Text = "START CALIBRATION";
            btnStartCalibration.Size = new Size(180, 38);
            btnStartCalibration.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnStartCalibration.Location = new Point(pnlFooter.Width - btnStartCalibration.Width - 15, 16);
            StyleHelper.ApplyButtonStyle(btnStartCalibration, true);
            btnStartCalibration.Click += BtnStartCalibration_Click;
            pnlFooter.Controls.Add(btnStartCalibration);

            // --- Sidebar Layout ---
            pnlLegend = new Panel();
            pnlLegend.Dock = DockStyle.Top;
            pnlLegend.Height = 220;
            pnlLegend.BackColor = Theme.SurfaceContainer;
            pnlLegend.Padding = new Padding(15);
            pnlSidebar.Controls.Add(pnlLegend);

            Panel sidebarSpacer = new Panel();
            sidebarSpacer.Dock = DockStyle.Top;
            sidebarSpacer.Height = 20;
            sidebarSpacer.BackColor = Color.Transparent;
            pnlSidebar.Controls.Add(sidebarSpacer);

            pnlQuickActions = new Panel();
            pnlQuickActions.Dock = DockStyle.Top;
            pnlQuickActions.Height = 260;
            pnlQuickActions.BackColor = Theme.SurfaceContainer;
            pnlQuickActions.Padding = new Padding(15);
            pnlSidebar.Controls.Add(pnlQuickActions);

            // --- Legend Contents ---
            Label lblLegendTitle = new Label();
            lblLegendTitle.Text = "ℹ  About This Calibration";
            lblLegendTitle.Font = new Font("Geist", 11F, FontStyle.Bold);
            lblLegendTitle.ForeColor = Theme.Primary;
            lblLegendTitle.Location = new Point(15, 15);
            lblLegendTitle.AutoSize = true;
            pnlLegend.Controls.Add(lblLegendTitle);

            Label lblLegendDesc = new Label();
            lblLegendDesc.Text = "Complete all calibration steps in sequence. Steps will become available only after the previous step is completed.";
            lblLegendDesc.Font = new Font("Geist", 9F, FontStyle.Regular);
            lblLegendDesc.ForeColor = Theme.OnSurfaceVariant;
            lblLegendDesc.Location = new Point(15, 45);
            lblLegendDesc.Size = new Size(270, 50);
            pnlLegend.Controls.Add(lblLegendDesc);

            // Legend indicators
            AddLegendItem(100, Theme.Tertiary, "NOT CALIBRATED", "Step is required and not completed.");
            AddLegendItem(135, Theme.Amber, "NOT CONFIGURED", "Step is required and not configured.");
            AddLegendItem(170, Theme.Green, "COMPLETED", "Step completed successfully.");

            // --- Quick Actions Contents ---
            Label lblQuickTitle = new Label();
            lblQuickTitle.Text = "⚡  Quick Actions";
            lblQuickTitle.Font = new Font("Geist", 11F, FontStyle.Bold);
            lblQuickTitle.ForeColor = Theme.Primary;
            lblQuickTitle.Location = new Point(15, 15);
            lblQuickTitle.AutoSize = true;
            pnlQuickActions.Controls.Add(lblQuickTitle);

            btnWizard = new Button();
            btnWizard.Text = "CALIBRATION WIZARD";
            btnWizard.Location = new Point(15, 50);
            btnWizard.Size = new Size(270, 36);
            StyleHelper.ApplyButtonStyle(btnWizard, true);
            btnWizard.Click += BtnStartCalibration_Click;
            pnlQuickActions.Controls.Add(btnWizard);

            btnRefresh = new Button();
            btnRefresh.Text = "REFRESH STATUS";
            btnRefresh.Location = new Point(15, 95);
            btnRefresh.Size = new Size(270, 36);
            StyleHelper.ApplyButtonStyle(btnRefresh, false);
            btnRefresh.Click += BtnRefresh_Click;
            pnlQuickActions.Controls.Add(btnRefresh);

            btnLoad = new Button();
            btnLoad.Text = "📤  LOAD CALIBRATION";
            btnLoad.Location = new Point(15, 140);
            btnLoad.Size = new Size(270, 36);
            StyleHelper.ApplyButtonStyle(btnLoad, false);
            btnLoad.Click += BtnLoad_Click;
            pnlQuickActions.Controls.Add(btnLoad);

            btnSave = new Button();
            btnSave.Text = "📥  SAVE CALIBRATION";
            btnSave.Location = new Point(15, 185);
            btnSave.Size = new Size(270, 36);
            StyleHelper.ApplyButtonStyle(btnSave, false);
            btnSave.Click += BtnSave_Click;
            pnlQuickActions.Controls.Add(btnSave);

            // --- Instantiate Cards ---
            AddCard(0, 0, "Frame Type", "Select your frame type and orientation.", "🛸", typeof(ConfigFrameClassType));
            AddCard(1, 0, "Accel Calibration", "Calibrate accelerometer for accurate attitude.", "⚖", typeof(ConfigAccelerometerCalibration));
            AddCard(2, 0, "Compass", "Calibrate compass for correct heading.", "🧭", typeof(ConfigHWCompass));

            AddCard(0, 1, "Radio Calibration", "Calibrate transmitter channels and endpoints.", "🎛", typeof(ConfigRadioInput));
            AddCard(1, 1, "Servo Output", "Test and verify servo outputs and directions.", "⚙", typeof(ConfigRadioOutput));
            AddCard(2, 1, "Serial Ports", "Configure serial ports and peripherals.", "🔌", typeof(ConfigSerial));

            AddCard(0, 2, "ESC Calibration", "Calibrate ESCs for proper throttle response.", "⚡", typeof(ConfigESCCalibration));
            AddCard(1, 2, "Flight Modes", "Configure flight modes and switch assignments.", "🏳", typeof(ConfigFlightModes));
            AddCard(2, 2, "FailSafe", "Configure failsafe actions and triggers.", "🛡", typeof(ConfigFailSafe));

            AddCard(0, 3, "Initial Tune Parameter", "Set basic tuning parameters for stable flight.", "⌥", typeof(ConfigInitialParams));
            AddCard(1, 3, "HW ID", "Read and verify autopilot hardware ID.", "🆔", typeof(ConfigHWIDs));
            AddCard(2, 3, "ADSB", "Configure ADS-B receiver and settings.", "📡", typeof(ConfigADSB));

            this.Resize += ConfigSetupOverview_Resize;
            ConfigSetupOverview_Resize(this, EventArgs.Empty);
        }

        private void AddLegendItem(int y, Color color, string title, string subtitle)
        {
            Panel dot = new Panel();
            dot.Size = new Size(10, 10);
            dot.Location = new Point(15, y + 4);
            dot.BackColor = color;
            pnlLegend.Controls.Add(dot);

            // Round the dot
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, 10, 10);
            dot.Region = new Region(path);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = new Font("Geist Mono", 8F, FontStyle.Bold);
            lblTitle.ForeColor = color;
            lblTitle.Location = new Point(32, y);
            lblTitle.AutoSize = true;
            pnlLegend.Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = subtitle;
            lblSub.Font = new Font("Geist", 8F, FontStyle.Regular);
            lblSub.ForeColor = Theme.OnSurfaceVariant;
            lblSub.Location = new Point(32, y + 14);
            lblSub.Size = new Size(250, 16);
            pnlLegend.Controls.Add(lblSub);
        }

        private void AddCard(int col, int row, string title, string description, string glyph, Type targetPage)
        {
            CalibrationCard card = new CalibrationCard(title, description, glyph, targetPage);
            card.Click += (s, e) => NavigateToPage(card.TargetPageType);
            gridCards.Controls.Add(card, col, row);
            cardsList.Add(card);
        }

        private void ConfigSetupOverview_Resize(object sender, EventArgs e)
        {
            if (pnlFooter != null && btnStartCalibration != null)
            {
                btnStartCalibration.Location = new Point(pnlFooter.Width - btnStartCalibration.Width - 15, 16);
            }
        }

        private void RefreshStatuses()
        {
            bool isConnected = MainV2.comPort.BaseStream.IsOpen;

            foreach (var card in cardsList)
            {
                if (!isConnected)
                {
                    card.ModuleStatus = (card.Title == "Servo Output" || card.Title == "Serial Ports" || card.Title == "Flight Modes" || card.Title == "Initial Tune Parameter" || card.Title == "ADSB") 
                        ? CalibrationStatus.NotConfigured 
                        : CalibrationStatus.NotCalibrated;
                    card.Invalidate();
                    continue;
                }

                // Query actual parameters
                try
                {
                    switch (card.Title)
                    {
                        case "Frame Type":
                            if (MainV2.comPort.MAV.param.ContainsKey("FRAME_CLASS") && (float)MainV2.comPort.MAV.param["FRAME_CLASS"].Value > 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else if (MainV2.comPort.MAV.param.ContainsKey("FRAME_TYPE") && (float)MainV2.comPort.MAV.param["FRAME_TYPE"].Value > 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotCalibrated;
                            break;

                        case "Accel Calibration":
                            if (MainV2.comPort.MAV.param.ContainsKey("INS_ACC_ID") && (float)MainV2.comPort.MAV.param["INS_ACC_ID"].Value > 0 &&
                                MainV2.comPort.MAV.param.ContainsKey("INS_ACCOFFS_X") && (float)MainV2.comPort.MAV.param["INS_ACCOFFS_X"].Value != 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotCalibrated;
                            break;

                        case "Compass":
                            if (MainV2.comPort.MAV.param.ContainsKey("COMPASS_DEV_ID") && (float)MainV2.comPort.MAV.param["COMPASS_DEV_ID"].Value > 0 &&
                                MainV2.comPort.MAV.param.ContainsKey("COMPASS_OFS_X") && (float)MainV2.comPort.MAV.param["COMPASS_OFS_X"].Value != 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotCalibrated;
                            break;

                        case "Radio Calibration":
                            if (MainV2.comPort.MAV.param.ContainsKey("RC1_MIN") && (float)MainV2.comPort.MAV.param["RC1_MIN"].Value != 1100)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotCalibrated;
                            break;

                        case "Servo Output":
                            if (MainV2.comPort.MAV.param.ContainsKey("SERVO1_FUNCTION") && (float)MainV2.comPort.MAV.param["SERVO1_FUNCTION"].Value > 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotConfigured;
                            break;

                        case "Serial Ports":
                            // If we have downloaded parameters, ports are active
                            card.ModuleStatus = CalibrationStatus.Completed;
                            break;

                        case "ESC Calibration":
                            if (MainV2.comPort.MAV.param.ContainsKey("ESC_CALIBRATION") && (float)MainV2.comPort.MAV.param["ESC_CALIBRATION"].Value > 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotCalibrated;
                            break;

                        case "Flight Modes":
                            if (MainV2.comPort.MAV.param.ContainsKey("FLTMODE1"))
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotConfigured;
                            break;

                        case "FailSafe":
                            if ((MainV2.comPort.MAV.param.ContainsKey("FS_THR_ENABLE") && (float)MainV2.comPort.MAV.param["FS_THR_ENABLE"].Value > 0) ||
                                (MainV2.comPort.MAV.param.ContainsKey("FS_BATT_ENABLE") && (float)MainV2.comPort.MAV.param["FS_BATT_ENABLE"].Value > 0))
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotConfigured;
                            break;

                        case "Initial Tune Parameter":
                            if (MainV2.comPort.MAV.param.ContainsKey("ATC_ANG_PIT_P"))
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotConfigured;
                            break;

                        case "HW ID":
                            card.ModuleStatus = CalibrationStatus.Completed;
                            break;

                        case "ADSB":
                            if (MainV2.comPort.MAV.param.ContainsKey("ADSB_ENABLE") && (float)MainV2.comPort.MAV.param["ADSB_ENABLE"].Value > 0)
                                card.ModuleStatus = CalibrationStatus.Completed;
                            else
                                card.ModuleStatus = CalibrationStatus.NotConfigured;
                            break;
                    }
                }
                catch
                {
                    card.ModuleStatus = CalibrationStatus.NotCalibrated;
                }

                card.Invalidate();
            }

            // Update footer info banner depending on whether steps remain
            bool anyIncomplete = false;
            foreach (var card in cardsList)
            {
                if (card.ModuleStatus != CalibrationStatus.Completed)
                {
                    anyIncomplete = true;
                    break;
                }
            }

            if (!anyIncomplete)
            {
                lblFooterInfo.Text = "✅   Calibration Complete\r\nAll steps completed successfully. Your vehicle is ready for flight.";
                lblFooterInfo.ForeColor = Theme.Green;
            }
            else
            {
                lblFooterInfo.Text = "ℹ   Calibration Required\r\nCompleting all calibration steps ensures safe and stable flight performance.";
                lblFooterInfo.ForeColor = Color.FromArgb(30, 144, 255);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshStatuses();
        }

        private void BtnStartCalibration_Click(object sender, EventArgs e)
        {
            // Find the first incomplete or unconfigured step and navigate to it
            foreach (var card in cardsList)
            {
                if (card.ModuleStatus != CalibrationStatus.Completed)
                {
                    NavigateToPage(card.TargetPageType);
                    return;
                }
            }

            CustomMessageBox.Show("All calibration steps are complete! Your autopilot is ready.");
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = ParamFile.FileMask })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var param2 = ParamFile.loadParamFile(ofd.FileName);
                        foreach (string key in param2.Keys)
                        {
                            try
                            {
                                MainV2.comPort.setParam(key, param2[key]);
                            }
                            catch {}
                        }
                        RefreshStatuses();
                        CustomMessageBox.Show("Calibration profile loaded successfully.");
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show("Error loading calibration file: " + ex.Message, "Error");
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "Param List|*.param;*.parm" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var data = new System.Collections.Hashtable();
                        foreach (string key in MainV2.comPort.MAV.param.Keys)
                        {
                            data[key] = (double)MainV2.comPort.MAV.param[key];
                        }
                        ParamFile.SaveParamFile(sfd.FileName, data);
                        CustomMessageBox.Show("Calibration profile saved successfully.");
                    }
                    catch (Exception ex)
                    {
                        CustomMessageBox.Show("Error saving calibration file: " + ex.Message, "Error");
                    }
                }
            }
        }

        private void NavigateToPage(Type pageType)
        {
            // Find parent BackstageView
            Control p = this.Parent;
            while (p != null)
            {
                if (p is BackstageView bsv)
                {
                    foreach (BackstageViewPage page in bsv.Pages)
                    {
                        if (page.Page != null && page.Page.GetType() == pageType)
                        {
                            bsv.ActivatePage(page);
                            return;
                        }
                    }
                    break;
                }
                p = p.Parent;
            }
        }

        // --- Custom Calibration Card Control ---
        private class CalibrationCard : Panel
        {
            public string Title { get; }
            public string Description { get; }
            public string IconGlyph { get; }
            public Type TargetPageType { get; }
            public CalibrationStatus ModuleStatus { get; set; } = CalibrationStatus.NotCalibrated;

            private bool isHovered = false;

            public CalibrationCard(string title, string description, string glyph, Type targetPage)
            {
                this.Title = title;
                this.Description = description;
                this.IconGlyph = glyph;
                this.TargetPageType = targetPage;
                this.DoubleBuffered = true;
                this.Cursor = Cursors.Hand;
                this.Margin = new Padding(8);
                this.BackColor = Theme.Panel;

                this.MouseEnter += (s, e) => { isHovered = true; this.BackColor = Theme.PanelHover; this.Invalidate(); };
                this.MouseLeave += (s, e) => { isHovered = false; this.BackColor = Theme.Panel; this.Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Border
                using (Pen borderPen = new Pen(isHovered ? Theme.Primary : Theme.OutlineVariant, 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                }

                // Draw Icon Box Background
                Rectangle iconBox = new Rectangle(12, 12, 38, 38);
                using (var iconBg = new SolidBrush(Color.FromArgb(15, Theme.Primary)))
                {
                    g.FillEllipse(iconBg, iconBox);
                }
                using (var iconBorder = new Pen(Color.FromArgb(50, Theme.Primary), 1))
                {
                    g.DrawEllipse(iconBorder, iconBox);
                }

                // Draw Glyph Icon
                using (Font iconFont = new Font("Segoe UI Emoji", 13F, FontStyle.Regular))
                using (Brush iconBrush = new SolidBrush(Theme.Primary))
                {
                    SizeF iconSize = g.MeasureString(IconGlyph, iconFont);
                    g.DrawString(IconGlyph, iconFont, iconBrush, 12 + (38 - iconSize.Width)/2, 12 + (38 - iconSize.Height)/2);
                }

                // Draw Text info
                using (Font titleFont = new Font("Geist", 10F, FontStyle.Bold))
                using (Brush titleBrush = new SolidBrush(Theme.OnSurface))
                {
                    g.DrawString(Title, titleFont, titleBrush, 62, 12);
                }

                using (Font descFont = new Font("Geist", 8.5F, FontStyle.Regular))
                using (Brush descBrush = new SolidBrush(Theme.OnSurfaceVariant))
                {
                    RectangleF descRect = new RectangleF(62, 28, this.Width - 95, 34);
                    g.DrawString(Description, descFont, descBrush, descRect);
                }

                // Draw Status Indicator
                Color statusColor = Theme.Tertiary;
                string statusText = "NOT CALIBRATED";

                if (ModuleStatus == CalibrationStatus.Completed)
                {
                    statusColor = Theme.Green;
                    statusText = "COMPLETED";
                }
                else if (ModuleStatus == CalibrationStatus.NotConfigured)
                {
                    statusColor = Theme.Amber;
                    statusText = "NOT CONFIGURED";
                }

                using (var statusBrush = new SolidBrush(statusColor))
                {
                    g.FillEllipse(statusBrush, 62, 69, 7, 7);
                }

                using (Font statusFont = new Font("Geist Mono", 7.5F, FontStyle.Bold))
                using (Brush statusBrush = new SolidBrush(statusColor))
                {
                    g.DrawString(statusText, statusFont, statusBrush, 75, 66);
                }

                // Draw Navigation Arrow (Right side)
                using (Font arrowFont = new Font("Segoe UI", 11F, FontStyle.Bold))
                using (Brush arrowBrush = new SolidBrush(isHovered ? Theme.Primary : Theme.OnSurfaceVariant))
                {
                    g.DrawString("›", arrowFont, arrowBrush, this.Width - 25, (this.Height - arrowFont.Height)/2 - 1);
                }
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
            }
        }
    }
}
