using System.Drawing;
using System.ComponentModel;

namespace System.Windows.Forms
{
    /// <summary>
    /// Dock window base class.
    /// </summary>
    [ToolboxItem(false)]
    public partial class DockWindow : Panel, INestedPanesContainer, ISplitterDragSource
    {
        internal class DefaultSplitterControl : SplitterBase
        {
            protected override int SplitterSize => Measures.SplitterSize;

            protected override void StartDrag()
            {
                var window = Parent as DockWindow;

                window?.DockPanel.BeginDrag(window, window.RectangleToScreen(Bounds));
            }
        }

        private readonly SplitterBase _mSplitter;

        internal DockWindow(DockPanel dockPanel, DockState dockState)
        {
            NestedPanes = new NestedPaneCollection(this);
            DockPanel = dockPanel;
            DockState = dockState;
            Visible = false;

            SuspendLayout();

            if (DockState == DockState.DockLeft || DockState == DockState.DockRight ||
                DockState == DockState.DockTop || DockState == DockState.DockBottom)
            {
                _mSplitter = DockPanel.Extender.DockWindowSplitterControlFactory.CreateSplitterControl();
                Controls.Add(_mSplitter);
            }

            switch (DockState)
            {
                case DockState.DockLeft:
                    Dock = DockStyle.Left;
                    _mSplitter.Dock = DockStyle.Right;
                    break;
                case DockState.DockRight:
                    Dock = DockStyle.Right;
                    _mSplitter.Dock = DockStyle.Left;
                    break;
                case DockState.DockTop:
                    Dock = DockStyle.Top;
                    _mSplitter.Dock = DockStyle.Bottom;
                    break;
                case DockState.DockBottom:
                    Dock = DockStyle.Bottom;
                    _mSplitter.Dock = DockStyle.Top;
                    break;
                case DockState.Document:
                    Dock = DockStyle.Fill;
                    break;
                case DockState.Unknown:
                    break;
                case DockState.Float:
                    break;
                case DockState.DockTopAutoHide:
                    break;
                case DockState.DockLeftAutoHide:
                    break;
                case DockState.DockBottomAutoHide:
                    break;
                case DockState.DockRightAutoHide:
                    break;
                case DockState.Hidden:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ResumeLayout();
        }

        public VisibleNestedPaneCollection VisibleNestedPanes => NestedPanes.VisibleNestedPanes;

        public NestedPaneCollection NestedPanes { get; }

        public DockPanel DockPanel { get; }

        public DockState DockState { get; }

        public bool IsFloat => DockState == DockState.Float;

        internal DockPane DefaultPane => VisibleNestedPanes.Count == 0 ? null : VisibleNestedPanes[0];

        public Rectangle DisplayingRectangle
        {
            get
            {
                Rectangle rect = ClientRectangle;
                // if DockWindow is document, exclude the border
                if (DockState == DockState.Document)
                {
                    rect.X += 1;
                    rect.Y += 1;
                    rect.Width -= 2;
                    rect.Height -= 2;
                }
                // exclude the splitter
                else if (DockState == DockState.DockLeft)
                    rect.Width -= Measures.SplitterSize;
                else if (DockState == DockState.DockRight)
                {
                    rect.X += Measures.SplitterSize;
                    rect.Width -= Measures.SplitterSize;
                }
                else if (DockState == DockState.DockTop)
                    rect.Height -= Measures.SplitterSize;
                else if (DockState == DockState.DockBottom)
                {
                    rect.Y += Measures.SplitterSize;
                    rect.Height -= Measures.SplitterSize;
                }

                return rect;
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            VisibleNestedPanes.Refresh();
            if (VisibleNestedPanes.Count == 0)
            {
                if (Visible)
                    Visible = false;
            }
            else if (!Visible)
            {
                Visible = true;
                VisibleNestedPanes.Refresh();
            }

            base.OnLayout(levent);
        }

        #region ISplitterDragSource Members

        void ISplitterDragSource.BeginDrag(Rectangle rectSplitter)
        {
        }

        void ISplitterDragSource.EndDrag()
        {
        }

        bool ISplitterDragSource.IsVertical => DockState == DockState.DockLeft || DockState == DockState.DockRight;

        Rectangle ISplitterDragSource.DragLimitBounds
        {
            get
            {
                var rectLimit = DockPanel.DockArea;
                var location = (ModifierKeys & Keys.Shift) == 0 ? Location : DockPanel.DockArea.Location;

                if (((ISplitterDragSource) this).IsVertical)
                {
                    rectLimit.X += MeasurePane.MinSize;
                    rectLimit.Width -= 2*MeasurePane.MinSize;
                    rectLimit.Y = location.Y;
                    if ((ModifierKeys & Keys.Shift) == 0)
                        rectLimit.Height = Height;
                }
                else
                {
                    rectLimit.Y += MeasurePane.MinSize;
                    rectLimit.Height -= 2*MeasurePane.MinSize;
                    rectLimit.X = location.X;
                    if ((ModifierKeys & Keys.Shift) == 0)
                        rectLimit.Width = Width;
                }

                return DockPanel.RectangleToScreen(rectLimit);
            }
        }

        void ISplitterDragSource.MoveSplitter(int offset)
        {
            if ((ModifierKeys & Keys.Shift) != 0)
                SendToBack();

            Rectangle rectDockArea = DockPanel.DockArea;
            if (DockState == DockState.DockLeft && rectDockArea.Width > 0)
            {
                if (DockPanel.DockLeftPortion > 1)
                    DockPanel.DockLeftPortion = Width + offset;
                else
                    DockPanel.DockLeftPortion += offset/(double) rectDockArea.Width;
            }
            else if (DockState == DockState.DockRight && rectDockArea.Width > 0)
            {
                if (DockPanel.DockRightPortion > 1)
                    DockPanel.DockRightPortion = Width - offset;
                else
                    DockPanel.DockRightPortion -= offset/(double) rectDockArea.Width;
            }
            else if (DockState == DockState.DockBottom && rectDockArea.Height > 0)
            {
                if (DockPanel.DockBottomPortion > 1)
                    DockPanel.DockBottomPortion = Height - offset;
                else
                    DockPanel.DockBottomPortion -= offset/(double) rectDockArea.Height;
            }
            else if (DockState == DockState.DockTop && rectDockArea.Height > 0)
            {
                if (DockPanel.DockTopPortion > 1)
                    DockPanel.DockTopPortion = Height + offset;
                else
                    DockPanel.DockTopPortion += offset/(double) rectDockArea.Height;
            }
        }

        #region IDragSource Members

        Control IDragSource.DragControl => this;

        #endregion

        #endregion
    }

    /// <summary>
    /// Dock window of Visual Studio 2003/2005 theme.
    /// </summary>
    [ToolboxItem(false)]
    internal class DefaultDockWindow : DockWindow
    {
        internal DefaultDockWindow(DockPanel dockPanel, DockState dockState) : base(dockPanel, dockState)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // if DockWindow is document, draw the border
            if (DockState == DockState.Document)
                e.Graphics.DrawRectangle(SystemPens.ControlDark, ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width - 1, ClientRectangle.Height - 1);

            base.OnPaint(e);
        }
    }
}
