using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Windows.Forms
{
    public sealed class AutoHideStrip : Control
    {

        private const int _ImageHeight = 16;
        private const int _ImageWidth = 16;
        private const int _ImageGapTop = 2;
        private const int _ImageGapLeft = 4;
        private const int _ImageGapRight = 2;
        private const int _ImageGapBottom = 2;
        private const int _TextGapLeft = 0;
        private const int _TextGapRight = 0;
        private const int _TabGapTop = 3;
        private const int _TabGapLeft = 4;
        private const int _TabGapBetween = 10;

        #region Customizable Properties
        public Font TextFont => DockPanel.Skin.AutoHideStripSkin.TextFont;

        private static StringFormat _stringFormatTabHorizontal;
        private StringFormat StringFormatTabHorizontal
        {
            get
            {
                if (_stringFormatTabHorizontal == null)
                {
                    _stringFormatTabHorizontal = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.None
                    };
                }

                if (RightToLeft == RightToLeft.Yes)
                    _stringFormatTabHorizontal.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                else
                    _stringFormatTabHorizontal.FormatFlags &= ~StringFormatFlags.DirectionRightToLeft;

                return _stringFormatTabHorizontal;
            }
        }

        private static StringFormat _stringFormatTabVertical;
        private StringFormat StringFormatTabVertical
        {
            get
            {
                if (_stringFormatTabVertical == null)
                {
                    _stringFormatTabVertical = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionVertical,
                        Trimming = StringTrimming.None
                    };
                }
                if (RightToLeft == RightToLeft.Yes)
                    _stringFormatTabVertical.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
                else
                    _stringFormatTabVertical.FormatFlags &= ~StringFormatFlags.DirectionRightToLeft;

                return _stringFormatTabVertical;
            }
        }

        private static int ImageHeight => _ImageHeight;

        private static int ImageWidth => _ImageWidth;

        private static int ImageGapTop => _ImageGapTop;

        private static int ImageGapLeft => _ImageGapLeft;

        private static int ImageGapRight => _ImageGapRight;

        private static int ImageGapBottom => _ImageGapBottom;

        private static int TextGapLeft => _TextGapLeft;

        private static int TextGapRight => _TextGapRight;

        private static int TabGapTop => _TabGapTop;

        private static int TabGapLeft => _TabGapLeft;

        private static int TabGapBetween => _TabGapBetween;

        private static Pen PenTabBorder => SystemPens.GrayText;

        #endregion


        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        private class Tab : IDisposable
        {
            protected internal Tab(IDockContent content)
            {
                Content = content;
            }

            ~Tab()
            {
                Dispose(false);
            }

            public int TabX { get; set; }

            public int TabWidth { get; set; }

            public IDockContent Content { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        private sealed class TabCollection : IEnumerable<Tab>
        {
            #region IEnumerable Members
            IEnumerator<Tab> IEnumerable<Tab>.GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }
            #endregion

            internal TabCollection(DockPane pane)
            {
                DockPane = pane;
            }

            private DockPane DockPane { get; }

            private DockPanel DockPanel => DockPane.DockPanel;

            public int Count => DockPane.DisplayingContents.Count;

            private Tab this[int index]
            {
                get
                {
                    var content = DockPane.DisplayingContents[index];
                    if (content == null)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    if (content.DockHandler.AutoHideTab == null)
                        content.DockHandler.AutoHideTab = DockPanel.AutoHideStripControl.CreateTab(content);
                    return content.DockHandler.AutoHideTab as Tab;
                }
            }

            private bool Contains(Tab tab)
            {
                return IndexOf(tab) != -1;
            }

            private bool Contains(IDockContent content)
            {
                return IndexOf(content) != -1;
            }

            private int IndexOf(Tab tab)
            {
                if (tab == null)
                    return -1;

                return IndexOf(tab.Content);
            }

            private int IndexOf(IDockContent content)
            {
                return DockPane.DisplayingContents.IndexOf(content);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        private class Pane : IDisposable
        {
            protected internal Pane(DockPane dockPane)
            {
                DockPane = dockPane;
            }

            ~Pane()
            {
                Dispose(false);
            }

            public DockPane DockPane { get; }

            public TabCollection AutoHideTabs
            {
                get
                {
                    if (DockPane.AutoHideTabs == null)
                        DockPane.AutoHideTabs = new TabCollection(DockPane);
                    return DockPane.AutoHideTabs as TabCollection;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        private sealed class PaneCollection : IEnumerable<Pane>
        {
            private class AutoHideState
            {
                private readonly DockState _mDockState;
                private bool _mSelected;

                public AutoHideState(DockState dockState)
                {
                    _mDockState = dockState;
                }

                public DockState DockState => _mDockState;

                public bool Selected
                {
                    get { return _mSelected; }
                    set { _mSelected = value; }
                }
            }

            private class AutoHideStateCollection
            {
                private readonly AutoHideState[] _mStates;

                public AutoHideStateCollection()
                {
                    _mStates = new[]    {
                                                new AutoHideState(DockState.DockTopAutoHide),
                                                new AutoHideState(DockState.DockBottomAutoHide),
                                                new AutoHideState(DockState.DockLeftAutoHide),
                                                new AutoHideState(DockState.DockRightAutoHide)
                                            };
                }

                public AutoHideState this[DockState dockState]
                {
                    get
                    {
                        for (var i = 0; i < _mStates.Length; i++)
                        {
                            if (_mStates[i].DockState == dockState)
                                return _mStates[i];
                        }
                        throw new ArgumentOutOfRangeException(nameof(dockState));
                    }
                }

                public bool ContainsPane(DockPane pane)
                {
                    return !pane.IsHidden && _mStates.Any(t => t.DockState == pane.DockState && t.Selected);
                }
            }

            internal PaneCollection(DockPanel panel, DockState dockState)
            {
                DockPanel = panel;
                States = new AutoHideStateCollection();
                States[DockState.DockTopAutoHide].Selected = dockState == DockState.DockTopAutoHide;
                States[DockState.DockBottomAutoHide].Selected = dockState == DockState.DockBottomAutoHide;
                States[DockState.DockLeftAutoHide].Selected = dockState == DockState.DockLeftAutoHide;
                States[DockState.DockRightAutoHide].Selected = dockState == DockState.DockRightAutoHide;
            }

            private DockPanel DockPanel { get; }

            private AutoHideStateCollection States { get; }

            public int Count
            {
                get
                {
                    return DockPanel.Panes.Count(pane => States.ContainsPane(pane));
                }
            }

            private Pane this[int index]
            {
                get
                {
                    int count = 0;
                    foreach (DockPane pane in DockPanel.Panes)
                    {
                        if (!States.ContainsPane(pane))
                            continue;

                        if (count == index)
                        {
                            if (pane.AutoHidePane == null)
                                pane.AutoHidePane = DockPanel.AutoHideStripControl.CreatePane(pane);
                            return pane.AutoHidePane as Pane;
                        }

                        count++;
                    }
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            private bool Contains(Pane pane)
            {
                return IndexOf(pane) != -1;
            }

            private int IndexOf(Pane pane)
            {
                if (pane == null)
                    return -1;

                var index = 0;
                foreach (var dockPane in DockPanel.Panes)
                {
                    if (!States.ContainsPane(pane.DockPane))
                        continue;

                    if (Equals(pane, dockPane.AutoHidePane))
                        return index;

                    index++;
                }
                return -1;
            }

            #region IEnumerable Members

            IEnumerator<Pane> IEnumerable<Pane>.GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            #endregion
        }

        public AutoHideStrip(DockPanel panel)
        {
            DockPanel = panel;
            PanesTop = new PaneCollection(panel, DockState.DockTopAutoHide);
            PanesBottom = new PaneCollection(panel, DockState.DockBottomAutoHide);
            PanesLeft = new PaneCollection(panel, DockState.DockLeftAutoHide);
            PanesRight = new PaneCollection(panel, DockState.DockRightAutoHide);

            SetStyle(ControlStyles.Selectable, false);
            SetStyle(ControlStyles.ResizeRedraw |
    ControlStyles.UserPaint |
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.OptimizedDoubleBuffer, true);

            BackColor = SystemColors.ControlLight;
        }

        private DockPanel DockPanel { get; }

        private PaneCollection PanesTop { get; }

        private PaneCollection PanesBottom { get; }

        private PaneCollection PanesLeft { get; }

        private PaneCollection PanesRight { get; }

        private PaneCollection GetPanes(DockState dockState)
        {
            switch (dockState)
            {
                case DockState.DockTopAutoHide:
                    return PanesTop;
                case DockState.DockBottomAutoHide:
                    return PanesBottom;
                case DockState.DockLeftAutoHide:
                    return PanesLeft;
                case DockState.DockRightAutoHide:
                    return PanesRight;
                case DockState.Unknown:
                    break;
                case DockState.Float:
                    break;
                case DockState.Document:
                    break;
                case DockState.DockTop:
                    break;
                case DockState.DockLeft:
                    break;
                case DockState.DockBottom:
                    break;
                case DockState.DockRight:
                    break;
                case DockState.Hidden:
                    break;
            }
            throw new ArgumentOutOfRangeException(nameof(dockState));
        }

        internal int GetNumberOfPanes(DockState dockState)
        {
            return GetPanes(dockState).Count;
        }

        private Rectangle RectangleTopLeft
        {
            get
            {
                int height = MeasureHeight();
                return PanesTop.Count > 0 && PanesLeft.Count > 0 ? new Rectangle(0, 0, height, height) : Rectangle.Empty;
            }
        }

        private Rectangle RectangleTopRight
        {
            get
            {
                int height = MeasureHeight();
                return PanesTop.Count > 0 && PanesRight.Count > 0 ? new Rectangle(Width - height, 0, height, height) : Rectangle.Empty;
            }
        }

        private Rectangle RectangleBottomLeft
        {
            get
            {
                int height = MeasureHeight();
                return PanesBottom.Count > 0 && PanesLeft.Count > 0 ? new Rectangle(0, Height - height, height, height) : Rectangle.Empty;
            }
        }

        private Rectangle RectangleBottomRight
        {
            get
            {
                int height = MeasureHeight();
                return PanesBottom.Count > 0 && PanesRight.Count > 0 ? new Rectangle(Width - height, Height - height, height, height) : Rectangle.Empty;
            }
        }

        internal Rectangle GetTabStripRectangle(DockState dockState)
        {
            int height = MeasureHeight();
            if (dockState == DockState.DockTopAutoHide && PanesTop.Count > 0)
                return new Rectangle(RectangleTopLeft.Width, 0, Width - RectangleTopLeft.Width - RectangleTopRight.Width, height);
            else if (dockState == DockState.DockBottomAutoHide && PanesBottom.Count > 0)
                return new Rectangle(RectangleBottomLeft.Width, Height - height, Width - RectangleBottomLeft.Width - RectangleBottomRight.Width, height);
            else if (dockState == DockState.DockLeftAutoHide && PanesLeft.Count > 0)
                return new Rectangle(0, RectangleTopLeft.Width, height, Height - RectangleTopLeft.Height - RectangleBottomLeft.Height);
            else if (dockState == DockState.DockRightAutoHide && PanesRight.Count > 0)
                return new Rectangle(Width - height, RectangleTopRight.Width, height, Height - RectangleTopRight.Height - RectangleBottomRight.Height);
            else
                return Rectangle.Empty;
        }

        private GraphicsPath _mDisplayingArea;
        private GraphicsPath DisplayingArea => _mDisplayingArea ?? (_mDisplayingArea = new GraphicsPath());

        private void SetRegion()
        {
            DisplayingArea.Reset();
            DisplayingArea.AddRectangle(RectangleTopLeft);
            DisplayingArea.AddRectangle(RectangleTopRight);
            DisplayingArea.AddRectangle(RectangleBottomLeft);
            DisplayingArea.AddRectangle(RectangleBottomRight);
            DisplayingArea.AddRectangle(GetTabStripRectangle(DockState.DockTopAutoHide));
            DisplayingArea.AddRectangle(GetTabStripRectangle(DockState.DockBottomAutoHide));
            DisplayingArea.AddRectangle(GetTabStripRectangle(DockState.DockLeftAutoHide));
            DisplayingArea.AddRectangle(GetTabStripRectangle(DockState.DockRightAutoHide));
            Region = new Region(DisplayingArea);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
                return;

            IDockContent content = HitTest();
            if (content == null)
                return;

            SetActiveAutoHideContent(content);

            content.DockHandler.Activate();
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);

            if (!DockPanel.ShowAutoHideContentOnHover)
                return;

            IDockContent content = HitTest();
            SetActiveAutoHideContent(content);

            // requires further tracking of mouse hover behavior,
            ResetMouseEventArgs();
        }

        private void SetActiveAutoHideContent(IDockContent content)
        {
            if (content != null && DockPanel.ActiveAutoHideContent != content)
                DockPanel.ActiveAutoHideContent = content;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            Color startColor = DockPanel.Skin.AutoHideStripSkin.DockStripGradient.StartColor;
            Color endColor = DockPanel.Skin.AutoHideStripSkin.DockStripGradient.EndColor;
            LinearGradientMode gradientMode = DockPanel.Skin.AutoHideStripSkin.DockStripGradient.LinearGradientMode;
            using (LinearGradientBrush brush = new LinearGradientBrush(ClientRectangle, startColor, endColor, gradientMode))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            DrawTabStrip(g);
        }

        private void DrawTabStrip(Graphics g)
        {
            DrawTabStrip(g, DockState.DockTopAutoHide);
            DrawTabStrip(g, DockState.DockBottomAutoHide);
            DrawTabStrip(g, DockState.DockLeftAutoHide);
            DrawTabStrip(g, DockState.DockRightAutoHide);
        }

        private void DrawTabStrip(Graphics g, DockState dockState)
        {
            Rectangle rectTabStrip = GetLogicalTabStripRectangle(dockState);

            if (rectTabStrip.IsEmpty)
                return;

            Matrix matrixIdentity = g.Transform;
            if (dockState == DockState.DockLeftAutoHide || dockState == DockState.DockRightAutoHide)
            {
                Matrix matrixRotated = new Matrix();
                matrixRotated.RotateAt(90, new PointF(rectTabStrip.X + (float)rectTabStrip.Height / 2,
                    rectTabStrip.Y + (float)rectTabStrip.Height / 2));
                g.Transform = matrixRotated;
            }

            foreach (Pane pane in GetPanes(dockState))
            {
                foreach (var tab1 in pane.AutoHideTabs)
                {
                    var tab = tab1;
                    DrawTab(g, tab);
                }
            }
            g.Transform = matrixIdentity;
        }


        protected override void OnLayout(LayoutEventArgs levent)
        {
            CalculateTabs();
            RefreshChanges();
            base.OnLayout(levent);
        }

        private void CalculateTabs()
        {
            CalculateTabs(DockState.DockTopAutoHide);
            CalculateTabs(DockState.DockBottomAutoHide);
            CalculateTabs(DockState.DockLeftAutoHide);
            CalculateTabs(DockState.DockRightAutoHide);
        }

        private void CalculateTabs(DockState dockState)
        {
            Rectangle rectTabStrip = GetLogicalTabStripRectangle(dockState);

            int imageHeight = rectTabStrip.Height - ImageGapTop - ImageGapBottom;
            int imageWidth = ImageWidth;
            if (imageHeight > ImageHeight)
                imageWidth = ImageWidth * (imageHeight / ImageHeight);

            int x = TabGapLeft + rectTabStrip.X;
            foreach (Pane pane in GetPanes(dockState))
            {
                foreach (var tab1 in pane.AutoHideTabs)
                {
                    var tab = tab1;
                    var width = imageWidth + ImageGapLeft + ImageGapRight +
                        TextRenderer.MeasureText(tab.Content.DockHandler.TabText, TextFont).Width +
                        TextGapLeft + TextGapRight;
                    tab.TabX = x;
                    tab.TabWidth = width;
                    x += width;
                }

                x += TabGapBetween;
            }
        }

        internal void RefreshChanges()
        {
            if (IsDisposed)
                return;

            SetRegion();
            OnRefreshChanges();
        }

        private void OnRefreshChanges()
        {
            CalculateTabs();
            Invalidate();
        }

        public int MeasureHeight()
        {
            return Math.Max(ImageGapBottom +
                ImageGapTop + ImageHeight,
                TextFont.Height) + TabGapTop;
        }

        private IDockContent HitTest()
        {
            Point ptMouse = PointToClient(MousePosition);
            return HitTest(ptMouse);
        }

        private Tab CreateTab(IDockContent content)
        {
            return new Tab(content);
        }

        private Pane CreatePane(DockPane dockPane)
        {
            return new Pane(dockPane);
        }

        private IDockContent HitTest(Point point)
        {
            return (from state in DockStates let rectTabStrip = GetLogicalTabStripRectangle(state, true) where rectTabStrip.Contains(point) from pane in GetPanes(state) from tab1 in pane.AutoHideTabs select (Tab)tab1 into tab let path = GetTabOutline(tab, true, true) where path.IsVisible(point) select tab.Content).FirstOrDefault();
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new AutoHideStripsAccessibleObject(this);
        }

        private Rectangle GetTabBounds(Tab tab)
        {
            GraphicsPath path = GetTabOutline((Tab)tab, true, true);
            RectangleF bounds = path.GetBounds();
            return new Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
        }

        internal static Rectangle ToScreen(Rectangle rectangle, Control parent)
        {
            if (parent == null)
                return rectangle;

            return new Rectangle(parent.PointToScreen(new Point(rectangle.Left, rectangle.Top)), new Size(rectangle.Width, rectangle.Height));
        }

        public class AutoHideStripsAccessibleObject : ControlAccessibleObject
        {
            private readonly AutoHideStrip _strip;

            public AutoHideStripsAccessibleObject(AutoHideStrip strip)
                : base(strip)
            {
                _strip = strip;
            }

            public override AccessibleRole Role => AccessibleRole.Window;

            public override int GetChildCount()
            {
                // Top, Bottom, Left, Right
                return 4;
            }

            public override AccessibleObject GetChild(int index)
            {
                switch (index)
                {
                    case 0:
                        return new AutoHideStripAccessibleObject(_strip, DockState.DockTopAutoHide, this);
                    case 1:
                        return new AutoHideStripAccessibleObject(_strip, DockState.DockBottomAutoHide, this);
                    case 2:
                        return new AutoHideStripAccessibleObject(_strip, DockState.DockLeftAutoHide, this);
                    default:
                        return new AutoHideStripAccessibleObject(_strip, DockState.DockRightAutoHide, this);
                }
            }
            
            public override AccessibleObject HitTest(int x, int y)
            {
                Dictionary<DockState, Rectangle> rectangles = new Dictionary<DockState, Rectangle> {
                    { DockState.DockTopAutoHide,    _strip.GetTabStripRectangle(DockState.DockTopAutoHide) },
                    { DockState.DockBottomAutoHide, _strip.GetTabStripRectangle(DockState.DockBottomAutoHide) },
                    { DockState.DockLeftAutoHide,   _strip.GetTabStripRectangle(DockState.DockLeftAutoHide) },
                    { DockState.DockRightAutoHide,  _strip.GetTabStripRectangle(DockState.DockRightAutoHide) },
                };

                var point = _strip.PointToClient(new Point(x, y));
                return (from rectangle in rectangles where rectangle.Value.Contains(point) select new AutoHideStripAccessibleObject(_strip, rectangle.Key, this)).FirstOrDefault();
            }
        }

        private Rectangle RtlTransform(Rectangle rect, DockState dockState)
        {
            Rectangle rectTransformed;
            if (dockState == DockState.DockLeftAutoHide || dockState == DockState.DockRightAutoHide)
                rectTransformed = rect;
            else
                rectTransformed = DrawHelper.RtlTransform(this, rect);

            return rectTransformed;
        }

        private GraphicsPath GetTabOutline(Tab tab, bool transformed, bool rtlTransform)
        {
            DockState dockState = tab.Content.DockHandler.DockState;
            Rectangle rectTab = GetTabRectangle(tab, transformed);
            if (rtlTransform)
                rectTab = RtlTransform(rectTab, dockState);
            bool upTab = dockState == DockState.DockLeftAutoHide || dockState == DockState.DockBottomAutoHide;
            DrawHelper.GetRoundedCornerTab(GraphicsPath, rectTab, upTab);

            return GraphicsPath;
        }

        private void DrawTab(Graphics g, Tab tab)
        {
            Rectangle rectTabOrigin = GetTabRectangle(tab);
            if (rectTabOrigin.IsEmpty)
                return;

            DockState dockState = tab.Content.DockHandler.DockState;
            IDockContent content = tab.Content;

            GraphicsPath path = GetTabOutline(tab, false, true);
            Color startColor = DockPanel.Skin.AutoHideStripSkin.TabGradient.StartColor;
            Color endColor = DockPanel.Skin.AutoHideStripSkin.TabGradient.EndColor;
            LinearGradientMode gradientMode = DockPanel.Skin.AutoHideStripSkin.TabGradient.LinearGradientMode;
            g.FillPath(new LinearGradientBrush(rectTabOrigin, startColor, endColor, gradientMode), path);
            g.DrawPath(PenTabBorder, path);

            // Set no rotate for drawing icon and text
            using (Matrix matrixRotate = g.Transform)
            {
                g.Transform = MatrixIdentity;

                int imageWidth = 0;
                if (((Form)content).ShowIcon)
                {
                    // Draw the icon
                    Rectangle rectImage = rectTabOrigin;
                    rectImage.X += ImageGapLeft;
                    rectImage.Y += ImageGapTop;
                    var imageHeight = rectTabOrigin.Height - ImageGapTop - ImageGapBottom;
                    imageWidth = ImageWidth;
                    if (imageHeight > ImageHeight)
                        imageWidth = ImageWidth * (imageHeight / ImageHeight);
                    rectImage.Height = imageHeight;
                    rectImage.Width = imageWidth;
                    rectImage = GetTransformedRectangle(dockState, rectImage);

                    if (dockState == DockState.DockLeftAutoHide || dockState == DockState.DockRightAutoHide)
                    {
                        // The DockState is DockLeftAutoHide or DockRightAutoHide, so rotate the image 90 degrees to the right. 
                        Rectangle rectTransform = RtlTransform(rectImage, dockState);
                        Point[] rotationPoints =
                        {
                        new Point(rectTransform.X + rectTransform.Width, rectTransform.Y),
                        new Point(rectTransform.X + rectTransform.Width, rectTransform.Y + rectTransform.Height),
                        new Point(rectTransform.X, rectTransform.Y)
                    };

                        using (Icon rotatedIcon = new Icon(((Form)content).Icon, 16, 16))
                        {
                            g.DrawImage(rotatedIcon.ToBitmap(), rotationPoints);
                        }
                    }
                    else
                    {
                        // Draw the icon normally without any rotation.
                        g.DrawIcon(((Form)content).Icon, RtlTransform(rectImage, dockState));
                    }
                }

                // Draw the text
                Rectangle rectText = rectTabOrigin;
                rectText.X += ImageGapLeft + imageWidth + ImageGapRight + TextGapLeft;
                rectText.Width -= ImageGapLeft + imageWidth + ImageGapRight + TextGapLeft + 4;
                rectText = RtlTransform(GetTransformedRectangle(dockState, rectText), dockState);

                Color textColor = DockPanel.Skin.AutoHideStripSkin.TabGradient.TextColor;

                if (dockState == DockState.DockLeftAutoHide || dockState == DockState.DockRightAutoHide)
                    g.DrawString(content.DockHandler.TabText, TextFont, new SolidBrush(textColor), rectText, StringFormatTabVertical);
                else
                    g.DrawString(content.DockHandler.TabText, TextFont, new SolidBrush(textColor), rectText, StringFormatTabHorizontal);

                // Set rotate back
                g.Transform = matrixRotate;
            }
        }

        private Rectangle GetLogicalTabStripRectangle(DockState dockState, bool transformed = false)
        {
            if (!DockHelper.IsDockStateAutoHide(dockState))
                return Rectangle.Empty;

            var leftPanes = GetPanes(DockState.DockLeftAutoHide).Count;
            var rightPanes = GetPanes(DockState.DockRightAutoHide).Count;
            var topPanes = GetPanes(DockState.DockTopAutoHide).Count;
            var bottomPanes = GetPanes(DockState.DockBottomAutoHide).Count;

            int x, y, width;

            var height = MeasureHeight();
            if (dockState == DockState.DockLeftAutoHide && leftPanes > 0)
            {
                x = 0;
                y = topPanes == 0 ? 0 : height;
                width = Height - (topPanes == 0 ? 0 : height) - (bottomPanes == 0 ? 0 : height);
            }
            else if (dockState == DockState.DockRightAutoHide && rightPanes > 0)
            {
                x = Width - height;
                if (leftPanes != 0 && x < height)
                    x = height;
                y = topPanes == 0 ? 0 : height;
                width = Height - (topPanes == 0 ? 0 : height) - (bottomPanes == 0 ? 0 : height);
            }
            else if (dockState == DockState.DockTopAutoHide && topPanes > 0)
            {
                x = leftPanes == 0 ? 0 : height;
                y = 0;
                width = Width - (leftPanes == 0 ? 0 : height) - (rightPanes == 0 ? 0 : height);
            }
            else if (dockState == DockState.DockBottomAutoHide && bottomPanes > 0)
            {
                x = leftPanes == 0 ? 0 : height;
                y = Height - height;
                if (topPanes != 0 && y < height)
                    y = height;
                width = Width - (leftPanes == 0 ? 0 : height) - (rightPanes == 0 ? 0 : height);
            }
            else
                return Rectangle.Empty;

            if (width == 0 || height == 0)
            {
                return Rectangle.Empty;
            }

            var rect = new Rectangle(x, y, width, height);
            return transformed ? GetTransformedRectangle(dockState, rect) : rect;
        }

        private Rectangle GetTabRectangle(Tab tab, bool transformed = false)
        {
            var dockState = tab.Content.DockHandler.DockState;
            var rectTabStrip = GetLogicalTabStripRectangle(dockState);

            if (rectTabStrip.IsEmpty)
                return Rectangle.Empty;

            var x = tab.TabX;
            var y = rectTabStrip.Y +
                (dockState == DockState.DockTopAutoHide || dockState == DockState.DockRightAutoHide ?
                0 : TabGapTop);
            var width = tab.TabWidth;
            var height = rectTabStrip.Height - TabGapTop;

            return !transformed ? new Rectangle(x, y, width, height) : GetTransformedRectangle(dockState, new Rectangle(x, y, width, height));
        }

        private static Matrix MatrixIdentity { get; } = new Matrix();

        private static DockState[] _dockStates;
        private static IEnumerable<DockState> DockStates
        {
            get
            {
                if (_dockStates != null) return _dockStates;
                _dockStates = new DockState[4];
                _dockStates[0] = DockState.DockLeftAutoHide;
                _dockStates[1] = DockState.DockRightAutoHide;
                _dockStates[2] = DockState.DockTopAutoHide;
                _dockStates[3] = DockState.DockBottomAutoHide;
                return _dockStates;
            }
        }

        private static GraphicsPath _graphicsPath;
        internal static GraphicsPath GraphicsPath => _graphicsPath ?? (_graphicsPath = new GraphicsPath());

        private Rectangle GetTransformedRectangle(DockState dockState, Rectangle rect)
        {
            if (dockState != DockState.DockLeftAutoHide && dockState != DockState.DockRightAutoHide)
                return rect;

            var pts = new PointF[1];
            // the center of the rectangle
            pts[0].X = rect.X + (float)rect.Width / 2;
            pts[0].Y = rect.Y + (float)rect.Height / 2;
            Rectangle rectTabStrip = GetLogicalTabStripRectangle(dockState);
            using (var matrix = new Matrix())
            {
                matrix.RotateAt(90, new PointF(rectTabStrip.X + (float)rectTabStrip.Height / 2,
                                               rectTabStrip.Y + (float)rectTabStrip.Height / 2));
                matrix.TransformPoints(pts);
            }

            return new Rectangle((int)(pts[0].X - (float)rect.Height / 2 + .5F),
                (int)(pts[0].Y - (float)rect.Width / 2 + .5F),
                rect.Height, rect.Width);
        }

        public class AutoHideStripAccessibleObject : AccessibleObject
        {
            private readonly AutoHideStrip _strip;
            private readonly DockState _state;

            public AutoHideStripAccessibleObject(AutoHideStrip strip, DockState state, AccessibleObject parent)
            {
                _strip = strip;
                _state = state;

                Parent = parent;
            }

            public override AccessibleObject Parent { get; }

            public override AccessibleRole Role => AccessibleRole.PageTabList;

            public override int GetChildCount()
            {
                return _strip.GetPanes(_state).Sum(pane => pane.AutoHideTabs.Count);
            }

            public override AccessibleObject GetChild(int index)
            {
                List<Tab> tabs = new List<Tab>();
                foreach (Pane pane in _strip.GetPanes(_state))
                {
                    tabs.AddRange(pane.AutoHideTabs);
                }

                return new AutoHideStripTabAccessibleObject(_strip, tabs[index], this);
            }

            public override Rectangle Bounds
            {
                get
                {
                    Rectangle rectangle = _strip.GetTabStripRectangle(_state);
                    return ToScreen(rectangle, _strip);
                }
            }
        }

        private class AutoHideStripTabAccessibleObject : AccessibleObject
        {
            private readonly AutoHideStrip _strip;
            private readonly Tab _tab;

            internal AutoHideStripTabAccessibleObject(AutoHideStrip strip, Tab tab, AccessibleObject parent)
            {
                _strip = strip;
                _tab = tab;

                Parent = parent;
            }

            public override AccessibleObject Parent { get; }

            public override AccessibleRole Role => AccessibleRole.PageTab;

            public override Rectangle Bounds
            {
                get
                {
                    Rectangle rectangle = _strip.GetTabBounds(_tab);
                    return ToScreen(rectangle, _strip);
                }
            }

            public override string Name
            {
                get
                {
                    return _tab.Content.DockHandler.TabText;
                }
                set
                {
                    //base.Name = value;
                }
            }
        }
    }
}
