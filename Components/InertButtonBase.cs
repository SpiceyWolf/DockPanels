using System.Drawing;
using System.Drawing.Imaging;

namespace System.Windows.Forms
{
    public abstract class InertButtonBase : Control
    {
        protected InertButtonBase()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        public abstract Bitmap Image
        {
            get;
        }

        private bool m_isMouseOver;
        protected bool IsMouseOver
        {
            get { return m_isMouseOver; }
            private set
            {
                if (m_isMouseOver == value)
                    return;

                m_isMouseOver = value;
                Invalidate();
            }
        }

        protected override Size DefaultSize => Resources.DockPane_Close.Size;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool over = ClientRectangle.Contains(e.X, e.Y);
            if (IsMouseOver != over)
                IsMouseOver = over;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (!IsMouseOver)
                IsMouseOver = true;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (IsMouseOver)
                IsMouseOver = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (IsMouseOver && Enabled)
            {
                using (var pen = new Pen(ForeColor))
                {
                    e.Graphics.DrawRectangle(pen, Rectangle.Inflate(ClientRectangle, -1, -1));
                }
            }

            using (var imageAttributes = new ImageAttributes())
            {
                var colorMap = new ColorMap[2];
                colorMap[0] = new ColorMap
                {
                    OldColor = Color.FromArgb(0, 0, 0),
                    NewColor = ForeColor
                };
                colorMap[1] = new ColorMap
                {
                    OldColor = Image.GetPixel(0, 0),
                    NewColor = Color.Transparent
                };

                imageAttributes.SetRemapTable(colorMap);

                e.Graphics.DrawImage(
                   Image,
                   new Rectangle(0, 0, Image.Width, Image.Height),
                   0, 0,
                   Image.Width,
                   Image.Height,
                   GraphicsUnit.Pixel,
                   imageAttributes);
            }

            base.OnPaint(e);
        }

        public void RefreshChanges()
        {
            if (IsDisposed)
                return;

            var mouseOver = ClientRectangle.Contains(PointToClient(MousePosition));
            IsMouseOver = mouseOver;

            OnRefreshChanges();
        }

        protected virtual void OnRefreshChanges()
        {
        }
    }
}
