using System.ComponentModel;
using System.Drawing;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Windows.Forms
{
    [ToolboxItem(false)]
    public partial class DockPane : UserControl, IDockDragSource
    {

        public enum AppearanceStyle
        {
            ToolWindow,
            Document
        }

        private enum HitTestArea
        {
            Caption,
            TabStrip,
            Content,
            None
        }

        private struct HitTestResult
        {
            public readonly HitTestArea HitArea;
            public readonly int Index;

            public HitTestResult(HitTestArea hitTestArea, int index)
            {
                HitArea = hitTestArea;
                Index = index;
            }
        }

        private DockPaneCaption CaptionControl { get; set; }

        public DockPaneStrip TabStripControl { get; private set; }

        protected internal DockPane(IDockContent content, DockState visibleState, bool show)
        {
            InternalConstruct(content, visibleState, false, Rectangle.Empty, null, DockAlignment.Right, 0.5, show);
        }

        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
        protected internal DockPane(IDockContent content, FloatWindow floatWindow, bool show)
        {
            if (floatWindow == null)
                throw new ArgumentNullException(nameof(floatWindow));

            InternalConstruct(content, DockState.Float, false, Rectangle.Empty, floatWindow.NestedPanes.GetDefaultPreviousPane(this), DockAlignment.Right, 0.5, show);
        }

        protected internal DockPane(IDockContent content, DockPane previousPane, DockAlignment alignment, double proportion, bool show)
        {
            if (previousPane == null)
                throw new ArgumentNullException(nameof(previousPane));
            InternalConstruct(content, previousPane.DockState, false, Rectangle.Empty, previousPane, alignment, proportion, show);
        }

        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
        protected internal DockPane(IDockContent content, Rectangle floatWindowBounds, bool show)
        {
            InternalConstruct(content, DockState.Float, true, floatWindowBounds, null, DockAlignment.Right, 0.5, show);
        }

        private void InternalConstruct(IDockContent content, DockState dockState, bool flagBounds, Rectangle floatWindowBounds, DockPane prevPane, DockAlignment alignment, double proportion, bool show)
        {
            if (dockState == DockState.Hidden || dockState == DockState.Unknown)
                throw new ArgumentException(Strings.DockPane_SetDockState_InvalidState);

            if (content == null)
                throw new ArgumentNullException(Strings.DockPane_Constructor_NullContent);

            if (content.DockHandler.DockPanel == null)
                throw new ArgumentException(Strings.DockPane_Constructor_NullDockPanel);


            SuspendLayout();
            SetStyle(ControlStyles.Selectable, false);

            IsFloat = dockState == DockState.Float;

            Contents = new DockContentCollection();
            DisplayingContents = new DockContentCollection(this);
            DockPanel = content.DockHandler.DockPanel;
            DockPanel.AddPane(this);

            Splitter = content.DockHandler.DockPanel.Extender.DockPaneSplitterControlFactory.CreateSplitterControl(this);

            NestedDockingStatus = new NestedDockingStatus(this);

            CaptionControl = DockPanel.DockPaneCaptionFactory.CreateDockPaneCaption(this);
            TabStripControl = DockPanel.DockPaneStripFactory.CreateDockPaneStrip(this);
            Controls.AddRange(new Control[] { CaptionControl, TabStripControl });

            DockPanel.SuspendLayout(true);
            if (flagBounds)
                FloatWindow = DockPanel.FloatWindowFactory.CreateFloatWindow(DockPanel, this, floatWindowBounds);
            else if (prevPane != null)
                DockTo(prevPane.NestedPanesContainer, prevPane, alignment, proportion);

            SetDockState(dockState);
            if (show)
                content.DockHandler.Pane = this;
            else if (IsFloat)
                content.DockHandler.FloatPane = this;
            else
                content.DockHandler.PanelPane = this;

            ResumeLayout();
            DockPanel.ResumeLayout(true, true);
        }

        private bool _mIsDisposing;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // IMPORTANT: avoid nested call into this method on Mono. 
                // https://github.com/dockpanelsuite/dockpanelsuite/issues/16
                if (Win32Helper.IsRunningOnMono)
                {
                    if (_mIsDisposing)
                        return;

                    _mIsDisposing = true;
                }

                _mDockState = DockState.Unknown;

                NestedPanesContainer?.NestedPanes.Remove(this);

                if (DockPanel != null)
                {
                    DockPanel.RemovePane(this);
                    DockPanel = null;
                }

                Splitter.Dispose();
                AutoHidePane?.Dispose();
            }
            base.Dispose(disposing);
        }

        private IDockContent _mActiveContent;
        public virtual IDockContent ActiveContent
        {
            get { return _mActiveContent; }
            set
            {
                if (ActiveContent == value)
                    return;

                if (value != null)
                {
                    if (!DisplayingContents.Contains(value))
                        throw new InvalidOperationException(Strings.DockPane_ActiveContent_InvalidValue);
                }
                else
                {
                    if (DisplayingContents.Count != 0)
                        throw new InvalidOperationException(Strings.DockPane_ActiveContent_InvalidValue);
                }

                IDockContent oldValue = _mActiveContent;

                if (DockPanel.ActiveAutoHideContent == oldValue)
                    DockPanel.ActiveAutoHideContent = null;

                _mActiveContent = value;

                _mActiveContent?.DockHandler.SetVisible();
                if (oldValue != null && DisplayingContents.Contains(oldValue))
                    oldValue.DockHandler.SetVisible();
                if (IsActivated)
                    _mActiveContent?.DockHandler.Activate();

                FloatWindow?.SetText();
                RefreshChanges();

                if (_mActiveContent != null)
                    TabStripControl.EnsureTabVisible(_mActiveContent);
            }
        }

        public virtual bool AllowDockDragAndDrop { get; set; } = true;

        internal IDisposable AutoHidePane { get; set; }

        internal object AutoHideTabs { get; set; }

        private object TabPageContextMenu
        {
            get
            {
                var content = ActiveContent;

                if (content == null)
                    return null;

                if (content.DockHandler.TabPageContextMenuStrip != null)
                    return content.DockHandler.TabPageContextMenuStrip;
                return content.DockHandler.TabPageContextMenu;
            }
        }

        internal bool HasTabPageContextMenu => TabPageContextMenu != null;

        internal void ShowTabPageContextMenu(Control control, Point position)
        {
            var menu = TabPageContextMenu;

            if (menu == null)
                return;

            var contextMenuStrip = menu as ContextMenuStrip;
            if (contextMenuStrip != null)
            {
                contextMenuStrip.Show(control, position);
                return;
            }

            var contextMenu = menu as ContextMenu;
            contextMenu?.Show(this, position);
        }

        private Rectangle CaptionRectangle
        {
            get
            {
                if (!HasCaption)
                    return Rectangle.Empty;

                var rectWindow = DisplayingRectangle;
                var x = rectWindow.X;
                var y = rectWindow.Y;
                var width = rectWindow.Width;
                var height = CaptionControl.MeasureHeight();

                return new Rectangle(x, y, width, height);
            }
        }

        internal Rectangle ContentRectangle
        {
            get
            {
                var rectWindow = DisplayingRectangle;
                var rectCaption = CaptionRectangle;
                var rectTabStrip = TabStripRectangle;

                var x = rectWindow.X;

                var y = rectWindow.Y + (rectCaption.IsEmpty ? 0 : rectCaption.Height);
                if (DockState == DockState.Document && DockPanel.DocumentTabStripLocation == DocumentTabStripLocation.Top)
                    y += rectTabStrip.Height;

                var width = rectWindow.Width;
                var height = rectWindow.Height - rectCaption.Height - rectTabStrip.Height;

                return new Rectangle(x, y, width, height);
            }
        }

        internal Rectangle TabStripRectangle => Appearance == AppearanceStyle.ToolWindow ? TabStripRectangleToolWindow : TabStripRectangleDocument;

        private Rectangle TabStripRectangleToolWindow
        {
            get
            {
                if (DisplayingContents.Count <= 1 || IsAutoHide)
                    return Rectangle.Empty;

                var rectWindow = DisplayingRectangle;

                var width = rectWindow.Width;
                var height = TabStripControl.MeasureHeight();
                var x = rectWindow.X;
                var y = rectWindow.Bottom - height;
                var rectCaption = CaptionRectangle;
                if (rectCaption.Contains(x, y))
                    y = rectCaption.Y + rectCaption.Height;

                return new Rectangle(x, y, width, height);
            }
        }

        private Rectangle TabStripRectangleDocument
        {
            get
            {
                if (DisplayingContents.Count == 0)
                    return Rectangle.Empty;
                
                var rectWindow = DisplayingRectangle;
                var x = rectWindow.X;
                var width = rectWindow.Width;
                var height = TabStripControl.MeasureHeight();

                int y;
                if (DockPanel.DocumentTabStripLocation == DocumentTabStripLocation.Bottom)
                    y = rectWindow.Height - height;
                else
                    y = rectWindow.Y;

                return new Rectangle(x, y, width, height);
            }
        }

        public virtual string CaptionText => ActiveContent == null ? string.Empty : ActiveContent.DockHandler.TabText;

        public DockContentCollection Contents { get; private set; }

        public DockContentCollection DisplayingContents { get; private set; }

        public DockPanel DockPanel { get; private set; }

        private bool HasCaption => DockState != DockState.Document && DockState != DockState.Hidden && DockState != DockState.Unknown && (DockState != DockState.Float || FloatWindow.VisibleNestedPanes.Count > 1);

        public bool IsActivated { get; private set; }

        internal void SetIsActivated(bool value)
        {
            if (IsActivated == value)
                return;

            IsActivated = value;
            if (DockState != DockState.Document)
                RefreshChanges(false);
            OnIsActivatedChanged(EventArgs.Empty);
        }

        public bool IsActiveDocumentPane { get; private set; }

        internal void SetIsActiveDocumentPane(bool value)
        {
            if (IsActiveDocumentPane == value)
                return;

            IsActiveDocumentPane = value;
            if (DockState == DockState.Document)
                RefreshChanges();
            OnIsActiveDocumentPaneChanged(EventArgs.Empty);
        }

        public bool IsDockStateValid(DockState dockState)
        {
            return Contents.All(content => content.DockHandler.IsDockStateValid(dockState));
        }

        public bool IsAutoHide => DockHelper.IsDockStateAutoHide(DockState);

        public AppearanceStyle Appearance => DockState == DockState.Document ? AppearanceStyle.Document : AppearanceStyle.ToolWindow;

        public Rectangle DisplayingRectangle => ClientRectangle;

        public void Activate()
        {
            if (DockHelper.IsDockStateAutoHide(DockState) && DockPanel.ActiveAutoHideContent != ActiveContent)
                DockPanel.ActiveAutoHideContent = ActiveContent;
            else if (!IsActivated)
                ActiveContent?.DockHandler.Activate();
        }

        internal void AddContent(IDockContent content)
        {
            if (Contents.Contains(content))
                return;

            Contents.Add(content);
        }

        internal void Close()
        {
            Dispose();
        }

        public void CloseActiveContent()
        {
            CloseContent(ActiveContent);
        }

        internal void CloseContent(IDockContent content)
        {
            if (content == null)
                return;

            if (!content.DockHandler.CloseButton)
                return;

            var dockPanel = DockPanel;

            dockPanel.SuspendLayout(true);

            try
            {
                if (content.DockHandler.HideOnClose)
                {
                    content.DockHandler.Hide();
                    NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(this);
                }
                else
                    content.DockHandler.Close();
            }
            finally
            {
                dockPanel.ResumeLayout(true, true);
            }
        }

        private HitTestResult GetHitTest(Point ptMouse)
        {
            var ptMouseClient = PointToClient(ptMouse);

            var rectCaption = CaptionRectangle;
            if (rectCaption.Contains(ptMouseClient))
                return new HitTestResult(HitTestArea.Caption, -1);

            var rectContent = ContentRectangle;
            if (rectContent.Contains(ptMouseClient))
                return new HitTestResult(HitTestArea.Content, -1);

            var rectTabStrip = TabStripRectangle;
            return rectTabStrip.Contains(ptMouseClient) ? new HitTestResult(HitTestArea.TabStrip, TabStripControl.HitTest(TabStripControl.PointToClient(ptMouse))) : new HitTestResult(HitTestArea.None, -1);
        }

        public bool IsHidden { get; private set; } = true;

        private void SetIsHidden(bool value)
        {
            if (IsHidden == value)
                return;

            IsHidden = value;
            if (DockHelper.IsDockStateAutoHide(DockState))
            {
                DockPanel.RefreshAutoHideStrip();
                DockPanel.PerformLayout();
            }
            else
            {
                ((Control) NestedPanesContainer)?.PerformLayout();
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            SetIsHidden(DisplayingContents.Count == 0);
            if (!IsHidden)
            {
                CaptionControl.Bounds = CaptionRectangle;
                TabStripControl.Bounds = TabStripRectangle;

                SetContentBounds();

                foreach (var content in Contents.Where(content => DisplayingContents.Contains(content)).Where(content => content.DockHandler.FlagClipWindow && content.DockHandler.Form.Visible))
                {
                    content.DockHandler.FlagClipWindow = false;
                }
            }

            base.OnLayout(e);
        }

        internal void SetContentBounds()
        {
            var rectContent = ContentRectangle;
            var rectInactive = new Rectangle(-rectContent.Width, rectContent.Y, rectContent.Width, rectContent.Height);
            foreach (var content in Contents)
                if (content.DockHandler.Pane == this)
                {
                    content.DockHandler.Form.Bounds = content == ActiveContent ? rectContent : rectInactive;
                }
        }

        internal void RefreshChanges()
        {
            RefreshChanges(true);
        }

        private void RefreshChanges(bool performLayout)
        {
            if (IsDisposed)
                return;

            CaptionControl.RefreshChanges();
            TabStripControl.RefreshChanges();
            if (DockState == DockState.Float)
                FloatWindow?.RefreshChanges();
            if (DockHelper.IsDockStateAutoHide(DockState) && DockPanel != null)
            {
                DockPanel.RefreshAutoHideStrip();
                DockPanel.PerformLayout();
            }

            if (performLayout)
                PerformLayout();
        }

        internal void RemoveContent(IDockContent content)
        {
            if (!Contents.Contains(content))
                return;

            Contents.Remove(content);
        }

        public void SetContentIndex(IDockContent content, int index)
        {
            var oldIndex = Contents.IndexOf(content);
            if (oldIndex == -1)
                throw new ArgumentException(Strings.DockPane_SetContentIndex_InvalidContent);

            if (index < 0 || index > Contents.Count - 1)
                if (index != -1)
                    throw new ArgumentOutOfRangeException(Strings.DockPane_SetContentIndex_InvalidIndex);

            if (oldIndex == index)
                return;
            if (oldIndex == Contents.Count - 1 && index == -1)
                return;

            Contents.Remove(content);
            if (index == -1)
                Contents.Add(content);
            else if (oldIndex < index)
                Contents.AddAt(content, index - 1);
            else
                Contents.AddAt(content, index);

            RefreshChanges();
        }

        private void SetParent()
        {
            switch (DockState)
            {
                case DockState.Unknown:
                case DockState.Hidden:
                    SetParent(null);
                    Splitter.Parent = null;
                    break;
                case DockState.Float:
                    SetParent(FloatWindow);
                    Splitter.Parent = FloatWindow;
                    break;
                default:
                    if (DockHelper.IsDockStateAutoHide(DockState))
                    {
                        SetParent(DockPanel.AutoHideControl);
                        Splitter.Parent = null;
                    }
                    else
                    {
                        SetParent(DockPanel.DockWindows[DockState]);
                        Splitter.Parent = Parent;
                    }
                    break;
            }
        }

        private void SetParent(Control value)
        {
            if (Parent == value)
                return;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Workaround of .Net Framework bug:
            // Change the parent of a control with focus may result in the first
            // MDI child form get activated. 
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            var contentFocused = GetFocusedContent();
            if (contentFocused != null)
                DockPanel.SaveFocus();

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            Parent = value;

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Workaround of .Net Framework bug:
            // Change the parent of a control with focus may result in the first
            // MDI child form get activated. 
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            contentFocused?.DockHandler.Activate();
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }

        public new void Show()
        {
            Activate();
        }

        internal void TestDrop(IDockDragSource dragSource, DockOutlineBase dockOutline)
        {
            if (!dragSource.CanDockTo(this))
                return;

            var ptMouse = MousePosition;

            var hitTestResult = GetHitTest(ptMouse);
            if (hitTestResult.HitArea == HitTestArea.Caption)
                dockOutline.Show(this, -1);
            else if (hitTestResult.HitArea == HitTestArea.TabStrip && hitTestResult.Index != -1)
                dockOutline.Show(this, hitTestResult.Index);
        }

        internal void ValidateActiveContent()
        {
            if (ActiveContent == null)
            {
                if (DisplayingContents.Count != 0)
                    ActiveContent = DisplayingContents[0];
                return;
            }

            if (DisplayingContents.IndexOf(ActiveContent) >= 0)
                return;

            IDockContent prevVisible = null;
            for (var i = Contents.IndexOf(ActiveContent) - 1; i >= 0; i--)
                if (Contents[i].DockHandler.DockState == DockState)
                {
                    prevVisible = Contents[i];
                    break;
                }

            IDockContent nextVisible = null;
            for (var i = Contents.IndexOf(ActiveContent) + 1; i < Contents.Count; i++)
                if (Contents[i].DockHandler.DockState == DockState)
                {
                    nextVisible = Contents[i];
                    break;
                }

            if (prevVisible != null)
                ActiveContent = prevVisible;
            else if (nextVisible != null)
                ActiveContent = nextVisible;
            else
                ActiveContent = null;
        }

        private static readonly object DockStateChangedEvent = new object();
        public event EventHandler DockStateChanged
        {
            add { Events.AddHandler(DockStateChangedEvent, value); }
            remove { Events.RemoveHandler(DockStateChangedEvent, value); }
        }
        protected virtual void OnDockStateChanged(EventArgs e)
        {
            var handler = (EventHandler)Events[DockStateChangedEvent];
            handler?.Invoke(this, e);
        }

        private static readonly object IsActivatedChangedEvent = new object();
        public event EventHandler IsActivatedChanged
        {
            add { Events.AddHandler(IsActivatedChangedEvent, value); }
            remove { Events.RemoveHandler(IsActivatedChangedEvent, value); }
        }
        protected virtual void OnIsActivatedChanged(EventArgs e)
        {
            EventHandler handler = (EventHandler)Events[IsActivatedChangedEvent];
            handler?.Invoke(this, e);
        }

        private static readonly object IsActiveDocumentPaneChangedEvent = new object();
        public event EventHandler IsActiveDocumentPaneChanged
        {
            add { Events.AddHandler(IsActiveDocumentPaneChangedEvent, value); }
            remove { Events.RemoveHandler(IsActiveDocumentPaneChangedEvent, value); }
        }
        protected virtual void OnIsActiveDocumentPaneChanged(EventArgs e)
        {
            var handler = (EventHandler)Events[IsActiveDocumentPaneChangedEvent];
            handler?.Invoke(this, e);
        }

        public DockWindow DockWindow
        {
            get { return NestedDockingStatus.NestedPanes?.Container as DockWindow; }
            set
            {
                var oldValue = DockWindow;
                if (oldValue == value)
                    return;

                DockTo(value);
            }
        }

        public FloatWindow FloatWindow
        {
            get { return NestedDockingStatus.NestedPanes?.Container as FloatWindow; }
            set
            {
                var oldValue = FloatWindow;
                if (oldValue == value)
                    return;

                DockTo(value);
            }
        }

        public NestedDockingStatus NestedDockingStatus { get; private set; }

        public bool IsFloat { get; private set; }

        public INestedPanesContainer NestedPanesContainer => NestedDockingStatus.NestedPanes?.Container;

        private DockState _mDockState = DockState.Unknown;
        public DockState DockState
        {
            get { return _mDockState; }
            set
            {
                SetDockState(value);
            }
        }

        public DockPane SetDockState(DockState value)
        {
            if (value == DockState.Unknown || value == DockState.Hidden)
                throw new InvalidOperationException(Strings.DockPane_SetDockState_InvalidState);

            if (value == DockState.Float == IsFloat)
            {
                InternalSetDockState(value);
                return this;
            }

            if (DisplayingContents.Count == 0)
                return null;

            var firstContent = DisplayingContents.FirstOrDefault(content => content.DockHandler.IsDockStateValid(value));
            if (firstContent == null)
                return null;

            firstContent.DockHandler.DockState = value;
            var pane = firstContent.DockHandler.Pane;
            DockPanel.SuspendLayout(true);
            for (var i = 0; i < DisplayingContents.Count; i++)
            {
                var content = DisplayingContents[i];
                if (content.DockHandler.IsDockStateValid(value))
                    content.DockHandler.Pane = pane;
            }
            DockPanel.ResumeLayout(true, true);
            return pane;
        }

        private void InternalSetDockState(DockState value)
        {
            if (_mDockState == value)
                return;

            var oldDockState = _mDockState;
            var oldContainer = NestedPanesContainer;

            _mDockState = value;

            SuspendRefreshStateChange();

            var contentFocused = GetFocusedContent();
            if (contentFocused != null)
                DockPanel.SaveFocus();

            if (!IsFloat)
                DockWindow = DockPanel.DockWindows[DockState];
            else if (FloatWindow == null)
                FloatWindow = DockPanel.FloatWindowFactory.CreateFloatWindow(DockPanel, this);

            if (contentFocused != null)
            {
                if (!Win32Helper.IsRunningOnMono)
                {
                    DockPanel.ContentFocusManager.Activate(contentFocused);
                }
            }

            ResumeRefreshStateChange(oldContainer, oldDockState);
        }

        private int _mCountRefreshStateChange;
        private void SuspendRefreshStateChange()
        {
            _mCountRefreshStateChange++;
            DockPanel.SuspendLayout(true);
        }

        private void ResumeRefreshStateChange()
        {
            _mCountRefreshStateChange--;
            Diagnostics.Debug.Assert(_mCountRefreshStateChange >= 0);
            DockPanel.ResumeLayout(true, true);
        }

        private bool IsRefreshStateChangeSuspended => _mCountRefreshStateChange != 0;

        private void ResumeRefreshStateChange(INestedPanesContainer oldContainer, DockState oldDockState)
        {
            ResumeRefreshStateChange();
            RefreshStateChange(oldContainer, oldDockState);
        }

        private void RefreshStateChange(INestedPanesContainer oldContainer, DockState oldDockState)
        {
            if (IsRefreshStateChangeSuspended)
                return;

            SuspendRefreshStateChange();

            DockPanel.SuspendLayout(true);

            var contentFocused = GetFocusedContent();
            if (contentFocused != null)
                DockPanel.SaveFocus();
            SetParent();

            ActiveContent?.DockHandler.SetDockState(ActiveContent.DockHandler.IsHidden, DockState, ActiveContent.DockHandler.Pane);
            foreach (var content in Contents.Where(content => content.DockHandler.Pane == this))
            {
                content.DockHandler.SetDockState(content.DockHandler.IsHidden, DockState, content.DockHandler.Pane);
            }

            var oldContainerControl = (Control) oldContainer;
            if (oldContainer?.DockState == oldDockState && !oldContainerControl.IsDisposed)
                oldContainerControl.PerformLayout();
            if (DockHelper.IsDockStateAutoHide(oldDockState))
                DockPanel.RefreshActiveAutoHideContent();

            if (NestedPanesContainer.DockState == DockState)
                ((Control)NestedPanesContainer).PerformLayout();
            if (DockHelper.IsDockStateAutoHide(DockState))
                DockPanel.RefreshActiveAutoHideContent();

            if (DockHelper.IsDockStateAutoHide(oldDockState) ||
                DockHelper.IsDockStateAutoHide(DockState))
            {
                DockPanel.RefreshAutoHideStrip();
                DockPanel.PerformLayout();
            }

            ResumeRefreshStateChange();

            contentFocused?.DockHandler.Activate();

            DockPanel.ResumeLayout(true, true);

            if (oldDockState != DockState)
                OnDockStateChanged(EventArgs.Empty);
        }

        private IDockContent GetFocusedContent()
        {
            return Contents.FirstOrDefault(content => content.DockHandler.Form.ContainsFocus);
        }

        public DockPane DockTo(INestedPanesContainer container)
        {
            if (container == null)
                throw new InvalidOperationException(Strings.DockPane_DockTo_NullContainer);

            DockAlignment alignment;
            if (container.DockState == DockState.DockLeft || container.DockState == DockState.DockRight)
                alignment = DockAlignment.Bottom;
            else
                alignment = DockAlignment.Right;

            return DockTo(container, container.NestedPanes.GetDefaultPreviousPane(this), alignment, 0.5);
        }

        public DockPane DockTo(INestedPanesContainer container, DockPane previousPane, DockAlignment alignment, double proportion)
        {
            if (container == null)
                throw new InvalidOperationException(Strings.DockPane_DockTo_NullContainer);

            if (container.IsFloat == IsFloat)
            {
                InternalAddToDockList(container, previousPane, alignment, proportion);
                return this;
            }

            var firstContent = GetFirstContent(container.DockState);
            if (firstContent == null)
                return null;

            DockPanel.DummyContent.DockPanel = DockPanel;
            var pane = container.IsFloat ? DockPanel.DockPaneFactory.CreateDockPane(DockPanel.DummyContent, (FloatWindow)container, true) : DockPanel.DockPaneFactory.CreateDockPane(DockPanel.DummyContent, container.DockState, true);

            pane.DockTo(container, previousPane, alignment, proportion);
            SetVisibleContentsToPane(pane);
            DockPanel.DummyContent.DockPanel = null;

            return pane;
        }

        private void SetVisibleContentsToPane(DockPane pane)
        {
            SetVisibleContentsToPane(pane, ActiveContent);
        }

        private void SetVisibleContentsToPane(DockPane pane, IDockContent activeContent)
        {
            for (var i = 0; i < DisplayingContents.Count; i++)
            {
                var content = DisplayingContents[i];
                if (!content.DockHandler.IsDockStateValid(pane.DockState)) continue;
                content.DockHandler.Pane = pane;
                i--;
            }

            if (activeContent.DockHandler.Pane == pane)
                pane.ActiveContent = activeContent;
        }

        private void InternalAddToDockList(INestedPanesContainer container, DockPane prevPane, DockAlignment alignment, double proportion)
        {
            if (container.DockState == DockState.Float != IsFloat)
                throw new InvalidOperationException(Strings.DockPane_DockTo_InvalidContainer);

            var count = container.NestedPanes.Count;
            if (container.NestedPanes.Contains(this))
                count--;
            if (prevPane == null && count > 0)
                throw new InvalidOperationException(Strings.DockPane_DockTo_NullPrevPane);

            if (prevPane != null && !container.NestedPanes.Contains(prevPane))
                throw new InvalidOperationException(Strings.DockPane_DockTo_NoPrevPane);

            if (prevPane == this)
                throw new InvalidOperationException(Strings.DockPane_DockTo_SelfPrevPane);

            var oldContainer = NestedPanesContainer;
            var oldDockState = DockState;
            container.NestedPanes.Add(this);
            NestedDockingStatus.SetStatus(container.NestedPanes, prevPane, alignment, proportion);

            if (DockHelper.IsDockWindowState(DockState))
                _mDockState = container.DockState;

            RefreshStateChange(oldContainer, oldDockState);
        }

        public void SetNestedDockingProportion(double proportion)
        {
            NestedDockingStatus.SetStatus(NestedDockingStatus.NestedPanes, NestedDockingStatus.PreviousPane, NestedDockingStatus.Alignment, proportion);
            ((Control) NestedPanesContainer)?.PerformLayout();
        }

        public DockPane Float()
        {
            DockPanel.SuspendLayout(true);

            var activeContent = ActiveContent;

            var floatPane = GetFloatPaneFromContents();
            if (floatPane == null)
            {
                var firstContent = GetFirstContent(DockState.Float);
                if (firstContent == null)
                {
                    DockPanel.ResumeLayout(true, true);
                    return null;
                }
                floatPane = DockPanel.DockPaneFactory.CreateDockPane(firstContent, DockState.Float, true);
            }
            SetVisibleContentsToPane(floatPane, activeContent);

            DockPanel.ResumeLayout(true, true);
            return floatPane;
        }

        private DockPane GetFloatPaneFromContents()
        {
            DockPane floatPane = null;
            for (var i = 0; i < DisplayingContents.Count; i++)
            {
                var content = DisplayingContents[i];
                if (!content.DockHandler.IsDockStateValid(DockState.Float))
                    continue;

                if (floatPane != null && content.DockHandler.FloatPane != floatPane)
                    return null;
                floatPane = content.DockHandler.FloatPane;
            }

            return floatPane;
        }

        private IDockContent GetFirstContent(DockState dockState)
        {
            for (int i = 0; i < DisplayingContents.Count; i++)
            {
                IDockContent content = DisplayingContents[i];
                if (content.DockHandler.IsDockStateValid(dockState))
                    return content;
            }
            return null;
        }

        public void RestoreToPanel()
        {
            DockPanel.SuspendLayout(true);

            for (var i = DisplayingContents.Count - 1; i >= 0; i--)
            {
                var content = DisplayingContents[i];
                if (content.DockHandler.CheckDockState(false) != DockState.Unknown)
                    content.DockHandler.IsFloat = false;
            }

            DockPanel.ResumeLayout(true, true);
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (int)Win32.Msgs.WM_MOUSEACTIVATE)
                Activate();

            base.WndProc(ref m);
        }

        #region IDockDragSource Members

        #region IDragSource Members

        Control IDragSource.DragControl => this;

        public IDockContent MouseOverTab { get; set; }

        #endregion

        bool IDockDragSource.IsDockStateValid(DockState dockState)
        {
            return IsDockStateValid(dockState);
        }

        bool IDockDragSource.CanDockTo(DockPane pane)
        {
            if (!IsDockStateValid(pane.DockState))
                return false;

            if (pane == this)
                return false;

            return true;
        }

        Rectangle IDockDragSource.BeginDrag(Point ptMouse)
        {
            Point location = PointToScreen(new Point(0, 0));
            Size size;

            DockPane floatPane = ActiveContent.DockHandler.FloatPane;
            if (DockState == DockState.Float || floatPane == null || floatPane.FloatWindow.NestedPanes.Count != 1)
                size = DockPanel.DefaultFloatWindowSize;
            else
                size = floatPane.FloatWindow.Size;

            if (ptMouse.X > location.X + size.Width)
                location.X += ptMouse.X - (location.X + size.Width) + Measures.SplitterSize;

            return new Rectangle(location, size);
        }

        void IDockDragSource.EndDrag()
        {
        }

        public void FloatAt(Rectangle floatWindowBounds)
        {
            if (FloatWindow == null || FloatWindow.NestedPanes.Count != 1)
                FloatWindow = DockPanel.FloatWindowFactory.CreateFloatWindow(DockPanel, this, floatWindowBounds);
            else
                FloatWindow.Bounds = floatWindowBounds;

            DockState = DockState.Float;

            NestedDockingStatus.NestedPanes.SwitchPaneWithFirstChild(this);
        }

        public void DockTo(DockPane pane, DockStyle dockStyle, int contentIndex)
        {
            if (dockStyle == DockStyle.Fill)
            {
                IDockContent activeContent = ActiveContent;
                for (int i = Contents.Count - 1; i >= 0; i--)
                {
                    IDockContent c = Contents[i];
                    if (c.DockHandler.DockState == DockState)
                    {
                        c.DockHandler.Pane = pane;
                        if (contentIndex != -1)
                            pane.SetContentIndex(c, contentIndex);
                    }
                }
                pane.ActiveContent = activeContent;
            }
            else
            {
                if (dockStyle == DockStyle.Left)
                    DockTo(pane.NestedPanesContainer, pane, DockAlignment.Left, 0.5);
                else if (dockStyle == DockStyle.Right)
                    DockTo(pane.NestedPanesContainer, pane, DockAlignment.Right, 0.5);
                else if (dockStyle == DockStyle.Top)
                    DockTo(pane.NestedPanesContainer, pane, DockAlignment.Top, 0.5);
                else if (dockStyle == DockStyle.Bottom)
                    DockTo(pane.NestedPanesContainer, pane, DockAlignment.Bottom, 0.5);

                DockState = pane.DockState;
            }
        }

        public void DockTo(DockPanel panel, DockStyle dockStyle)
        {
            if (panel != DockPanel)
                throw new ArgumentException(Strings.IDockDragSource_DockTo_InvalidPanel, nameof(panel));

            if (dockStyle == DockStyle.Top)
                DockState = DockState.DockTop;
            else if (dockStyle == DockStyle.Bottom)
                DockState = DockState.DockBottom;
            else if (dockStyle == DockStyle.Left)
                DockState = DockState.DockLeft;
            else if (dockStyle == DockStyle.Right)
                DockState = DockState.DockRight;
            else if (dockStyle == DockStyle.Fill)
                DockState = DockState.Document;
        }

        #endregion

        #region cachedLayoutArgs leak workaround

        /// <summary>
        /// There's a bug in the WinForms layout engine
        /// that can result in a deferred layout to not
        /// properly clear out the cached layout args after
        /// the layout operation is performed.
        /// Specifically, this bug is hit when the bounds of
        /// the Pane change, initiating a layout on the parent
        /// (DockWindow) which is where the bug hits.
        /// To work around it, when a pane loses the DockWindow
        /// as its parent, that parent DockWindow needs to
        /// perform a layout to flush the cached args, if they exist.
        /// </summary>
        private DockWindow _lastParentWindow;
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            var newParent = Parent as DockWindow;
            if (newParent != _lastParentWindow)
            {
                _lastParentWindow?.PerformLayout();
                _lastParentWindow = newParent;
            }
        }
        #endregion
    }
}
