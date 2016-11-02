using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Permissions;

namespace System.Windows.Forms
{
    public sealed class DockPaneCaption : Control
    {
        private sealed class InertButton : InertButtonBase
        {
            private readonly Bitmap _mImage;
            private readonly Bitmap _mImageAutoHide;

            public InertButton(DockPaneCaption dockPaneCaption, Bitmap image, Bitmap imageAutoHide)
            {
                DockPaneCaption = dockPaneCaption;
                _mImage = image;
                _mImageAutoHide = imageAutoHide;
                RefreshChanges();
            }

            private DockPaneCaption DockPaneCaption { get; }

            private bool IsAutoHide => DockPaneCaption.DockPane.IsAutoHide;

            public override Bitmap Image => IsAutoHide ? _mImageAutoHide : _mImage;

            protected override void OnRefreshChanges()
            {
                if (DockPaneCaption.DockPane.DockPanel == null) return;
                if (DockPaneCaption.TextColor == ForeColor) return;
                ForeColor = DockPaneCaption.TextColor;
                Invalidate();
            }
        }

        internal DockPaneCaption(DockPane pane)
        {
            DockPane = pane; 

            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Selectable, false);

            SuspendLayout();

            Components = new Container();
            _mToolTip = new ToolTip(Components);

            ResumeLayout();
        }

        private DockPane DockPane { get; }

        private DockPane.AppearanceStyle Appearance => DockPane.Appearance;

        private bool HasTabPageContextMenu => DockPane.HasTabPageContextMenu;

        private void ShowTabPageContextMenu(Point position)
        {
            DockPane.ShowTabPageContextMenu(this, position);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Right)
                ShowTabPageContextMenu(new Point(e.X, e.Y));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left &&
                DockPane.DockPanel.AllowEndUserDocking &&
                DockPane.AllowDockDragAndDrop &&
                !DockHelper.IsDockStateAutoHide(DockPane.DockState) &&
                DockPane.ActiveContent != null)
                DockPane.DockPanel.BeginDrag(DockPane);
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]         
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)Win32.Msgs.WM_LBUTTONDBLCLK)
            {
                if (DockHelper.IsDockStateAutoHide(DockPane.DockState))
                {
                    DockPane.DockPanel.ActiveAutoHideContent = null;
                    return;
                }

                if (DockPane.IsFloat)
                    DockPane.RestoreToPanel();
                else
                    DockPane.Float();
            }
            base.WndProc(ref m);
        }

        internal void RefreshChanges()
        {
            if (IsDisposed)
                return;

            OnRefreshChanges();
        }

        private void OnRefreshChanges()
        {
            SetButtons();
            Invalidate();
        }

        internal int MeasureHeight()
        {
            var height = TextFont.Height + TextGapTop + TextGapBottom;

            if (height < ButtonClose.Image.Height + ButtonGapTop + ButtonGapBottom)
                height = ButtonClose.Image.Height + ButtonGapTop + ButtonGapBottom;

            return height;
        }

        #region consts
        private const int _TextGapTop = 2;
        private const int _TextGapBottom = 0;
        private const int _TextGapLeft = 3;
        private const int _TextGapRight = 3;
        private const int _ButtonGapTop = 2;
        private const int _ButtonGapBottom = 1;
        private const int _ButtonGapBetween = 1;
        private const int _ButtonGapLeft = 1;
        private const int _ButtonGapRight = 2;
        #endregion

        private static Bitmap _imageButtonClose;
        private static Bitmap ImageButtonClose => _imageButtonClose ?? (_imageButtonClose = Resources.DockPane_Close);

        private InertButton m_buttonClose;
        private InertButton ButtonClose
        {
            get
            {
                if (m_buttonClose != null) return m_buttonClose;
                m_buttonClose = new InertButton(this, ImageButtonClose, ImageButtonClose);
                _mToolTip.SetToolTip(m_buttonClose, ToolTipClose);
                m_buttonClose.Click += Close_Click;
                Controls.Add(m_buttonClose);

                return m_buttonClose;
            }
        }

        private static Bitmap _imageButtonAutoHide;
        private static Bitmap ImageButtonAutoHide => _imageButtonAutoHide ?? (_imageButtonAutoHide = Resources.DockPane_AutoHide);

        private static Bitmap _imageButtonDock;
        private static Bitmap ImageButtonDock => _imageButtonDock ?? (_imageButtonDock = Resources.DockPane_Dock);

        private InertButton _mButtonAutoHide;
        private InertButton ButtonAutoHide
        {
            get
            {
                if (_mButtonAutoHide != null) return _mButtonAutoHide;
                _mButtonAutoHide = new InertButton(this, ImageButtonDock, ImageButtonAutoHide);
                _mToolTip.SetToolTip(_mButtonAutoHide, ToolTipAutoHide);
                _mButtonAutoHide.Click += AutoHide_Click;
                Controls.Add(_mButtonAutoHide);

                return _mButtonAutoHide;
            }
        }

        private static Bitmap _imageButtonOptions;
        private static Bitmap ImageButtonOptions => _imageButtonOptions ?? (_imageButtonOptions = Resources.DockPane_Option);

        private InertButton _mButtonOptions;
        private InertButton ButtonOptions
        {
            get
            {
                if (_mButtonOptions != null) return _mButtonOptions;
                _mButtonOptions = new InertButton(this, ImageButtonOptions, ImageButtonOptions);
                _mToolTip.SetToolTip(_mButtonOptions, ToolTipOptions);
                _mButtonOptions.Click += Options_Click;
                Controls.Add(_mButtonOptions);
                return _mButtonOptions;
            }
        }

        private IContainer Components { get; }

        private readonly ToolTip _mToolTip;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Components.Dispose();
            base.Dispose(disposing);
        }

        private static int TextGapTop => _TextGapTop;

        public Font TextFont => DockPane.DockPanel.Skin.DockPaneStripSkin.TextFont;

        private static int TextGapBottom => _TextGapBottom;

        private static int TextGapLeft => _TextGapLeft;

        private static int TextGapRight => _TextGapRight;

        private static int ButtonGapTop => _ButtonGapTop;

        private static int ButtonGapBottom => _ButtonGapBottom;

        private static int ButtonGapLeft => _ButtonGapLeft;

        private static int ButtonGapRight => _ButtonGapRight;

        private static int ButtonGapBetween => _ButtonGapBetween;

        private static string _toolTipClose;
        private static string ToolTipClose => _toolTipClose ?? (_toolTipClose = Strings.DockPaneCaption_ToolTipClose);

        private static string _toolTipOptions;
        private static string ToolTipOptions => _toolTipOptions ?? (_toolTipOptions = Strings.DockPaneCaption_ToolTipOptions);

        private static string _toolTipAutoHide;
        private static string ToolTipAutoHide => _toolTipAutoHide ?? (_toolTipAutoHide = Strings.DockPaneCaption_ToolTipAutoHide);

        private static Blend _activeBackColorGradientBlend;
        private static Blend ActiveBackColorGradientBlend
        {
            get
            {
                if (_activeBackColorGradientBlend != null) return _activeBackColorGradientBlend;
                var blend = new Blend(2)
                {
                    Factors = new[] { 0.5F, 1.0F },
                    Positions = new[] { 0.0F, 1.0F }
                };

                _activeBackColorGradientBlend = blend;

                return _activeBackColorGradientBlend;
            }
        }

        private Color TextColor => DockPane.IsActivated ? DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.TextColor : DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.TextColor;

        private static TextFormatFlags _textFormat =
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.VerticalCenter;
        private TextFormatFlags TextFormat
        {
            get
            {
                if (RightToLeft == RightToLeft.No)
                    return _textFormat;
                return _textFormat | TextFormatFlags.RightToLeft | TextFormatFlags.Right;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawCaption(e.Graphics);
        }

        private void DrawCaption(Graphics g)
        {
            if (ClientRectangle.Width == 0 || ClientRectangle.Height == 0)
                return;

            if (DockPane.IsActivated)
            {
                var startColor = DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.StartColor;
                var endColor = DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.EndColor;
                var gradientMode = DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.LinearGradientMode;
                using (var brush = new LinearGradientBrush(ClientRectangle, startColor, endColor, gradientMode))
                {
                    brush.Blend = ActiveBackColorGradientBlend;
                    g.FillRectangle(brush, ClientRectangle);
                }
            }
            else
            {
                var startColor = DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.StartColor;
                var endColor = DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.EndColor;
                var gradientMode = DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.LinearGradientMode;
                using (var brush = new LinearGradientBrush(ClientRectangle, startColor, endColor, gradientMode))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
            }

            var rectCaption = ClientRectangle;

            var rectCaptionText = rectCaption;
            rectCaptionText.X += TextGapLeft;
            rectCaptionText.Width -= TextGapLeft + TextGapRight;
            rectCaptionText.Width -= ButtonGapLeft + ButtonClose.Width + ButtonGapRight;
            if (ShouldShowAutoHideButton)
                rectCaptionText.Width -= ButtonAutoHide.Width + ButtonGapBetween;
            if (HasTabPageContextMenu)
                rectCaptionText.Width -= ButtonOptions.Width + ButtonGapBetween;
            rectCaptionText.Y += TextGapTop;
            rectCaptionText.Height -= TextGapTop + TextGapBottom;

            var colorText = DockPane.IsActivated ? DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.TextColor : DockPane.DockPanel.Skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.TextColor;

            TextRenderer.DrawText(g, DockPane.CaptionText, TextFont, DrawHelper.RtlTransform(this, rectCaptionText), colorText, TextFormat);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            SetButtonsPosition();
            base.OnLayout(levent);
        }

        private bool CloseButtonEnabled => DockPane.ActiveContent?.DockHandler.CloseButton ?? false;

        /// <summary>
        /// Determines whether the close button is visible on the content
        /// </summary>
        private bool CloseButtonVisible => DockPane.ActiveContent?.DockHandler.CloseButtonVisible ?? false;

        private bool ShouldShowAutoHideButton => !DockPane.IsFloat;

        private void SetButtons()
        {
            ButtonClose.Enabled = CloseButtonEnabled;
            ButtonClose.Visible = CloseButtonVisible;
            ButtonAutoHide.Visible = ShouldShowAutoHideButton;
            ButtonOptions.Visible = HasTabPageContextMenu;
            ButtonClose.RefreshChanges();
            ButtonAutoHide.RefreshChanges();
            ButtonOptions.RefreshChanges();

            SetButtonsPosition();
        }

        private void SetButtonsPosition()
        {
            // set the size and location for close and auto-hide buttons
            Rectangle rectCaption = ClientRectangle;
            int buttonWidth = ButtonClose.Image.Width;
            int buttonHeight = ButtonClose.Image.Height;
            int height = rectCaption.Height - ButtonGapTop - ButtonGapBottom;
            if (buttonHeight < height)
            {
                buttonWidth = buttonWidth * (height / buttonHeight);
                buttonHeight = height;
            }
            Size buttonSize = new Size(buttonWidth, buttonHeight);
            int x = rectCaption.X + rectCaption.Width - 1 - ButtonGapRight - m_buttonClose.Width;
            int y = rectCaption.Y + ButtonGapTop;
            Point point = new Point(x, y);
            ButtonClose.Bounds = DrawHelper.RtlTransform(this, new Rectangle(point, buttonSize));

            // If the close button is not visible draw the auto hide button overtop.
            // Otherwise it is drawn to the left of the close button.
            if (CloseButtonVisible)
                point.Offset(-(buttonWidth + ButtonGapBetween), 0);

            ButtonAutoHide.Bounds = DrawHelper.RtlTransform(this, new Rectangle(point, buttonSize));
            if (ShouldShowAutoHideButton)
                point.Offset(-(buttonWidth + ButtonGapBetween), 0);
            ButtonOptions.Bounds = DrawHelper.RtlTransform(this, new Rectangle(point, buttonSize));
        }

        private void Close_Click(object sender, EventArgs e)
        {
            DockPane.CloseActiveContent();
        }

        private void AutoHide_Click(object sender, EventArgs e)
        {
            DockPane.DockState = DockHelper.ToggleAutoHideState(DockPane.DockState);
            if (DockHelper.IsDockStateAutoHide(DockPane.DockState))
            {
                DockPane.DockPanel.ActiveAutoHideContent = null;
                DockPane.NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(DockPane);
            }
        }

        private void Options_Click(object sender, EventArgs e)
        {
            ShowTabPageContextMenu(PointToClient(MousePosition));
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            PerformLayout();
        }
    }
}
