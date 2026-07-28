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

        protected override void OnPaint(PaintEventArgs pevent)
        {
            if (this.Parent != null)
            {
                ((BackStageViewMenuPanel)this.Parent).PaintBackground(pevent);
            }

           Graphics g = pevent.Graphics;
           g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
           g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

           // Determine the font to use
           Font buttonFont;
           try
           {
               buttonFont = new Font("Segoe UI", 9.5F * (100 / g.DpiX), _isSelected ? FontStyle.Bold : FontStyle.Regular);
           }
           catch
           {
               buttonFont = new Font(this.Font.FontFamily, 9.5F * (100 / g.DpiX), _isSelected ? FontStyle.Bold : FontStyle.Regular);
           }

           if (_isSelected)
           {
               // Fill with a subtle dark gradient for the selected state
               var rect = new Rectangle(0, 0, Width, Height);
               using (var bgBrush = new LinearGradientBrush(rect, HighlightColor2, Color.FromArgb(30, HighlightColor1), LinearGradientMode.Horizontal))
               {
                   g.FillRectangle(bgBrush, rect);
               }

               // Left accent bar (red stripe)
               int accentWidth = 3;
               using (var accentBrush = new SolidBrush(HighlightColor1))
               {
                   g.FillRectangle(accentBrush, 0, 0, accentWidth, Height);
               }

               // Top and bottom border lines
               using (var linePen = new Pen(Color.FromArgb(60, HighlightColor1)))
               {
                   g.DrawLine(linePen, 0, 0, Width, 0);
                   g.DrawLine(linePen, 0, Height - 1, Width, Height - 1);
               }

               // Selected text
               g.DrawString(Text, buttonFont, new SolidBrush(SelectedTextColor), accentWidth + 8, (Height - buttonFont.GetHeight()) / 2);

               // Right side arrow indicator
               var pencilBrush = new Pen(this.PencilBorderColor);
               g.DrawLine(pencilBrush, Width - 1, 0, Width - 1, Height - 1);

               var arrowBrush = new SolidBrush(this.ContentPageColor);
               var midheight = Height / 2;
               var arSize = 7;
               var arrowPoints = new[]
               {
                   new Point(Width, midheight + arSize),
                   new Point(Width - arSize, midheight),
                   new Point(Width, midheight - arSize)
               };
               g.FillPolygon(arrowBrush, arrowPoints);
               g.DrawPolygon(pencilBrush, arrowPoints);
           }
           else
           {
               if (_isMouseOver)
               {
                   // Subtle hover highlight
                   using (var brush = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                   {
                       g.FillRectangle(brush, this.ClientRectangle);
                   }

                   // Subtle left accent on hover
                   using (var accentBrush = new SolidBrush(Color.FromArgb(80, HighlightColor1)))
                   {
                       g.FillRectangle(accentBrush, 0, 0, 2, Height);
                   }

                   using (var butPen = new Pen(Color.FromArgb(40, PencilBorderColor)))
                   {
                       g.DrawLine(butPen, 0, 0, Width, 0);
                       g.DrawLine(butPen, 0, Height - 1, Width, Height - 1);
                   }
               }

               g.DrawString(Text, buttonFont, new SolidBrush(this.UnSelectedTextColor), 11, (Height - buttonFont.GetHeight(g)) / 2);
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