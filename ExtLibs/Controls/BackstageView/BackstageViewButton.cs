using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MissionPlanner.Controls.BackstageView
{
    public class BackstageViewButton : Control
    {
        private bool _isSelected;

        internal Color ContentPageColor = Color.Gray;
        internal Color PencilBorderColor = Color.White;
        internal Color SelectedTextColor = Color.White;
        internal Color UnSelectedTextColor = Color.Gray;
        internal Color HighlightColor1 = SystemColors.Highlight;
        internal Color HighlightColor2 = SystemColors.MenuHighlight;
        private bool _isMouseOver;

        //internal Color HighlightColor1 = Color.FromArgb(0x94, 0xc1, 0x1f);
        //internal Color HighlightColor2 = Color.FromArgb(0xcd, 0xe2, 0x96);

        public BackstageViewButton()
        {
            this.SuspendLayout();

            SetStyle(ControlStyles.ResizeRedraw, true);

            this.Width = 150;
            this.Height = 30;
            
            this.ResumeLayout(false);
        }

        /// <summary>
        /// Whether this button should show the selected style
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;

                    this.Invalidate();
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
           Graphics g = pevent.Graphics;
           g.SmoothingMode = SmoothingMode.AntiAlias;
           g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

           // Clear background with parent dark panel color
           using (var bgBrush = new SolidBrush(Color.FromArgb(18, 24, 32)))
           {
               g.FillRectangle(bgBrush, this.ClientRectangle);
           }

           // Determine font to use
           Font buttonFont;
           try
           {
               buttonFont = new Font("Segoe UI", 9.5F, _isSelected ? FontStyle.Bold : FontStyle.Regular);
           }
           catch
           {
               buttonFont = new Font(this.Font.FontFamily, 9.5F, _isSelected ? FontStyle.Bold : FontStyle.Regular);
           }

           int marginX = 8;
           int marginY = 2;
           var pillRect = new Rectangle(marginX, marginY, Width - (marginX * 2), Height - (marginY * 2));

           if (_isSelected)
           {
               // Modern rounded green gradient pill
               using (var gp = RoundedRect(pillRect, 6))
               using (var bgBrush = new LinearGradientBrush(pillRect, Color.FromArgb(0x00, 0x56, 0x1B), Color.FromArgb(0x03, 0x22, 0x0B), LinearGradientMode.Horizontal))
               using (var borderPen = new Pen(Color.FromArgb(100, 124, 255, 0), 1.2f))
               {
                   g.FillPath(bgBrush, gp);
                   g.DrawPath(borderPen, gp);
               }

               // Matrix Green accent bar on the left
               using (var accentBrush = new SolidBrush(Color.FromArgb(124, 255, 0)))
               {
                   var barRect = new Rectangle(pillRect.X + 3, pillRect.Y + 4, 4, pillRect.Height - 8);
                   using (var barPath = RoundedRect(barRect, 2))
                   {
                       g.FillPath(accentBrush, barPath);
                   }
               }

               // Selected bold white text
               g.DrawString(Text, buttonFont, Brushes.White, pillRect.X + 16, (Height - buttonFont.GetHeight(g)) / 2);

               // Right chevron indicator (>)
               using (var fontChevron = new Font("Segoe UI", 8F, FontStyle.Bold))
               using (var chevronBrush = new SolidBrush(Color.FromArgb(124, 255, 0)))
               {
                   g.DrawString("›", fontChevron, chevronBrush, pillRect.Right - 16, (Height - fontChevron.GetHeight(g)) / 2);
               }
           }
           else
           {
               if (_isMouseOver)
               {
                   // Subtle rounded hover pill
                   using (var gp = RoundedRect(pillRect, 6))
                   using (var brush = new SolidBrush(Color.FromArgb(25, 124, 255, 0)))
                   using (var borderPen = new Pen(Color.FromArgb(30, 124, 255, 0), 1f))
                   {
                       g.FillPath(brush, gp);
                       g.DrawPath(borderPen, gp);
                   }

                   using (var accentBrush = new SolidBrush(Color.FromArgb(90, 124, 255, 0)))
                   {
                       var barRect = new Rectangle(pillRect.X + 3, pillRect.Y + 6, 3, pillRect.Height - 12);
                       using (var barPath = RoundedRect(barRect, 1))
                       {
                           g.FillPath(accentBrush, barPath);
                       }
                   }

                   g.DrawString(Text, buttonFont, Brushes.White, pillRect.X + 16, (Height - buttonFont.GetHeight(g)) / 2);
               }
               else
               {
                   using (var textBrush = new SolidBrush(Color.FromArgb(175, 190, 205)))
                   {
                       g.DrawString(Text, buttonFont, textBrush, marginX + 14, (Height - buttonFont.GetHeight(g)) / 2);
                   }
               }
           }

           buttonFont.Dispose();
        }


        protected override void OnMouseEnter(EventArgs e)
        {
            _isMouseOver = true;
            base.OnMouseEnter(e);
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isMouseOver = false;
            base.OnMouseLeave(e);
            this.Invalidate();

        }

        /*
        // This IS necessary for transparency - windows only..... remove it
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TRANSPARENT = 0x20;
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }
         */
    }
}