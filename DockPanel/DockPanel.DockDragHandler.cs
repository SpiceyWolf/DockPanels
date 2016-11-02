using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace System.Windows.Forms
{
    partial class DockPanel
    {
        #region PaneIndicator

        public interface IPaneIndicator : IHitTest
        {
            Point Location { get; set; }
            bool Visible { get; set; }
            int Left { get; }
            int Top { get; }
            int Right { get; }
            int Bottom { get; }
            Rectangle ClientRectangle { get; }
            int Width { get; }
            int Height { get; }
            GraphicsPath DisplayingGraphicsPath { get; }
        }

        public struct HotSpotIndex
        {
            public HotSpotIndex(int x, int y, DockStyle dockStyle)
            {
                X = x;
                Y = y;
                DockStyle = dockStyle;
            }

            public int X { get; }

            public int Y { get; }

            public DockStyle DockStyle { get; }
        }

        internal class DefaultPaneIndicator : PictureBox, IPaneIndicator
        {
            private static readonly Bitmap BitmapPaneDiamond = Resources.DockIndicator_PaneDiamond;
            private static readonly Bitmap BitmapPaneDiamondLeft = Resources.DockIndicator_PaneDiamond_Left;
            private static readonly Bitmap BitmapPaneDiamondRight = Resources.DockIndicator_PaneDiamond_Right;
            private static readonly Bitmap BitmapPaneDiamondTop = Resources.DockIndicator_PaneDiamond_Top;
            private static readonly Bitmap BitmapPaneDiamondBottom = Resources.DockIndicator_PaneDiamond_Bottom;
            private static readonly Bitmap BitmapPaneDiamondFill = Resources.DockIndicator_PaneDiamond_Fill;
            private static readonly Bitmap BitmapPaneDiamondHotSpot = Resources.DockIndicator_PaneDiamond_HotSpot;
            private static readonly Bitmap BitmapPaneDiamondHotSpotIndex = Resources.DockIndicator_PaneDiamond_HotSpotIndex;
            private static readonly HotSpotIndex[] HotSpots = new[]
            {
                new HotSpotIndex(1, 0, DockStyle.Top),
                new HotSpotIndex(0, 1, DockStyle.Left),
                new HotSpotIndex(1, 1, DockStyle.Fill),
                new HotSpotIndex(2, 1, DockStyle.Right),
                new HotSpotIndex(1, 2, DockStyle.Bottom)
            };

            public DefaultPaneIndicator()
            {
                SizeMode = PictureBoxSizeMode.AutoSize;
                Image = BitmapPaneDiamond;
                Region = new Region(DisplayingGraphicsPath);
            }

            public GraphicsPath DisplayingGraphicsPath { get; } = DrawHelper.CalculateGraphicsPathFromBitmap(BitmapPaneDiamond);

            public DockStyle HitTest(Point pt)
            {
                if (!Visible)
                    return DockStyle.None;

                pt = PointToClient(pt);
                if (!ClientRectangle.Contains(pt))
                    return DockStyle.None;

                for (int i = HotSpots.GetLowerBound(0); i <= HotSpots.GetUpperBound(0); i++)
                {
                    if (BitmapPaneDiamondHotSpot.GetPixel(pt.X, pt.Y) == BitmapPaneDiamondHotSpotIndex.GetPixel(HotSpots[i].X, HotSpots[i].Y))
                        return HotSpots[i].DockStyle;
                }

                return DockStyle.None;
            }

            private DockStyle _mStatus = DockStyle.None;
            public DockStyle Status
            {
                get { return _mStatus; }
                set
                {
                    _mStatus = value;
                    if (_mStatus == DockStyle.None)
                        Image = BitmapPaneDiamond;
                    else if (_mStatus == DockStyle.Left)
                        Image = BitmapPaneDiamondLeft;
                    else if (_mStatus == DockStyle.Right)
                        Image = BitmapPaneDiamondRight;
                    else if (_mStatus == DockStyle.Top)
                        Image = BitmapPaneDiamondTop;
                    else if (_mStatus == DockStyle.Bottom)
                        Image = BitmapPaneDiamondBottom;
                    else if (_mStatus == DockStyle.Fill)
                        Image = BitmapPaneDiamondFill;
                }
            }
        }
        #endregion PaneIndicator

        #region IHitTest
        public interface IHitTest
        {
            DockStyle HitTest(Point pt);
            DockStyle Status { get; set; }
        }
        #endregion

        #region PanelIndicator

        public interface IPanelIndicator : IHitTest
        {
            Point Location { get; set; }
            bool Visible { get; set; }
            Rectangle Bounds { get; }
            int Width { get; }
            int Height { get; }
        }

        internal class DefaultPanelIndicator : PictureBox, IPanelIndicator
        {
            private static readonly Image ImagePanelLeft = Resources.DockIndicator_PanelLeft;
            private static readonly Image ImagePanelRight = Resources.DockIndicator_PanelRight;
            private static readonly Image ImagePanelTop = Resources.DockIndicator_PanelTop;
            private static readonly Image ImagePanelBottom = Resources.DockIndicator_PanelBottom;
            private static readonly Image ImagePanelFill = Resources.DockIndicator_PanelFill;
            private static readonly Image ImagePanelLeftActive = Resources.DockIndicator_PanelLeft_Active;
            private static readonly Image ImagePanelRightActive = Resources.DockIndicator_PanelRight_Active;
            private static readonly Image ImagePanelTopActive = Resources.DockIndicator_PanelTop_Active;
            private static readonly Image ImagePanelBottomActive = Resources.DockIndicator_PanelBottom_Active;
            private static readonly Image ImagePanelFillActive = Resources.DockIndicator_PanelFill_Active;

            public DefaultPanelIndicator(DockStyle dockStyle)
            {
                DockStyle = dockStyle;
                SizeMode = PictureBoxSizeMode.AutoSize;
                Image = ImageInactive;
            }

            private DockStyle DockStyle { get; }

            private DockStyle _mStatus;
            public DockStyle Status
            {
                get { return _mStatus; }
                set
                {
                    if (value != DockStyle && value != DockStyle.None)
                        throw new InvalidEnumArgumentException();

                    if (_mStatus == value)
                        return;

                    _mStatus = value;
                    IsActivated = _mStatus != DockStyle.None;
                }
            }

            private Image ImageInactive
            {
                get
                {
                    if (DockStyle == DockStyle.Left)
                        return ImagePanelLeft;
                    else if (DockStyle == DockStyle.Right)
                        return ImagePanelRight;
                    else if (DockStyle == DockStyle.Top)
                        return ImagePanelTop;
                    else if (DockStyle == DockStyle.Bottom)
                        return ImagePanelBottom;
                    else if (DockStyle == DockStyle.Fill)
                        return ImagePanelFill;
                    else
                        return null;
                }
            }

            private Image ImageActive
            {
                get
                {
                    if (DockStyle == DockStyle.Left)
                        return ImagePanelLeftActive;
                    else if (DockStyle == DockStyle.Right)
                        return ImagePanelRightActive;
                    else if (DockStyle == DockStyle.Top)
                        return ImagePanelTopActive;
                    else if (DockStyle == DockStyle.Bottom)
                        return ImagePanelBottomActive;
                    else if (DockStyle == DockStyle.Fill)
                        return ImagePanelFillActive;
                    else
                        return null;
                }
            }

            private bool _mIsActivated;
            private bool IsActivated
            {
                get { return _mIsActivated; }
                set
                {
                    _mIsActivated = value;
                    Image = IsActivated ? ImageActive : ImageInactive;
                }
            }

            public DockStyle HitTest(Point pt)
            {
                return Visible && ClientRectangle.Contains(PointToClient(pt)) ? DockStyle : DockStyle.None;
            }
        }
        #endregion PanelIndicator

        internal class DefaultDockOutline : DockOutlineBase
        {
            public DefaultDockOutline()
            {
                DragForm = new DragForm();
                SetDragForm(Rectangle.Empty);
                DragForm.BackColor = SystemColors.ActiveCaption;
                DragForm.Opacity = 0.5;
                DragForm.Show(false);
            }

            private DragForm DragForm { get; }

            protected override void OnShow()
            {
                CalculateRegion();
            }

            protected override void OnClose()
            {
                DragForm.Close();
            }

            private void CalculateRegion()
            {
                if (SameAsOldValue)
                    return;

                if (!FloatWindowBounds.IsEmpty)
                    SetOutline(FloatWindowBounds);
                else if (DockTo is DockPanel)
                    SetOutline((DockPanel) DockTo, Dock, ContentIndex != 0);
                else if (DockTo is DockPane)
                    SetOutline((DockPane) DockTo, Dock, ContentIndex);
                else
                    SetOutline();
            }

            private void SetOutline()
            {
                SetDragForm(Rectangle.Empty);
            }

            private void SetOutline(Rectangle floatWindowBounds)
            {
                SetDragForm(floatWindowBounds);
            }

            private void SetOutline(DockPanel dockPanel, DockStyle dock, bool fullPanelEdge)
            {
                Rectangle rect = fullPanelEdge ? dockPanel.DockArea : dockPanel.DocumentWindowBounds;
                rect.Location = dockPanel.PointToScreen(rect.Location);
                if (dock == DockStyle.Top)
                {
                    int height = dockPanel.GetDockWindowSize(DockState.DockTop);
                    rect = new Rectangle(rect.X, rect.Y, rect.Width, height);
                }
                else if (dock == DockStyle.Bottom)
                {
                    int height = dockPanel.GetDockWindowSize(DockState.DockBottom);
                    rect = new Rectangle(rect.X, rect.Bottom - height, rect.Width, height);
                }
                else if (dock == DockStyle.Left)
                {
                    int width = dockPanel.GetDockWindowSize(DockState.DockLeft);
                    rect = new Rectangle(rect.X, rect.Y, width, rect.Height);
                }
                else if (dock == DockStyle.Right)
                {
                    int width = dockPanel.GetDockWindowSize(DockState.DockRight);
                    rect = new Rectangle(rect.Right - width, rect.Y, width, rect.Height);
                }
                else if (dock == DockStyle.Fill)
                {
                    rect = dockPanel.DocumentWindowBounds;
                    rect.Location = dockPanel.PointToScreen(rect.Location);
                }

                SetDragForm(rect);
            }

            private void SetOutline(DockPane pane, DockStyle dock, int contentIndex)
            {
                if (dock != DockStyle.Fill)
                {
                    Rectangle rect = pane.DisplayingRectangle;
                    if (dock == DockStyle.Right)
                        rect.X += rect.Width / 2;
                    if (dock == DockStyle.Bottom)
                        rect.Y += rect.Height / 2;
                    if (dock == DockStyle.Left || dock == DockStyle.Right)
                        rect.Width -= rect.Width / 2;
                    if (dock == DockStyle.Top || dock == DockStyle.Bottom)
                        rect.Height -= rect.Height / 2;
                    rect.Location = pane.PointToScreen(rect.Location);

                    SetDragForm(rect);
                }
                else if (contentIndex == -1)
                {
                    Rectangle rect = pane.DisplayingRectangle;
                    rect.Location = pane.PointToScreen(rect.Location);
                    SetDragForm(rect);
                }
                else
                {
                    using (GraphicsPath path = pane.TabStripControl.GetOutline(contentIndex))
                    {
                        RectangleF rectF = path.GetBounds();
                        Rectangle rect = new Rectangle((int)rectF.X, (int)rectF.Y, (int)rectF.Width, (int)rectF.Height);
                        using (Matrix matrix = new Matrix(rect, new[] { new Point(0, 0), new Point(rect.Width, 0), new Point(0, rect.Height) }))
                        {
                            path.Transform(matrix);
                        }
                        Region region = new Region(path);
                        SetDragForm(rect, region);
                    }
                }
            }

            private void SetDragForm(Rectangle rect)
            {
                DragForm.Bounds = rect;
                if (rect == Rectangle.Empty)
                {
                    DragForm.Region?.Dispose();

                    DragForm.Region = new Region(Rectangle.Empty);
                }
                else if (DragForm.Region != null)
                {
                    DragForm.Region.Dispose();
                    DragForm.Region = null;
                }
            }

            private void SetDragForm(Rectangle rect, Region region)
            {
                DragForm.Bounds = rect;
                DragForm.Region = region;
            }
        }

        private sealed class DockDragHandler : DragHandler
        {
            private class DockIndicator : DragForm
            {
                #region consts
                private int _PanelIndicatorMargin = 10;
                #endregion

                public DockIndicator(DockDragHandler dragHandler)
                {
                    DragHandler = dragHandler;
                    Controls.AddRange(new[] {
                        (Control)PaneDiamond,
                        (Control)PanelLeft,
                        (Control)PanelRight,
                        (Control)PanelTop,
                        (Control)PanelBottom,
                        (Control)PanelFill
                        });
                    Region = new Region(Rectangle.Empty);
                }

                private IPaneIndicator _mPaneDiamond;
                private IPaneIndicator PaneDiamond => _mPaneDiamond ??
                                                      (_mPaneDiamond =
                                                          DragHandler.DockPanel.Extender.PaneIndicatorFactory.CreatePaneIndicator());

                private IPanelIndicator _mPanelLeft;
                private IPanelIndicator PanelLeft => _mPanelLeft ??
                                                     (_mPanelLeft =
                                                         DragHandler.DockPanel.Extender.PanelIndicatorFactory.CreatePanelIndicator(
                                                             DockStyle.Left));

                private IPanelIndicator _mPanelRight;
                private IPanelIndicator PanelRight => _mPanelRight ??
                                                      (_mPanelRight =
                                                          DragHandler.DockPanel.Extender.PanelIndicatorFactory.CreatePanelIndicator(
                                                              DockStyle.Right));

                private IPanelIndicator _mPanelTop;
                private IPanelIndicator PanelTop => _mPanelTop ??
                                                    (_mPanelTop =
                                                        DragHandler.DockPanel.Extender.PanelIndicatorFactory.CreatePanelIndicator(
                                                            DockStyle.Top));

                private IPanelIndicator _mPanelBottom;
                private IPanelIndicator PanelBottom => _mPanelBottom ??
                                                       (_mPanelBottom =
                                                           DragHandler.DockPanel.Extender.PanelIndicatorFactory.CreatePanelIndicator(
                                                               DockStyle.Bottom));

                private IPanelIndicator _mPanelFill;
                private IPanelIndicator PanelFill => _mPanelFill ??
                                                     (_mPanelFill =
                                                         DragHandler.DockPanel.Extender.PanelIndicatorFactory.CreatePanelIndicator(
                                                             DockStyle.Fill));

                private bool _mFullPanelEdge;
                public bool FullPanelEdge
                {
                    private get { return _mFullPanelEdge; }
                    set
                    {
                        if (_mFullPanelEdge == value)
                            return;

                        _mFullPanelEdge = value;
                        RefreshChanges();
                    }
                }

                private DockDragHandler DragHandler { get; }

                private DockPanel DockPanel => DragHandler.DockPanel;

                private DockPane _mDockPane;
                public DockPane DockPane
                {
                    private get { return _mDockPane; }
                    set
                    {
                        if (_mDockPane == value)
                            return;

                        DockPane oldDisplayingPane = DisplayingPane;
                        _mDockPane = value;
                        if (oldDisplayingPane != DisplayingPane)
                            RefreshChanges();
                    }
                }

                private IHitTest _mHitTest;
                private IHitTest HitTestResult
                {
                    get { return _mHitTest; }
                    set
                    {
                        if (_mHitTest == value)
                            return;

                        if (_mHitTest != null)
                            _mHitTest.Status = DockStyle.None;

                        _mHitTest = value;
                    }
                }

                private DockPane DisplayingPane => ShouldPaneDiamondVisible() ? DockPane : null;

                private void RefreshChanges()
                {
                    var region = new Region(Rectangle.Empty);
                    var rectDockArea = FullPanelEdge ? DockPanel.DockArea : DockPanel.DocumentWindowBounds;

                    rectDockArea = RectangleToClient(DockPanel.RectangleToScreen(rectDockArea));
                    if (ShouldPanelIndicatorVisible(DockState.DockLeft))
                    {
                        PanelLeft.Location = new Point(rectDockArea.X + _PanelIndicatorMargin, rectDockArea.Y + (rectDockArea.Height - PanelRight.Height) / 2);
                        PanelLeft.Visible = true;
                        region.Union(PanelLeft.Bounds);
                    }
                    else
                        PanelLeft.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.DockRight))
                    {
                        PanelRight.Location = new Point(rectDockArea.X + rectDockArea.Width - PanelRight.Width - _PanelIndicatorMargin, rectDockArea.Y + (rectDockArea.Height - PanelRight.Height) / 2);
                        PanelRight.Visible = true;
                        region.Union(PanelRight.Bounds);
                    }
                    else
                        PanelRight.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.DockTop))
                    {
                        PanelTop.Location = new Point(rectDockArea.X + (rectDockArea.Width - PanelTop.Width) / 2, rectDockArea.Y + _PanelIndicatorMargin);
                        PanelTop.Visible = true;
                        region.Union(PanelTop.Bounds);
                    }
                    else
                        PanelTop.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.DockBottom))
                    {
                        PanelBottom.Location = new Point(rectDockArea.X + (rectDockArea.Width - PanelBottom.Width) / 2, rectDockArea.Y + rectDockArea.Height - PanelBottom.Height - _PanelIndicatorMargin);
                        PanelBottom.Visible = true;
                        region.Union(PanelBottom.Bounds);
                    }
                    else
                        PanelBottom.Visible = false;

                    if (ShouldPanelIndicatorVisible(DockState.Document))
                    {
                        Rectangle rectDocumentWindow = RectangleToClient(DockPanel.RectangleToScreen(DockPanel.DocumentWindowBounds));
                        PanelFill.Location = new Point(rectDocumentWindow.X + (rectDocumentWindow.Width - PanelFill.Width) / 2, rectDocumentWindow.Y + (rectDocumentWindow.Height - PanelFill.Height) / 2);
                        PanelFill.Visible = true;
                        region.Union(PanelFill.Bounds);
                    }
                    else
                        PanelFill.Visible = false;

                    if (ShouldPaneDiamondVisible())
                    {
                        Rectangle rect = RectangleToClient(DockPane.RectangleToScreen(DockPane.ClientRectangle));
                        PaneDiamond.Location = new Point(rect.Left + (rect.Width - PaneDiamond.Width) / 2, rect.Top + (rect.Height - PaneDiamond.Height) / 2);
                        PaneDiamond.Visible = true;
                        using (GraphicsPath graphicsPath = PaneDiamond.DisplayingGraphicsPath.Clone() as GraphicsPath)
                        {
                            Point[] pts = new Point[]
                                {
                                    new Point(PaneDiamond.Left, PaneDiamond.Top),
                                    new Point(PaneDiamond.Right, PaneDiamond.Top),
                                    new Point(PaneDiamond.Left, PaneDiamond.Bottom)
                                };
                            using (Matrix matrix = new Matrix(PaneDiamond.ClientRectangle, pts))
                            {
                                graphicsPath?.Transform(matrix);
                            }

                            if (graphicsPath != null) region.Union(graphicsPath);
                        }
                    }
                    else
                        PaneDiamond.Visible = false;

                    Region = region;
                }

                private bool ShouldPanelIndicatorVisible(DockState dockState)
                {
                    if (!Visible)
                        return false;

                    if (DockPanel.DockWindows[dockState].Visible)
                        return false;

                    return DragHandler.DragSource.IsDockStateValid(dockState);
                }

                private bool ShouldPaneDiamondVisible()
                {
                    if (DockPane == null)
                        return false;

                    if (!DockPanel.AllowEndUserNestedDocking)
                        return false;

                    return DragHandler.DragSource.CanDockTo(DockPane);
                }

                public override void Show(bool bActivate)
                {
                    base.Show(bActivate);
                    Bounds = SystemInformation.VirtualScreen;
                    RefreshChanges();
                }

                public void TestDrop()
                {
                    Point pt = MousePosition;
                    DockPane = DockHelper.PaneAtPoint(pt, DockPanel);

                    if (TestDrop(PanelLeft, pt) != DockStyle.None)
                        HitTestResult = PanelLeft;
                    else if (TestDrop(PanelRight, pt) != DockStyle.None)
                        HitTestResult = PanelRight;
                    else if (TestDrop(PanelTop, pt) != DockStyle.None)
                        HitTestResult = PanelTop;
                    else if (TestDrop(PanelBottom, pt) != DockStyle.None)
                        HitTestResult = PanelBottom;
                    else if (TestDrop(PanelFill, pt) != DockStyle.None)
                        HitTestResult = PanelFill;
                    else if (TestDrop(PaneDiamond, pt) != DockStyle.None)
                        HitTestResult = PaneDiamond;
                    else
                        HitTestResult = null;

                    if (HitTestResult != null)
                    {
                        if (HitTestResult is IPaneIndicator)
                            DragHandler.Outline.Show(DockPane, HitTestResult.Status);
                        else
                            DragHandler.Outline.Show(DockPanel, HitTestResult.Status, FullPanelEdge);
                    }
                }

                private static DockStyle TestDrop(IHitTest hitTest, Point pt)
                {
                    return hitTest.Status = hitTest.HitTest(pt);
                }
            }

            public DockDragHandler(DockPanel panel)
                : base(panel)
            {
            }

            private new IDockDragSource DragSource
            {
                get { return base.DragSource as IDockDragSource; }
                set { base.DragSource = value; }
            }

            private DockOutlineBase Outline { get; set; }

            private DockIndicator Indicator { get; set; }

            private Rectangle FloatOutlineBounds { get; set; }

            public void BeginDrag(IDockDragSource dragSource)
            {
                DragSource = dragSource;

                if (!BeginDrag())
                {
                    DragSource = null;
                    return;
                }

                Outline = DockPanel.Extender.DockOutlineFactory.CreateDockOutline();
                Indicator = new DockIndicator(this);
                Indicator.Show(false);

                FloatOutlineBounds = DragSource.BeginDrag(StartMousePosition);
            }

            protected override void OnDragging()
            {
                TestDrop();
            }

            protected override void OnEndDrag(bool abort)
            {
                DockPanel.SuspendLayout(true);

                Outline.Close();
                Indicator.Close();

                EndDrag(abort);

                // Queue a request to layout all children controls
                DockPanel.PerformMdiClientLayout();

                DockPanel.ResumeLayout(true, true);

                DragSource.EndDrag();

                DragSource = null;

                // Fire notification
                DockPanel.OnDocumentDragged();
            }

            private void TestDrop()
            {
                Outline.FlagTestDrop = false;

                Indicator.FullPanelEdge = (ModifierKeys & Keys.Shift) != 0;

                if ((ModifierKeys & Keys.Control) == 0)
                {
                    Indicator.TestDrop();

                    if (!Outline.FlagTestDrop)
                    {
                        DockPane pane = DockHelper.PaneAtPoint(MousePosition, DockPanel);
                        if (pane != null && DragSource.IsDockStateValid(pane.DockState))
                            pane.TestDrop(DragSource, Outline);
                    }

                    if (!Outline.FlagTestDrop && DragSource.IsDockStateValid(DockState.Float))
                    {
                        FloatWindow floatWindow = DockHelper.FloatWindowAtPoint(MousePosition, DockPanel);
                        floatWindow?.TestDrop(DragSource, Outline);
                    }
                }
                else
                    Indicator.DockPane = DockHelper.PaneAtPoint(MousePosition, DockPanel);

                if (!Outline.FlagTestDrop)
                {
                    if (DragSource.IsDockStateValid(DockState.Float))
                    {
                        Rectangle rect = FloatOutlineBounds;
                        rect.Offset(MousePosition.X - StartMousePosition.X, MousePosition.Y - StartMousePosition.Y);
                        Outline.Show(rect);
                    }
                }

                if (!Outline.FlagTestDrop)
                {
                    Cursor.Current = Cursors.No;
                    Outline.Show();
                }
                else
                    Cursor.Current = DragControl.Cursor;
            }

            private void EndDrag(bool abort)
            {
                if (abort)
                    return;

                if (!Outline.FloatWindowBounds.IsEmpty)
                    DragSource.FloatAt(Outline.FloatWindowBounds);
                else if (Outline.DockTo is DockPane)
                {
                    var pane = (DockPane) Outline.DockTo;
                    DragSource.DockTo(pane, Outline.Dock, Outline.ContentIndex);
                }
                else if (Outline.DockTo is DockPanel)
                {
                    var panel = (DockPanel) Outline.DockTo;
                    panel.UpdateDockWindowZOrder(Outline.Dock, Outline.FlagFullEdge);
                    DragSource.DockTo(panel, Outline.Dock);
                }
            }
        }

        private DockDragHandler _mDockDragHandler;
        private DockDragHandler GetDockDragHandler()
        {
            return _mDockDragHandler ?? (_mDockDragHandler = new DockDragHandler(this));
        }

        internal void BeginDrag(IDockDragSource dragSource)
        {
            GetDockDragHandler().BeginDrag(dragSource);
        }
    }
}
