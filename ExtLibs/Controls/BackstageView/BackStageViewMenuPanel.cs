using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Controls.BackstageView
{
    public class BackStageViewMenuPanel : Panel
    {
        internal Color GradColor = Color.FromArgb(18, 24, 32);
        internal Color PencilBorderColor = Color.FromArgb(30, 255, 255, 255);

        public BackStageViewMenuPanel()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(18, 24, 32);

            HorizontalScroll.Enabled = false;
            HorizontalScroll.Visible = false;
            HorizontalScroll.Maximum = 0;
            HScroll = false;
            AutoScroll = true;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            using (var bgBrush = new SolidBrush(Color.FromArgb(18, 24, 32)))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            // Subtle right-edge separator line
            using (var pen = new Pen(Color.FromArgb(25, 255, 255, 255)))
            {
                g.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            }
        }

        protected override void OnResize(System.EventArgs eventargs)
        {
            base.OnResize(eventargs);
            this.Invalidate();
        }

        public void PaintBackground(PaintEventArgs pevent)
        {
            OnPaintBackground(pevent);
        }
    }
}