using System.Windows.Forms;

namespace MissionPlanner.GCSViews
{
    partial class FirmwareSetup
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.backstageView = new MissionPlanner.Controls.BackstageView.BackstageView();
            this.SuspendLayout();
            // 
            // backstageView
            // 
            this.backstageView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.backstageView.HighlightColor1 = System.Drawing.SystemColors.Highlight;
            this.backstageView.HighlightColor2 = System.Drawing.SystemColors.MenuHighlight;
            this.backstageView.Name = "backstageView";
            this.backstageView.WidthMenu = 190;
            // 
            // FirmwareSetup
            // 
            this.Controls.Add(this.backstageView);
            this.Name = "FirmwareSetup";
            this.Size = new System.Drawing.Size(800, 600);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FirmwareSetup_FormClosing);
            this.Load += new System.EventHandler(this.FirmwareSetup_Load);
            ApplyOdinFirmwareTheme();
            this.ResumeLayout(false);
        }

        private void ApplyOdinFirmwareTheme()
        {
            this.SuspendLayout();

            System.Drawing.Color bgColor = MainV2.OdinTheme.Background;
            System.Drawing.Color panelColor = MainV2.OdinTheme.Panel;
            System.Drawing.Color borderColor = MainV2.OdinTheme.Border;
            System.Drawing.Color textWhite = MainV2.OdinTheme.White;
            System.Drawing.Color sidebarBg = MainV2.OdinTheme.Background;
            System.Drawing.Color accentRed = MainV2.OdinTheme.Green;

            this.BackColor = bgColor;
            this.ForeColor = textWhite;

            this.backstageView.BackColor = bgColor;
            this.backstageView.ForeColor = textWhite;
            this.backstageView.ButtonsAreaBgColor = sidebarBg;
            this.backstageView.ButtonsAreaPencilColor = borderColor;
            this.backstageView.HighlightColor1 = accentRed;
            this.backstageView.HighlightColor2 = System.Drawing.Color.FromArgb(0, 80, 20);
            this.backstageView.WidthMenu = 190;

            // Add a header panel
            System.Windows.Forms.Panel headerPanel = new System.Windows.Forms.Panel();
            headerPanel.Name = "odinFirmwareHeader";
            headerPanel.Height = 40;
            headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            headerPanel.BackColor = bgColor;
            headerPanel.Padding = new System.Windows.Forms.Padding(12, 0, 0, 0);

            System.Windows.Forms.Panel accentLine = new System.Windows.Forms.Panel();
            accentLine.Width = 3;
            accentLine.Dock = System.Windows.Forms.DockStyle.Left;
            accentLine.BackColor = accentRed;
            headerPanel.Controls.Add(accentLine);

            System.Windows.Forms.Label headerLabel = new System.Windows.Forms.Label();
            headerLabel.Text = "  AERO-GCS: Hardware & Firmware";
            headerLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            headerLabel.ForeColor = textWhite;
            headerLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            headerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            headerPanel.Controls.Add(headerLabel);

            System.Windows.Forms.Panel separatorLine = new System.Windows.Forms.Panel();
            separatorLine.Height = 1;
            separatorLine.Dock = System.Windows.Forms.DockStyle.Bottom;
            separatorLine.BackColor = borderColor;
            headerPanel.Controls.Add(separatorLine);

            this.Controls.Add(headerPanel);
            headerPanel.SendToBack();

            this.ResumeLayout(false);
        }

        internal Controls.BackstageView.BackstageView backstageView;
    }
}
