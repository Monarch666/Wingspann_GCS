using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// Apex Control Style Helper — applies the Horizon GCS design system to WinForms controls.
    /// Follows the "No-Line Rule": separation via tonal layering, not explicit borders.
    /// </summary>
    public static class StyleHelper
    {
        // ── Panel Styles ────────────────────────────────────────────────

        /// <summary>
        /// Styles a panel with tonal layering, optional title, and L-bracket reticle corners.
        /// </summary>
        public static void ApplyPanelStyle(Panel p, string title, string sysTag)
        {
            p.BackColor = Theme.SurfaceContainer;
            p.Tag = $"{title}|{sysTag}";
            p.Paint += PanelPaintHandler;
        }

        private static void PanelPaintHandler(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;
            if (panel == null) return;

            string title = "";
            string sysTag = "";
            if (panel.Tag is string tagStr)
            {
                string[] parts = tagStr.Split('|');
                title = parts.Length > 0 ? parts[0] : "";
                sysTag = parts.Length > 1 ? parts[1] : "";
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = panel.Width;
            int h = panel.Height;

            // Draw L-bracket reticle corners (Data Grid Module pattern)
            int bracketLen = 16;
            int bracketThick = 2;
            using (var bracketPen = new Pen(Theme.OutlineVariant, bracketThick))
            {
                // Top-left L
                g.DrawLine(bracketPen, 0, 0, bracketLen, 0);
                g.DrawLine(bracketPen, 0, 0, 0, bracketLen);
                // Top-right L
                g.DrawLine(bracketPen, w - 1, 0, w - 1 - bracketLen, 0);
                g.DrawLine(bracketPen, w - 1, 0, w - 1, bracketLen);
                // Bottom-left L
                g.DrawLine(bracketPen, 0, h - 1, bracketLen, h - 1);
                g.DrawLine(bracketPen, 0, h - 1, 0, h - 1 - bracketLen);
                // Bottom-right L
                g.DrawLine(bracketPen, w - 1, h - 1, w - 1 - bracketLen, h - 1);
                g.DrawLine(bracketPen, w - 1, h - 1, w - 1, h - 1 - bracketLen);
            }

            // Render Title and System Tag in bracket notation
            if (!string.IsNullOrEmpty(title))
            {
                using (var titleBrush = new SolidBrush(Theme.OnSurface))
                using (var tagBrush = new SolidBrush(Theme.OnSurfaceVariant))
                {
                    g.DrawString(title, Theme.FontTitle, titleBrush, 16, 12);

                    if (!string.IsNullOrEmpty(sysTag))
                    {
                        string fullTag = $"[ {sysTag} ]";
                        SizeF tagSize = g.MeasureString(fullTag, Theme.FontLabel);
                        g.DrawString(fullTag, Theme.FontLabel, tagBrush, w - tagSize.Width - 16, 15);
                    }
                }
            }
        }

        // ── Button Styles ───────────────────────────────────────────────

        /// <summary>
        /// "Actuator" button style.
        /// Primary: solid green background + black text (high visibility).
        /// Secondary: ghost outline + green text.
        /// </summary>
        public static void ApplyButtonStyle(Button btn, bool isPrimary)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            if (btn is MissionPlanner.Controls.MyButton myBtn)
            {
                if (isPrimary)
                {
                    myBtn.BGGradTop = Theme.Primary;
                    myBtn.BGGradBot = Theme.PrimaryDim;
                    myBtn.TextColor = Color.Black;
                    myBtn.Outline = Theme.Outline;
                    myBtn.ColorMouseOver = Color.FromArgb(73, Theme.Primary);
                    myBtn.ColorMouseDown = Color.FromArgb(150, Theme.Primary);
                }
                else
                {
                    myBtn.BGGradTop = Color.Transparent;
                    myBtn.BGGradBot = Color.Transparent;
                    myBtn.TextColor = Theme.OnSurface;
                    myBtn.Outline = Theme.Outline;
                    myBtn.ColorMouseOver = Theme.SurfaceContainerHigh;
                    myBtn.ColorMouseDown = Theme.SurfaceContainerHighest;
                }
                return;
            }

            if (isPrimary)
            {
                btn.BackColor = Theme.Primary;
                btn.ForeColor = Color.Black;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Theme.PrimaryDim;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 200, 50);
            }
            else
            {
                // Ghost / outline style
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Theme.OnSurface;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Theme.Outline;
                btn.FlatAppearance.MouseOverBackColor = Theme.SurfaceContainerHigh;
                btn.FlatAppearance.MouseDownBackColor = Theme.SurfaceContainerHighest;
            }
        }

        /// <summary>
        /// Danger/destructive action button: red outline, red text.
        /// </summary>
        public static void ApplyDangerButtonStyle(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            if (btn is MissionPlanner.Controls.MyButton myBtn)
            {
                myBtn.BGGradTop = Color.Transparent;
                myBtn.BGGradBot = Color.Transparent;
                myBtn.TextColor = Theme.Tertiary;
                myBtn.Outline = Theme.Tertiary;
                myBtn.ColorMouseOver = Color.FromArgb(30, 255, 59, 48);
                myBtn.ColorMouseDown = Color.FromArgb(50, 255, 59, 48);
                return;
            }

            btn.BackColor = Color.Transparent;
            btn.ForeColor = Theme.Tertiary;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Theme.Tertiary;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 59, 48);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 255, 59, 48);
        }

        // ── Label Styles ────────────────────────────────────────────────

        public static void ApplyLabelStyle(Label lbl, Font font, Color color)
        {
            lbl.Font = font;
            lbl.ForeColor = color;
            lbl.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Uppercase monospace label (mimics cockpit instrumentation).
        /// </summary>
        public static void ApplySystemLabel(Label lbl, string text)
        {
            lbl.Text = text.ToUpperInvariant();
            lbl.Font = Theme.FontLabel;
            lbl.ForeColor = Theme.OnSurfaceVariant;
            lbl.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Large telemetry value display.
        /// </summary>
        public static void ApplyTelemetryValue(Label lbl)
        {
            lbl.Font = Theme.FontTelemetry;
            lbl.ForeColor = Theme.PrimaryText;
            lbl.BackColor = Color.Transparent;
        }

        // ── Checkbox ────────────────────────────────────────────────────

        public static void ApplyCheckboxStyle(CheckBox chk)
        {
            chk.FlatStyle = FlatStyle.Flat;
            chk.FlatAppearance.BorderColor = Theme.Outline;
            chk.FlatAppearance.CheckedBackColor = Theme.Primary;
            chk.ForeColor = Theme.OnSurface;
            chk.Font = Theme.FontBody;
            chk.Cursor = Cursors.Hand;
        }

        // ── TextBox ─────────────────────────────────────────────────────

        /// <summary>
        /// Tactical input: dark background, green focus border.
        /// </summary>
        public static void ApplyTextBoxStyle(TextBox txt)
        {
            txt.BackColor = Theme.SurfaceContainerHighest;
            txt.ForeColor = Theme.OnSurface;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = Theme.FontBody;
        }

        // ── ComboBox ────────────────────────────────────────────────────

        public static void ApplyComboBoxStyle(ComboBox cmb)
        {
            cmb.BackColor = Theme.SurfaceContainerHighest;
            cmb.ForeColor = Theme.OnSurface;
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.Font = Theme.FontBody;
        }

        // ── DataGridView ────────────────────────────────────────────────

        /// <summary>
        /// Zebra-striped telemetry grid: alternating surface tones, no divider lines.
        /// </summary>
        public static void ApplyDataGridStyle(DataGridView dgv)
        {
            dgv.BackgroundColor = Theme.SurfaceContainer;
            dgv.DefaultCellStyle.BackColor = Theme.SurfaceContainer;
            dgv.DefaultCellStyle.ForeColor = Theme.OnSurface;
            dgv.DefaultCellStyle.SelectionBackColor = Theme.SurfaceContainerHigh;
            dgv.DefaultCellStyle.SelectionForeColor = Theme.PrimaryText;
            dgv.DefaultCellStyle.Font = Theme.FontBody;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Theme.SurfaceDim;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Theme.SurfaceContainerHighest;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Theme.OnSurfaceVariant;
            dgv.ColumnHeadersDefaultCellStyle.Font = Theme.FontLabel;
            dgv.EnableHeadersVisualStyles = false;

            dgv.GridColor = Theme.OutlineVariant;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.BorderStyle = BorderStyle.None;
            dgv.RowHeadersVisible = false;
        }

        // ── Status Chip ─────────────────────────────────────────────────

        /// <summary>
        /// Creates a small rectangular status badge (e.g., "GPS: 3D LOCK" or "BAT: 24.2V").
        /// </summary>
        public static Label CreateStatusChip(string text, Color bgColor, Color fgColor)
        {
            Label chip = new Label();
            chip.Text = text.ToUpperInvariant();
            chip.Font = Theme.FontLabelSmall;
            chip.ForeColor = fgColor;
            chip.BackColor = bgColor;
            chip.AutoSize = true;
            chip.Padding = new Padding(6, 3, 6, 3);
            chip.Margin = new Padding(2);
            return chip;
        }

        // ── Section Header ──────────────────────────────────────────────

        /// <summary>
        /// Creates a section header with bracket-notation system tag.
        /// Example: "Compass Calibration  [ MAG_CAL ]"
        /// </summary>
        public static Panel CreateSectionHeader(string title, string subtitle, string statusText, Color statusColor)
        {
            Panel header = new Panel();
            header.BackColor = Color.Transparent;
            header.Height = 80;
            header.Dock = DockStyle.Top;

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = Theme.FontHeadlineMd;
            lblTitle.ForeColor = Theme.OnSurface;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(0, 8);
            header.Controls.Add(lblTitle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                Label lblSub = new Label();
                lblSub.Text = subtitle;
                lblSub.Font = Theme.FontBodySmall;
                lblSub.ForeColor = Theme.OnSurfaceVariant;
                lblSub.AutoSize = true;
                lblSub.Location = new Point(0, 38);
                header.Controls.Add(lblSub);
            }

            if (!string.IsNullOrEmpty(statusText))
            {
                Label lblStatus = new Label();
                lblStatus.Text = statusText.ToUpperInvariant();
                lblStatus.Font = new Font("Consolas", 9F, FontStyle.Bold);
                lblStatus.ForeColor = statusColor;
                lblStatus.AutoSize = true;
                lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                lblStatus.Location = new Point(header.Width - 200, 12);
                header.Controls.Add(lblStatus);
            }

            return header;
        }

        // ── Recursive Theme Application ─────────────────────────────────

        /// <summary>
        /// Recursively applies the Apex Control dark theme to all child controls.
        /// </summary>
        public static void ApplyThemeRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Panel p && p.Tag == null)
                {
                    p.BackColor = Theme.SurfaceContainer;
                }
                else if (c is Label lbl && lbl.Tag == null)
                {
                    lbl.ForeColor = Theme.OnSurface;
                    lbl.BackColor = Color.Transparent;
                }
                else if (c is Button btn && btn.Tag == null)
                {
                    ApplyButtonStyle(btn, false);
                }
                else if (c is TextBox txt && txt.Tag == null)
                {
                    ApplyTextBoxStyle(txt);
                }
                else if (c is CheckBox chk && chk.Tag == null)
                {
                    ApplyCheckboxStyle(chk);
                }
                else if (c is ComboBox cmb && cmb.Tag == null)
                {
                    ApplyComboBoxStyle(cmb);
                }
                else if (c is GroupBox gb)
                {
                    gb.BackColor = Theme.SurfaceContainer;
                    gb.ForeColor = Theme.OnSurfaceVariant;
                }
                else if (c is DataGridView dgv && dgv.Tag == null)
                {
                    ApplyDataGridStyle(dgv);
                }

                // Recurse into children
                if (c.HasChildren)
                {
                    ApplyThemeRecursive(c);
                }
            }
        }
    }
}
