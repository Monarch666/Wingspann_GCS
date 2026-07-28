using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Controls.BackstageView
{
    public class BackStageViewMenuPanel : Panel
    {
        internal Color GradColor = Color.White;
        internal Color PencilBorderColor = Color.White;

        private const int GradientWidth = 20;

        public BackStageViewMenuPanel()
        {
            this.SetStyle(ControlStyles.UserPaint, true);

            HorizontalScroll.Enabled = false;
            HorizontalScroll.Visible = false;
            HorizontalScroll.Maximum = 0;
            HScroll = false;
            AutoScroll = true;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Fill with solid background color (dark theme compatible)
            using (var bgBrush = new SolidBrush(BackColor))
            {
                pevent.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
            }

            // Subtle right-edge separator line
            pevent.Graphics.DrawLine(new Pen(PencilBorderColor), Width - 1, 0, Width - 1, Height);
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