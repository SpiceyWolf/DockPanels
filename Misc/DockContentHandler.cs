using System.Drawing;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Forms
{
    public delegate string GetPersistStringCallback();

    public class DockContentHandler : IDisposable, IDockDragSource
    {
        public DockContentHandler(Form form) : this(form, null)
        {
        }

        public DockContentHandler(Form form, GetPersistStringCallback getPersistStringCallback)
        {
            if (!(form is IDockContent))
                throw new ArgumentException(Strings.DockContent_Constructor_InvalidForm, nameof(form));

            Form = form;
            GetPersistStringCallback = getPersistStringCallback;

            Events = new EventHandlerList();
            Form.Disposed += Form_Disposed;
            Form.TextChanged += Form_TextChanged;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DockPanel = null;
                _mAutoHideTab?.Dispose();
                _mTab?.Dispose();

                Form.Disposed -= Form_Disposed;
                Form.TextChanged -= Form_TextChanged;
                Events.Dispose();
            }
        }

        public Form Form { get; }

        public IDockContent Content => Form as IDockContent;

        public IDockContent PreviousActive { get; internal set; }

        public IDockContent NextActive { get; internal set; }

        private EventHandlerList Events { get; }

        public bool AllowEndUserDocking { get; set; } = true;

        private double _mAutoHidePortion = 0.25;
        public double AutoHidePortion
        {
            get { return _mAutoHidePortion; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(Strings.DockContentHandler_AutoHidePortion_OutOfRange);

                if (_mAutoHidePortion == value)
                    return;

                _mAutoHidePortion = value;

                if (DockPanel == null)
                    return;

                if (DockPanel.ActiveAutoHideContent == Content)
                    DockPanel.PerformLayout();
            }
        }

        private bool _mCloseButton = true;
        public bool CloseButton
        {
            get { return _mCloseButton; }
            set
            {
                if (_mCloseButton == value)
                    return;

                _mCloseButton = value;
                if (IsActiveContentHandler)
                    Pane.RefreshChanges();
            }
        }

        private bool _mCloseButtonVisible = true;
        /// <summary>
        /// Determines whether the close button is visible on the content
        /// </summary>
        public bool CloseButtonVisible
        {
            get { return _mCloseButtonVisible; }
            set
            {
                if (_mCloseButtonVisible == value)
                    return;

                _mCloseButtonVisible = value;
                if (IsActiveContentHandler)
                    Pane.RefreshChanges();
            }
        }

        private bool IsActiveContentHandler => Pane?.ActiveContent != null && Pane.ActiveContent.DockHandler == this;

        private DockState DefaultDockState
        {
            get
            {
                if (ShowHint != DockState.Unknown && ShowHint != DockState.Hidden)
                    return ShowHint;

                if ((DockAreas & DockAreas.Document) != 0)
                    return DockState.Document;
                if ((DockAreas & DockAreas.DockRight) != 0)
                    return DockState.DockRight;
                if ((DockAreas & DockAreas.DockLeft) != 0)
                    return DockState.DockLeft;
                if ((DockAreas & DockAreas.DockBottom) != 0)
                    return DockState.DockBottom;
                if ((DockAreas & DockAreas.DockTop) != 0)
                    return DockState.DockTop;

                return DockState.Unknown;
            }
        }

        private DockState DefaultShowState
        {
            get
            {
                if (ShowHint != DockState.Unknown)
                    return ShowHint;

                if ((DockAreas & DockAreas.Document) != 0)
                    return DockState.Document;
                if ((DockAreas & DockAreas.DockRight) != 0)
                    return DockState.DockRight;
                if ((DockAreas & DockAreas.DockLeft) != 0)
                    return DockState.DockLeft;
                if ((DockAreas & DockAreas.DockBottom) != 0)
                    return DockState.DockBottom;
                if ((DockAreas & DockAreas.DockTop) != 0)
                    return DockState.DockTop;
                if ((DockAreas & DockAreas.Float) != 0)
                    return DockState.Float;

                return DockState.Unknown;
            }
        }

        private DockAreas _mAllowedAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.DockTop | DockAreas.DockBottom | DockAreas.Document | DockAreas.Float;
        public DockAreas DockAreas
        {
            get { return _mAllowedAreas; }
            set
            {
                if (_mAllowedAreas == value)
                    return;

                if (!DockHelper.IsDockStateValid(DockState, value))
                    throw new InvalidOperationException(Strings.DockContentHandler_DockAreas_InvalidValue);

                _mAllowedAreas = value;

                if (!DockHelper.IsDockStateValid(ShowHint, _mAllowedAreas))
                    ShowHint = DockState.Unknown;
            }
        }

        private DockState _mDockState = DockState.Unknown;
        public DockState DockState
        {
            get { return _mDockState; }
            set
            {
                if (_mDockState == value)
                    return;

                DockPanel.SuspendLayout(true);

                if (value == DockState.Hidden)
                    IsHidden = true;
                else
                    SetDockState(false, value, Pane);

                DockPanel.ResumeLayout(true, true);
            }
        }

        private DockPanel _mDockPanel;
        public DockPanel DockPanel
        {
            get { return _mDockPanel; }
            set
            {
                if (_mDockPanel == value)
                    return;

                Pane = null;

                _mDockPanel?.RemoveContent(Content);

                if (_mTab != null)
                {
                    _mTab.Dispose();
                    _mTab = null;
                }

                if (_mAutoHideTab != null)
                {
                    _mAutoHideTab.Dispose();
                    _mAutoHideTab = null;
                }

                _mDockPanel = value;

                if (_mDockPanel != null)
                {
                    _mDockPanel.AddContent(Content);
                    Form.TopLevel = false;
                    Form.FormBorderStyle = FormBorderStyle.None;
                    Form.ShowInTaskbar = false;
                    Form.WindowState = FormWindowState.Normal;
                    if (Win32Helper.IsRunningOnMono)
                        return;

                    NativeMethods.SetWindowPos(Form.Handle, IntPtr.Zero, 0, 0, 0, 0,
                        Win32.FlagsSetWindowPos.SWP_NOACTIVATE |
                        Win32.FlagsSetWindowPos.SWP_NOMOVE |
                        Win32.FlagsSetWindowPos.SWP_NOSIZE |
                        Win32.FlagsSetWindowPos.SWP_NOZORDER |
                        Win32.FlagsSetWindowPos.SWP_NOOWNERZORDER |
                        Win32.FlagsSetWindowPos.SWP_FRAMECHANGED);
                }
            }
        }

        public Icon Icon => Form.Icon;

        public DockPane Pane
        {
            get { return IsFloat ? FloatPane : PanelPane; }
            set
            {
                if (Pane == value)
                    return;

                DockPanel.SuspendLayout(true);

                DockPane oldPane = Pane;

                SuspendSetDockState();
                FloatPane = value == null ? null : (value.IsFloat ? value : FloatPane);
                PanelPane = value == null ? null : (value.IsFloat ? PanelPane : value);
                ResumeSetDockState(IsHidden, value?.DockState ?? DockState.Unknown, oldPane);

                DockPanel.ResumeLayout(true, true);
            }
        }

        private bool _mIsHidden = true;
        public bool IsHidden
        {
            get { return _mIsHidden; }
            set
            {
                if (_mIsHidden == value)
                    return;

                SetDockState(value, VisibleState, Pane);
            }
        }

        private string _mTabText;
        public string TabText
        {
            get
            {
                if (!string.IsNullOrEmpty(_mTabText)) return _mTabText;

                if (string.IsNullOrEmpty(Form.Text))
                    return "   ";

                var tex = Form.Text;
                var len = Form.Text.Length;
                while (len < 3)
                {
                    tex += " ";
                    len++;
                }

                return tex;
            }
            set
            {
                if (_mTabText == value)
                    return;

                _mTabText = value;
                Pane?.RefreshChanges();
            }
        }

        private DockState _mVisibleState = DockState.Unknown;
        public DockState VisibleState
        {
            get { return _mVisibleState; }
            set
            {
                if (_mVisibleState == value)
                    return;

                SetDockState(IsHidden, value, Pane);
            }
        }

        private bool _mIsFloat;
        public bool IsFloat
        {
            get { return _mIsFloat; }
            set
            {
                if (_mIsFloat == value)
                    return;

                DockState visibleState = CheckDockState(value);

                if (visibleState == DockState.Unknown)
                    throw new InvalidOperationException(Strings.DockContentHandler_IsFloat_InvalidValue);

                SetDockState(IsHidden, visibleState, Pane);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters")]
        public DockState CheckDockState(bool isFloat)
        {
            DockState dockState;

            if (isFloat)
            {
                dockState = !IsDockStateValid(DockState.Float) ? DockState.Unknown : DockState.Float;
            }
            else
            {
                dockState = PanelPane?.DockState ?? DefaultDockState;
                if (dockState != DockState.Unknown && !IsDockStateValid(dockState))
                    dockState = DockState.Unknown;
            }

            return dockState;
        }

        private DockPane _mPanelPane;
        public DockPane PanelPane
        {
            get { return _mPanelPane; }
            set
            {
                if (_mPanelPane == value)
                    return;

                if (value != null)
                {
                    if (value.IsFloat || value.DockPanel != DockPanel)
                        throw new InvalidOperationException(Strings.DockContentHandler_DockPane_InvalidValue);
                }

                DockPane oldPane = Pane;

                if (_mPanelPane != null)
                    RemoveFromPane(_mPanelPane);
                _mPanelPane = value;
                if (_mPanelPane != null)
                {
                    _mPanelPane.AddContent(Content);
                    SetDockState(IsHidden, IsFloat ? DockState.Float : _mPanelPane.DockState, oldPane);
                }
                else
                    SetDockState(IsHidden, DockState.Unknown, oldPane);
            }
        }

        private void RemoveFromPane(DockPane pane)
        {
            pane.RemoveContent(Content);
            SetPane(null);
            if (pane.Contents.Count == 0)
                pane.Dispose();
        }

        private DockPane _mFloatPane;
        public DockPane FloatPane
        {
            get { return _mFloatPane; }
            set
            {
                if (_mFloatPane == value)
                    return;

                if (value != null)
                {
                    if (!value.IsFloat || value.DockPanel != DockPanel)
                        throw new InvalidOperationException(Strings.DockContentHandler_FloatPane_InvalidValue);
                }

                DockPane oldPane = Pane;

                if (_mFloatPane != null)
                    RemoveFromPane(_mFloatPane);
                _mFloatPane = value;
                if (_mFloatPane != null)
                {
                    _mFloatPane.AddContent(Content);
                    SetDockState(IsHidden, IsFloat ? DockState.Float : VisibleState, oldPane);
                }
                else
                    SetDockState(IsHidden, DockState.Unknown, oldPane);
            }
        }

        private int _mCountSetDockState;
        private void SuspendSetDockState()
        {
            _mCountSetDockState++;
        }

        private void ResumeSetDockState()
        {
            _mCountSetDockState--;
            if (_mCountSetDockState < 0)
                _mCountSetDockState = 0;
        }

        internal bool IsSuspendSetDockState => _mCountSetDockState != 0;

        private void ResumeSetDockState(bool isHidden, DockState visibleState, DockPane oldPane)
        {
            ResumeSetDockState();
            SetDockState(isHidden, visibleState, oldPane);
        }

        internal void SetDockState(bool isHidden, DockState visibleState, DockPane oldPane)
        {
            if (IsSuspendSetDockState)
                return;

            if (DockPanel == null && visibleState != DockState.Unknown)
                throw new InvalidOperationException(Strings.DockContentHandler_SetDockState_NullPanel);

            if (visibleState == DockState.Hidden || (visibleState != DockState.Unknown && !IsDockStateValid(visibleState)))
                throw new InvalidOperationException(Strings.DockContentHandler_SetDockState_InvalidState);

            DockPanel dockPanel = DockPanel;
            dockPanel?.SuspendLayout(true);

            SuspendSetDockState();

            DockState oldDockState = DockState;

            if (_mIsHidden != isHidden || oldDockState == DockState.Unknown)
            {
                _mIsHidden = isHidden;
            }
            _mVisibleState = visibleState;
            _mDockState = isHidden ? DockState.Hidden : visibleState;

            if (visibleState == DockState.Unknown)
                Pane = null;
            else
            {
                _mIsFloat = _mVisibleState == DockState.Float;

                if (Pane == null)
                    Pane = DockPanel?.DockPaneFactory.CreateDockPane(Content, visibleState, true);
                else if (Pane.DockState != visibleState)
                {
                    if (Pane.Contents.Count == 1)
                        Pane.SetDockState(visibleState);
                    else
                        Pane = DockPanel?.DockPaneFactory.CreateDockPane(Content, visibleState, true);
                }
            }

            if (Form.ContainsFocus)
            {
                if (DockState == DockState.Hidden || DockState == DockState.Unknown)
                {
                    if (!Win32Helper.IsRunningOnMono)
                    {
                        DockPanel?.ContentFocusManager.GiveUpFocus(Content);
                    }
                }
            }

            SetPaneAndVisible(Pane);

            if (oldPane != null && !oldPane.IsDisposed && oldDockState == oldPane.DockState)
                RefreshDockPane(oldPane);

            if (Pane != null && DockState == Pane.DockState)
            {
                if (oldPane != null && ((Pane != oldPane) ||
                                        (Pane == oldPane && oldDockState != oldPane.DockState)))
                {
                    // Avoid early refresh of hidden AutoHide panes
                    if (Pane != null && (Pane.DockWindow == null || Pane.DockWindow.Visible || Pane.IsHidden) && !Pane.IsAutoHide)
                    {
                        RefreshDockPane(Pane);
                    }
                }
            }

            if (oldDockState != DockState)
            {
                if (DockState == DockState.Hidden || DockState == DockState.Unknown ||
                    DockHelper.IsDockStateAutoHide(DockState))
                {
                    if (!Win32Helper.IsRunningOnMono)
                    {
                        DockPanel?.ContentFocusManager.RemoveFromList(Content);
                    }
                }
                else if (!Win32Helper.IsRunningOnMono)
                {
                    DockPanel?.ContentFocusManager.AddToList(Content);
                }

                ResetAutoHidePortion(oldDockState, DockState);
                OnDockStateChanged(EventArgs.Empty);
            }

            ResumeSetDockState();

            dockPanel?.ResumeLayout(true, true);
        }

        private void ResetAutoHidePortion(DockState oldState, DockState newState)
        {
            if (oldState == newState || DockHelper.ToggleAutoHideState(oldState) == newState)
                return;

            switch (newState)
            {
                case DockState.DockTop:
                case DockState.DockTopAutoHide:
                    AutoHidePortion = DockPanel.DockTopPortion;
                    break;
                case DockState.DockLeft:
                case DockState.DockLeftAutoHide:
                    AutoHidePortion = DockPanel.DockLeftPortion;
                    break;
                case DockState.DockBottom:
                case DockState.DockBottomAutoHide:
                    AutoHidePortion = DockPanel.DockBottomPortion;
                    break;
                case DockState.DockRight:
                case DockState.DockRightAutoHide:
                    AutoHidePortion = DockPanel.DockRightPortion;
                    break;
            }
        }

        private static void RefreshDockPane(DockPane pane)
        {
            pane.RefreshChanges();
            pane.ValidateActiveContent();
        }

        internal string PersistString => GetPersistStringCallback == null ? Form.GetType().ToString() : GetPersistStringCallback();

        public GetPersistStringCallback GetPersistStringCallback { get; set; }


        public bool HideOnClose { get; set; }

        private DockState _mShowHint = DockState.Unknown;
        public DockState ShowHint
        {
            get { return _mShowHint; }
            set
            {
                if (!DockHelper.IsDockStateValid(value, DockAreas))
                    throw new InvalidOperationException(Strings.DockContentHandler_ShowHint_InvalidValue);

                _mShowHint = value;
            }
        }

        public bool IsActivated { get; internal set; }

        public bool IsDockStateValid(DockState dockState)
        {
            return DockHelper.IsDockStateValid(dockState, DockAreas);
        }

        public ContextMenu TabPageContextMenu { get; set; }

        public string ToolTipText { get; set; }

        public void Activate()
        {
            if (DockPanel == null)
                Form.Activate();
            else if (Pane == null)
                Show(DockPanel);
            else
            {
                IsHidden = false;
                Pane.ActiveContent = Content;
                if (DockHelper.IsDockStateAutoHide(DockState))
                {
                    if (DockPanel.ActiveAutoHideContent != Content)
                    {
                        DockPanel.ActiveAutoHideContent = null;
                        return;
                    }
                }

                if (Form.ContainsFocus)
                    return;

                if (Win32Helper.IsRunningOnMono)
                    return;

                DockPanel.ContentFocusManager.Activate(Content);
            }
        }

        public void GiveUpFocus()
        {
            if (!Win32Helper.IsRunningOnMono)
                DockPanel.ContentFocusManager.GiveUpFocus(Content);
        }

        internal IntPtr ActiveWindowHandle { get; set; } = IntPtr.Zero;

        public void Hide()
        {
            IsHidden = true;
        }

        internal void SetPaneAndVisible(DockPane pane)
        {
            SetPane(pane);
            SetVisible();
        }

        private void SetPane(DockPane pane)
        {
            FlagClipWindow = true;
            if (Form.MdiParent != null)
                Form.MdiParent = null;
            if (Form.TopLevel)
                Form.TopLevel = false;
            SetParent(pane);
        }

        internal void SetVisible()
        {
            bool visible;

            if (IsHidden)
                visible = false;
            else if (Pane != null && Pane.ActiveContent == Content)
                visible = true;
            else if (Pane != null && Pane.ActiveContent != Content)
                visible = false;
            else
                visible = Form.Visible;

            if (Form.Visible != visible)
                Form.Visible = visible;
        }

        private void SetParent(Control value)
        {
            if (Form.Parent == value)
                return;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Workaround of .Net Framework bug:
            // Change the parent of a control with focus may result in the first
            // MDI child form get activated. 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            bool bRestoreFocus = false;
            if (Form.ContainsFocus)
            {
                // Suggested as a fix for a memory leak by bugreports
                if (value == null && !IsFloat)
                {
                    if (!Win32Helper.IsRunningOnMono)
                    {
                        DockPanel.ContentFocusManager.GiveUpFocus(Content);
                    }
                }
                else
                {
                    DockPanel.SaveFocus();
                    bRestoreFocus = true;
                }
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            Form.Parent = value;

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Workaround of .Net Framework bug:
            // Change the parent of a control with focus may result in the first
            // MDI child form get activated. 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (bRestoreFocus)
                Activate();

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        public void Show()
        {
            if (DockPanel == null)
                Form.Show();
            else
                Show(DockPanel);
        }

        public void Show(DockPanel dockPanel)
        {
            if (dockPanel == null)
                throw new ArgumentNullException(Strings.DockContentHandler_Show_NullDockPanel);

            if (DockState == DockState.Unknown)
                Show(dockPanel, DefaultShowState);
            else if (DockPanel != dockPanel)
                Show(dockPanel, DockState == DockState.Hidden ? _mVisibleState : DockState);
            else
                Activate();
        }

        public void Show(DockPanel dockPanel, DockState dockState)
        {
            if (dockPanel == null)
                throw new ArgumentNullException(Strings.DockContentHandler_Show_NullDockPanel);

            if (dockState == DockState.Unknown || dockState == DockState.Hidden)
                throw new ArgumentException(Strings.DockContentHandler_Show_InvalidDockState);

            dockPanel.SuspendLayout(true);

            DockPanel = dockPanel;

            if (dockState == DockState.Float)
            {
                if (FloatPane == null)
                    Pane = DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.Float, true);
            }
            else if (PanelPane == null)
            {
                DockPane paneExisting = null;
                foreach (DockPane pane in DockPanel.Panes)
                    if (pane.DockState == dockState)
                    {
                        if (paneExisting == null || pane.IsActivated)
                            paneExisting = pane;

                        if (pane.IsActivated)
                            break;
                    }

                Pane = paneExisting ?? DockPanel.DockPaneFactory.CreateDockPane(Content, dockState, true);
            }

            DockState = dockState;
            dockPanel.ResumeLayout(true, true); //we'll resume the layout before activating to ensure that the position
            Activate();                         //and size of the form are finally processed before the form is shown
        }

        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters")]
        public void Show(DockPanel dockPanel, Rectangle floatWindowBounds)
        {
            if (dockPanel == null)
                throw new ArgumentNullException(Strings.DockContentHandler_Show_NullDockPanel);

            dockPanel.SuspendLayout(true);

            DockPanel = dockPanel;
            if (FloatPane == null)
            {
                IsHidden = true;	// to reduce the screen flicker
                FloatPane = DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.Float, false);
                FloatPane.FloatWindow.StartPosition = FormStartPosition.Manual;
            }

            FloatPane.FloatWindow.Bounds = floatWindowBounds;

            Show(dockPanel, DockState.Float);
            Activate();

            dockPanel.ResumeLayout(true, true);
        }

        public void Show(DockPane pane, IDockContent beforeContent)
        {
            if (pane == null)
                throw new ArgumentNullException(Strings.DockContentHandler_Show_NullPane);

            if (beforeContent != null && pane.Contents.IndexOf(beforeContent) == -1)
                throw new ArgumentException(Strings.DockContentHandler_Show_InvalidBeforeContent);

            pane.DockPanel.SuspendLayout(true);

            DockPanel = pane.DockPanel;
            Pane = pane;
            pane.SetContentIndex(Content, pane.Contents.IndexOf(beforeContent));
            Show();

            pane.DockPanel.ResumeLayout(true, true);
        }

        public void Show(DockPane previousPane, DockAlignment alignment, double proportion)
        {
            if (previousPane == null)
                throw new ArgumentException(Strings.DockContentHandler_Show_InvalidPrevPane);

            if (DockHelper.IsDockStateAutoHide(previousPane.DockState))
                throw new ArgumentException(Strings.DockContentHandler_Show_InvalidPrevPane);

            previousPane.DockPanel.SuspendLayout(true);

            DockPanel = previousPane.DockPanel;
            DockPanel.DockPaneFactory.CreateDockPane(Content, previousPane, alignment, proportion, true);
            Show();

            previousPane.DockPanel.ResumeLayout(true, true);
        }

        public void Close()
        {
            var dockPanel = DockPanel;
            dockPanel?.SuspendLayout(true);
            Form.Close();
            dockPanel?.ResumeLayout(true, true);
        }

        private DockPaneStrip.Tab _mTab;
        internal DockPaneStrip.Tab GetTab(DockPaneStrip dockPaneStrip)
        {
            return _mTab ?? (_mTab = dockPaneStrip.CreateTab(Content));
        }

        private IDisposable _mAutoHideTab;
        internal IDisposable AutoHideTab
        {
            get { return _mAutoHideTab; }
            set { _mAutoHideTab = value; }
        }

        #region Events
        private static readonly object DockStateChangedEvent = new object();
        public event EventHandler DockStateChanged
        {
            add { Events.AddHandler(DockStateChangedEvent, value); }
            remove { Events.RemoveHandler(DockStateChangedEvent, value); }
        }
        protected virtual void OnDockStateChanged(EventArgs e)
        {
            EventHandler handler = (EventHandler)Events[DockStateChangedEvent];
            handler?.Invoke(this, e);
        }
        #endregion

        private void Form_Disposed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void Form_TextChanged(object sender, EventArgs e)
        {
            if (DockHelper.IsDockStateAutoHide(DockState))
                DockPanel.RefreshAutoHideStrip();
            else if (Pane != null)
            {
                Pane.FloatWindow?.SetText();
                Pane.RefreshChanges();
            }
        }

        private bool _mFlagClipWindow;
        internal bool FlagClipWindow
        {
            get { return _mFlagClipWindow; }
            set
            {
                if (_mFlagClipWindow == value)
                    return;

                _mFlagClipWindow = value;
                Form.Region = _mFlagClipWindow ? new Region(Rectangle.Empty) : null;
            }
        }

        public ContextMenuStrip TabPageContextMenuStrip { get; set; }

        #region IDockDragSource Members

        Control IDragSource.DragControl => Form;

        bool IDockDragSource.CanDockTo(DockPane pane)
        {
            if (!IsDockStateValid(pane.DockState))
                return false;

            if (Pane == pane && pane.DisplayingContents.Count == 1)
                return false;

            return true;
        }

        Rectangle IDockDragSource.BeginDrag(Point ptMouse)
        {
            Size size;
            DockPane floatPane = FloatPane;
            if (DockState == DockState.Float || floatPane == null || floatPane.FloatWindow.NestedPanes.Count != 1)
                size = DockPanel.DefaultFloatWindowSize;
            else
                size = floatPane.FloatWindow.Size;

            Point location;
            var rectPane = Pane.ClientRectangle;
            if (DockState == DockState.Document)
            {
                location = Pane.DockPanel.DocumentTabStripLocation == DocumentTabStripLocation.Bottom ? new Point(rectPane.Left, rectPane.Bottom - size.Height) : new Point(rectPane.Left, rectPane.Top);
            }
            else
            {
                location = new Point(rectPane.Left, rectPane.Bottom);
                location.Y -= size.Height;
            }
            location = Pane.PointToScreen(location);

            if (ptMouse.X > location.X + size.Width)
                location.X += ptMouse.X - (location.X + size.Width) + Measures.SplitterSize;

            return new Rectangle(location, size);
        }

        void IDockDragSource.EndDrag()
        {
        }

        public void FloatAt(Rectangle floatWindowBounds)
        {
            // TODO: where is the pane used?
            DockPanel.DockPaneFactory.CreateDockPane(Content, floatWindowBounds, true);
        }

        public void DockTo(DockPane pane, DockStyle dockStyle, int contentIndex)
        {
            if (dockStyle == DockStyle.Fill)
            {
                bool samePane = Pane == pane;
                if (!samePane)
                    Pane = pane;

                int visiblePanes = 0;
                int convertedIndex = 0;
                while (visiblePanes <= contentIndex && convertedIndex < Pane.Contents.Count)
                {
                    DockContent window = Pane.Contents[convertedIndex] as DockContent;
                    if (window != null && !window.IsHidden)
                        ++visiblePanes;

                    ++convertedIndex;
                }

                contentIndex = Math.Min(Math.Max(0, convertedIndex - 1), Pane.Contents.Count - 1);

                if (contentIndex == -1 || !samePane)
                    pane.SetContentIndex(Content, contentIndex);
                else
                {
                    DockContentCollection contents = pane.Contents;
                    int oldIndex = contents.IndexOf(Content);
                    int newIndex = contentIndex;
                    if (oldIndex < newIndex)
                    {
                        newIndex += 1;
                        if (newIndex > contents.Count - 1)
                            newIndex = -1;
                    }
                    pane.SetContentIndex(Content, newIndex);
                }
            }
            else
            {
                DockPane paneFrom = DockPanel.DockPaneFactory.CreateDockPane(Content, pane.DockState, true);
                INestedPanesContainer container = pane.NestedPanesContainer;
                if (dockStyle == DockStyle.Left)
                    paneFrom.DockTo(container, pane, DockAlignment.Left, 0.5);
                else if (dockStyle == DockStyle.Right)
                    paneFrom.DockTo(container, pane, DockAlignment.Right, 0.5);
                else if (dockStyle == DockStyle.Top)
                    paneFrom.DockTo(container, pane, DockAlignment.Top, 0.5);
                else if (dockStyle == DockStyle.Bottom)
                    paneFrom.DockTo(container, pane, DockAlignment.Bottom, 0.5);

                paneFrom.DockState = pane.DockState;
            }
        }

        public void DockTo(DockPanel panel, DockStyle dockStyle)
        {
            if (panel != DockPanel)
                throw new ArgumentException(Strings.IDockDragSource_DockTo_InvalidPanel, nameof(panel));

            if (dockStyle == DockStyle.Top)
                DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.DockTop, true);
            else if (dockStyle == DockStyle.Bottom)
                DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.DockBottom, true);
            else if (dockStyle == DockStyle.Left)
                DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.DockLeft, true);
            else if (dockStyle == DockStyle.Right)
                DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.DockRight, true);
            else switch (dockStyle)
                {
                    case DockStyle.Fill:
                        DockPanel.DockPaneFactory.CreateDockPane(Content, DockState.Document, true);
                        break;
                    default:
                        return;
                }
        }

        #endregion
    }
}
