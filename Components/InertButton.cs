using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;

namespace System.Windows.Forms
{
    internal class InertButton : Button
    {
        private enum RepeatClickStatus
        {
            Disabled,
            Started,
            Repeating,
            Stopped
        }

        private class RepeatClickEventArgs : EventArgs
        {
            static RepeatClickEventArgs()
            {
                Empty = new RepeatClickEventArgs();
            }

            public new static RepeatClickEventArgs Empty { get; }
        }

        private readonly IContainer components = new Container();
        private int m_borderWidth = 1;
        private bool m_mouseOver;
        private bool m_mouseCapture;
        private bool m_isPopup;
        private Image m_imageEnabled;
        private Image m_imageDisabled;
        private int m_imageIndexEnabled = -1;
        private int m_imageIndexDisabled = -1;
        private bool m_monochrom = true;
        private ToolTip m_toolTip;
        private string m_toolTipText = "";
        private Color m_borderColor = Color.Empty;

        public InertButton()
        {
            InternalConstruct(null, null);
        }

        public InertButton(Image imageEnabled)
        {
            InternalConstruct(imageEnabled, null);
        }

        public InertButton(Image imageEnabled, Image imageDisabled)
        {
            InternalConstruct(imageEnabled, imageDisabled);
        }
        
        private void InternalConstruct(Image imageEnabled, Image imageDisabled)
        {
            // Remember parameters
            ImageEnabled = imageEnabled;
            ImageDisabled = imageDisabled;

            // Prevent drawing flicker by blitting from memory in WM_PAINT
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            // Prevent base class from trying to generate double click events and
            // so testing clicks against the double click time and rectangle. Getting
            // rid of this allows the user to press then release button very quickly.
            //SetStyle(ControlStyles.StandardDoubleClick, false);

            // Should not be allowed to select this control
            SetStyle(ControlStyles.Selectable, false);

            Timer = new Timer {Enabled = false};
            Timer.Tick += Timer_Tick;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        public Color BorderColor
        {
            get    {    return m_borderColor;    }
            set
            {
                if (m_borderColor == value) return;
                m_borderColor = value;
                Invalidate();
            }
        }

        private bool ShouldSerializeBorderColor()
        {
            return m_borderColor != Color.Empty;
        }

        public int BorderWidth
        {
            get { return m_borderWidth; }

            set
            {
                if (value < 1)
                    value = 1;
                if (m_borderWidth == value) return;
                m_borderWidth = value;
                Invalidate();
            }
        }

        public Image ImageEnabled
        {
            get
            { 
                if (m_imageEnabled != null)
                    return m_imageEnabled;

                try
                {
                    if (ImageList == null || ImageIndexEnabled == -1)
                        return null;
                    return ImageList.Images[m_imageIndexEnabled];
                }
                catch
                {
                    return null;
                }
            }

            set
            {
                if (m_imageEnabled == value) return;
                m_imageEnabled = value;
                Invalidate();
            }
        }

        private bool ShouldSerializeImageEnabled()
        {
            return m_imageEnabled != null;
        }

        public Image ImageDisabled
        {
            get
            {
                if (m_imageDisabled != null)
                    return m_imageDisabled;

                try
                {
                    if (ImageList == null || ImageIndexDisabled == -1)
                        return null;
                    return ImageList.Images[m_imageIndexDisabled];
                }
                catch
                {
                    return null;
                }
            }

            set
            {
                if (m_imageDisabled == value) return;
                m_imageDisabled = value;
                Invalidate();
            }
        }

        public int ImageIndexEnabled
        {
            get    {    return m_imageIndexEnabled;    }
            set
            {
                if (m_imageIndexEnabled == value) return;
                m_imageIndexEnabled = value;
                Invalidate();
            }
        }

        public int ImageIndexDisabled
        {
            get    {    return m_imageIndexDisabled;    }
            set
            {
                if (m_imageIndexDisabled == value) return;
                m_imageIndexDisabled = value;
                Invalidate();
            }
        }

        public bool IsPopup
        {
            get { return m_isPopup; }

            set
            {
                if (m_isPopup == value) return;
                m_isPopup = value;
                Invalidate();
            }
        }

        public bool Monochrome
        {
            get    {    return m_monochrom;    }
            set
            {
                if (value != m_monochrom)
                {
                    m_monochrom = value;
                    Invalidate();
                }
            }
        }

        public bool RepeatClick
        {
            get    {    return ClickStatus != RepeatClickStatus.Disabled;    }
            set
            {
                m_mouseOver = value;
                ClickStatus = RepeatClickStatus.Stopped;
            }
        }

        private RepeatClickStatus _mClickStatus = RepeatClickStatus.Disabled;
        private RepeatClickStatus ClickStatus
        {
            get    {    return _mClickStatus;    }
            set
            {
                if (_mClickStatus == value)
                    return;

                _mClickStatus = value;
                switch (ClickStatus)
                {
                    case RepeatClickStatus.Started:
                        Timer.Interval = RepeatClickDelay;
                        Timer.Enabled = true;
                        break;
                    case RepeatClickStatus.Repeating:
                        Timer.Interval = RepeatClickInterval;
                        break;
                    case RepeatClickStatus.Disabled:
                        break;
                    case RepeatClickStatus.Stopped:
                        break;
                    default:
                        Timer.Enabled = false;
                        break;
                }
            }
        }

        public int RepeatClickDelay { get; set; } = 500;

        public int RepeatClickInterval { get; set; } = 100;

        private Timer Timer { get; set; }

        public string ToolTipText
        {
            get    {    return m_toolTipText;    }
            set
            {
                if (m_toolTipText == value) return;
                if (m_toolTip == null)
                    m_toolTip = new ToolTip(components);
                m_toolTipText = value;
                m_toolTip.SetToolTip(this, value);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (m_mouseCapture && m_mouseOver)
                OnClick(RepeatClickEventArgs.Empty);
            if (ClickStatus == RepeatClickStatus.Started)
                ClickStatus = RepeatClickStatus.Repeating;
        }

        /// <exclude/>
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);

            if (mevent.Button != MouseButtons.Left)
                return;

            if (m_mouseCapture == false || m_mouseOver == false)
            {
                m_mouseCapture = true;
                m_mouseOver = true;

                //Redraw to show button state
                Invalidate();
            }

            if (RepeatClick)
            {
                OnClick(RepeatClickEventArgs.Empty);
                ClickStatus = RepeatClickStatus.Started;
            }
        }

        /// <exclude/>
        protected override void OnClick(EventArgs e)
        {
            if (RepeatClick && !(e is RepeatClickEventArgs))
                return;

            base.OnClick (e);
        }

        /// <exclude/>
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);

            if (mevent.Button != MouseButtons.Left)
                return;

            if (m_mouseOver || m_mouseCapture)
            {
                m_mouseOver = false;
                m_mouseCapture = false;

                // Redraw to show button state
                Invalidate();
            }

            if (RepeatClick)
                ClickStatus = RepeatClickStatus.Stopped;
        }

        /// <exclude/>
        protected override void OnMouseMove(MouseEventArgs mevent)
        {
            base.OnMouseMove(mevent);

            // Is mouse point inside our client rectangle
            bool over = ClientRectangle.Contains(new Point(mevent.X, mevent.Y));

            // If entering the button area or leaving the button area...
            if (over != m_mouseOver)
            {
                // Update state
                m_mouseOver = over;

                // Redraw to show button state
                Invalidate();
            }
        }

        /// <exclude/>
        protected override void OnMouseEnter(EventArgs e)
        {
            // Update state to reflect mouse over the button area
            if (!m_mouseOver)
            {
                m_mouseOver = true;

                // Redraw to show button state
                Invalidate();
            }

            base.OnMouseEnter(e);
        }

        /// <exclude/>
        protected override void OnMouseLeave(EventArgs e)
        {
            // Update state to reflect mouse not over the button area
            if (m_mouseOver)
            {
                m_mouseOver = false;

                // Redraw to show button state
                Invalidate();
            }

            base.OnMouseLeave(e);
        }

        /// <exclude/>
        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            DrawBackground(pevent.Graphics);
            DrawImage(pevent.Graphics);
            DrawText(pevent.Graphics);
            DrawBorder(pevent.Graphics);
        }

        private void DrawBackground(Graphics g)
        {
            using (SolidBrush brush = new SolidBrush(BackColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }
        }

        private void DrawImage(Graphics g)
        {
            var image = Enabled ? ImageEnabled : (ImageDisabled ?? ImageEnabled);
            ImageAttributes imageAttr = null;

            if (null == image)
                return;

            if (m_monochrom)
            {
                imageAttr = new ImageAttributes();

                // transform the monochrom image
                // white -> BackColor
                // black -> ForeColor
                var colorMap = new ColorMap[2];
                colorMap[0] = new ColorMap
                {
                    OldColor = Color.White,
                    NewColor = BackColor
                };
                colorMap[1] = new ColorMap
                {
                    OldColor = Color.Black,
                    NewColor = ForeColor
                };
                imageAttr.SetRemapTable(colorMap);
            }

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            if (!Enabled && (null == ImageDisabled))
            {
                using (Bitmap bitmapMono = new Bitmap(image, ClientRectangle.Size))
                {
                    if (imageAttr != null)
                    {
                        using (Graphics gMono = Graphics.FromImage(bitmapMono))
                        {
                            gMono.DrawImage(image, new[] { new Point(0, 0), new Point(image.Width - 1, 0), new Point(0, image.Height - 1) }, rect, GraphicsUnit.Pixel, imageAttr);
                        }
                    }
                    ControlPaint.DrawImageDisabled(g, bitmapMono, 0, 0, BackColor);
                }
            }
            else
            {
                // Three points provided are upper-left, upper-right and 
                // lower-left of the destination parallelogram. 
                Point[] pts = new Point[3];
                pts[0].X = Enabled && m_mouseOver && m_mouseCapture ? 1 : 0;
                pts[0].Y = Enabled && m_mouseOver && m_mouseCapture ? 1 : 0;
                pts[1].X = pts[0].X + ClientRectangle.Width;
                pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X;
                pts[2].Y = pts[1].Y + ClientRectangle.Height;

                if (imageAttr == null)
                    g.DrawImage(image, pts, rect, GraphicsUnit.Pixel);
                else
                    g.DrawImage(image, pts, rect, GraphicsUnit.Pixel, imageAttr);
            }
        }    

        private void DrawText(Graphics g)
        {
            if (Text == string.Empty)
                return;

            Rectangle rect = ClientRectangle;

            rect.X += BorderWidth;
            rect.Y += BorderWidth;
            rect.Width -= 2 * BorderWidth;
            rect.Height -= 2 * BorderWidth;

            StringFormat stringFormat = new StringFormat();

            if (TextAlign == ContentAlignment.TopLeft)
            {
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Near;
            }
            else if (TextAlign == ContentAlignment.TopCenter)
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Near;
            }
            else if (TextAlign == ContentAlignment.TopRight)
            {
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Near;
            }
            else if (TextAlign == ContentAlignment.MiddleLeft)
            {
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Center;
            }
            else if (TextAlign == ContentAlignment.MiddleCenter)
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
            }
            else if (TextAlign == ContentAlignment.MiddleRight)
            {
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Center;
            }
            else if (TextAlign == ContentAlignment.BottomLeft)
            {
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Far;
            }
            else if (TextAlign == ContentAlignment.BottomCenter)
            {
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Far;
            }
            else if (TextAlign == ContentAlignment.BottomRight)
            {
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Far;
            }

            using (Brush brush = new SolidBrush(ForeColor))
            {
                g.DrawString(Text, Font, brush, rect, stringFormat);
            }
        }

        private void DrawBorder(Graphics g)
        {
            ButtonBorderStyle bs;

            // Decide on the type of border to draw around image
            if (!Enabled)
                bs = IsPopup ? ButtonBorderStyle.Outset : ButtonBorderStyle.Solid;
            else if (m_mouseOver && m_mouseCapture)
                bs = ButtonBorderStyle.Inset;
            else if (IsPopup || m_mouseOver)
                bs = ButtonBorderStyle.Outset;
            else
                bs = ButtonBorderStyle.Solid;

            Color colorLeftTop;
            Color colorRightBottom;
            if (bs == ButtonBorderStyle.Solid)
            {
                colorLeftTop = BackColor;
                colorRightBottom = BackColor;
            }
            else if (bs == ButtonBorderStyle.Outset)
            {
                colorLeftTop = m_borderColor.IsEmpty ? BackColor : m_borderColor;
                colorRightBottom = BackColor;
            }
            else
            {
                colorLeftTop = BackColor;
                colorRightBottom = m_borderColor.IsEmpty ? BackColor : m_borderColor;
            }
            ControlPaint.DrawBorder(g, ClientRectangle,
                colorLeftTop, m_borderWidth, bs,
                colorLeftTop, m_borderWidth, bs,
                colorRightBottom, m_borderWidth, bs,
                colorRightBottom, m_borderWidth, bs);
        }

        /// <exclude/>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (Enabled == false)
            {
                m_mouseOver = false;
                m_mouseCapture = false;
                if (RepeatClick && ClickStatus != RepeatClickStatus.Stopped)
                    ClickStatus = RepeatClickStatus.Stopped;
            }
            Invalidate();
        }
    }
}