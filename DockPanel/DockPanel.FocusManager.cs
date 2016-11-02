using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Forms
{
    internal interface IContentFocusManager
    {
        void Activate(IDockContent content);
        void GiveUpFocus(IDockContent content);
        void AddToList(IDockContent content);
        void RemoveFromList(IDockContent content);
    }

    partial class DockPanel
    {
        private interface IFocusManager
        {
            void SuspendFocusTracking();
            void ResumeFocusTracking();
            bool IsFocusTrackingSuspended { get; }
            IDockContent ActiveContent { get; }
            DockPane ActivePane { get; }
            IDockContent ActiveDocument { get; }
            DockPane ActiveDocumentPane { get; }
        }

        private class FocusManagerImpl : Component, IContentFocusManager, IFocusManager
        {
            private class HookEventArgs : EventArgs
            {
                [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
                public int HookCode;
                [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
                public IntPtr WParam;
                public IntPtr LParam;
            }

            private sealed class LocalWindowsHook : IDisposable
            {
                // Internal properties
                private IntPtr _mHHook = IntPtr.Zero;
                private readonly NativeMethods.HookProc _mFilterFunc;
                private readonly Win32.HookType _mHookType;

                // Event delegate
                public delegate void HookEventHandler(object sender, HookEventArgs e);

                // Event: HookInvoked 
                public event HookEventHandler HookInvoked;

                private void OnHookInvoked(HookEventArgs e)
                {
                    HookInvoked?.Invoke(this, e);
                }

                public LocalWindowsHook(Win32.HookType hook)
                {
                    _mHookType = hook;
                    _mFilterFunc = CoreHookProc;
                }

                // Default filter function
                private IntPtr CoreHookProc(int code, IntPtr wParam, IntPtr lParam)
                {
                    if (code < 0)
                        return NativeMethods.CallNextHookEx(_mHHook, code, wParam, lParam);

                    // Let clients determine what to do
                    var e = new HookEventArgs
                    {
                        HookCode = code,
                        WParam = wParam,
                        LParam = lParam
                    };
                    OnHookInvoked(e);

                    // Yield to the next hook in the chain
                    return NativeMethods.CallNextHookEx(_mHHook, code, wParam, lParam);
                }

                // Install the hook
                public void Install()
                {
                    if (_mHHook != IntPtr.Zero)
                        Uninstall();

                    int threadId = NativeMethods.GetCurrentThreadId();
                    _mHHook = NativeMethods.SetWindowsHookEx(_mHookType, _mFilterFunc, IntPtr.Zero, threadId);
                }

                // Uninstall the hook
                private void Uninstall()
                {
                    if (_mHHook == IntPtr.Zero) return;
                    NativeMethods.UnhookWindowsHookEx(_mHHook);
                    _mHHook = IntPtr.Zero;
                }

                ~LocalWindowsHook()
                {
                    Dispose();
                }

                public void Dispose()
                {
                    Uninstall();
                    GC.SuppressFinalize(this);
                }
            }

            // Use a static instance of the windows hook to prevent stack overflows in the windows kernel.
            [ThreadStatic]
            private static LocalWindowsHook _smLocalWindowsHook;

            private readonly LocalWindowsHook.HookEventHandler _mHookEventHandler;

            public FocusManagerImpl(DockPanel dockPanel)
            {
                DockPanel = dockPanel;
                if (Win32Helper.IsRunningOnMono)
                    return;
                _mHookEventHandler = HookEventHandler;

                // Ensure the windows hook has been created for this thread
                if (_smLocalWindowsHook == null)
                {
                    _smLocalWindowsHook = new LocalWindowsHook(Win32.HookType.WH_CALLWNDPROCRET);
                    _smLocalWindowsHook.Install();
                }

                _smLocalWindowsHook.HookInvoked += _mHookEventHandler;
            }

            private DockPanel DockPanel { get; }

            private bool _mDisposed;
            protected override void Dispose(bool disposing)
            {
                if (!_mDisposed && disposing)
                {
                    if (!Win32Helper.IsRunningOnMono)
                    {
                        _smLocalWindowsHook.HookInvoked -= _mHookEventHandler;
                    }

                    _mDisposed = true;
                }

                base.Dispose(disposing);
            }

            private IDockContent ContentActivating { get; set; }

            public void Activate(IDockContent content)
            {
                if (IsFocusTrackingSuspended)
                {
                    ContentActivating = content;
                    return;
                }

                if (content == null)
                    return;
                DockContentHandler handler = content.DockHandler;
                if (handler.Form.IsDisposed)
                    return; // Should not reach here, but better than throwing an exception
                if (ContentContains(content, handler.ActiveWindowHandle))
                {
                    if (!Win32Helper.IsRunningOnMono)
                    {
                        NativeMethods.SetFocus(handler.ActiveWindowHandle);
                    }
                }

                if (handler.Form.ContainsFocus)
                    return;

                if (handler.Form.SelectNextControl(handler.Form.ActiveControl, true, true, true, true))
                    return;

                if (Win32Helper.IsRunningOnMono)
                    return;

                // Since DockContent Form is not selectalbe, use Win32 SetFocus instead
                NativeMethods.SetFocus(handler.Form.Handle);
            }

            private List<IDockContent> ListContent { get; } = new List<IDockContent>();

            public void AddToList(IDockContent content)
            {
                if (ListContent.Contains(content) || IsInActiveList(content))
                    return;

                ListContent.Add(content);
            }

            public void RemoveFromList(IDockContent content)
            {
                if (IsInActiveList(content))
                    RemoveFromActiveList(content);
                if (ListContent.Contains(content))
                    ListContent.Remove(content);
            }

            private IDockContent LastActiveContent { get; set; }

            private bool IsInActiveList(IDockContent content)
            {
                return !(content.DockHandler.NextActive == null && LastActiveContent != content);
            }

            private void AddLastToActiveList(IDockContent content)
            {
                IDockContent last = LastActiveContent;
                if (last == content)
                    return;

                DockContentHandler handler = content.DockHandler;

                if (IsInActiveList(content))
                    RemoveFromActiveList(content);

                handler.PreviousActive = last;
                handler.NextActive = null;
                LastActiveContent = content;
                if (last != null)
                    last.DockHandler.NextActive = LastActiveContent;
            }

            private void RemoveFromActiveList(IDockContent content)
            {
                if (LastActiveContent == content)
                    LastActiveContent = content.DockHandler.PreviousActive;

                IDockContent prev = content.DockHandler.PreviousActive;
                IDockContent next = content.DockHandler.NextActive;
                if (prev != null)
                    prev.DockHandler.NextActive = next;
                if (next != null)
                    next.DockHandler.PreviousActive = prev;

                content.DockHandler.PreviousActive = null;
                content.DockHandler.NextActive = null;
            }

            public void GiveUpFocus(IDockContent content)
            {
                DockContentHandler handler = content.DockHandler;
                if (!handler.Form.ContainsFocus)
                    return;

                if (IsFocusTrackingSuspended)
                    DockPanel.DummyControl.Focus();

                if (LastActiveContent == content)
                {
                    IDockContent prev = handler.PreviousActive;
                    if (prev != null)
                        Activate(prev);
                    else if (ListContent.Count > 0)
                        Activate(ListContent[ListContent.Count - 1]);
                }
                else if (LastActiveContent != null)
                    Activate(LastActiveContent);
                else if (ListContent.Count > 0)
                    Activate(ListContent[ListContent.Count - 1]);
            }

            private static bool ContentContains(IDockContent content, IntPtr hWnd)
            {
                var control = FromChildHandle(hWnd);
                for (var parent = control; parent != null; parent = parent.Parent)
                    if (parent == content.DockHandler.Form)
                        return true;

                return false;
            }

            private uint _mCountSuspendFocusTracking;
            public void SuspendFocusTracking()
            {
                if (_mDisposed)
                    return;

                if (_mCountSuspendFocusTracking++ == 0)
                {
                    if (!Win32Helper.IsRunningOnMono)
                        _smLocalWindowsHook.HookInvoked -= _mHookEventHandler;
                }
            }

            public void ResumeFocusTracking()
            {
                if (_mDisposed || _mCountSuspendFocusTracking == 0)
                    return;

                if (--_mCountSuspendFocusTracking == 0)
                {
                    if (ContentActivating != null)
                    {
                        Activate(ContentActivating);
                        ContentActivating = null;
                    }

                    if (!Win32Helper.IsRunningOnMono)
                        _smLocalWindowsHook.HookInvoked += _mHookEventHandler;

                    if (!InRefreshActiveWindow)
                        RefreshActiveWindow();
                }
            }

            public bool IsFocusTrackingSuspended => _mCountSuspendFocusTracking != 0;

            // Windows hook event handler
            private void HookEventHandler(object sender, HookEventArgs e)
            {
                Win32.Msgs msg = (Win32.Msgs)Marshal.ReadInt32(e.LParam, IntPtr.Size * 3);

                if (msg == Win32.Msgs.WM_KILLFOCUS)
                {
                    IntPtr wParam = Marshal.ReadIntPtr(e.LParam, IntPtr.Size * 2);
                    DockPane pane = GetPaneFromHandle(wParam);
                    if (pane == null)
                        RefreshActiveWindow();
                }
                else if (msg == Win32.Msgs.WM_SETFOCUS || msg == Win32.Msgs.WM_MDIACTIVATE)
                    RefreshActiveWindow();
            }

            private DockPane GetPaneFromHandle(IntPtr hWnd)
            {
                var control = FromChildHandle(hWnd);

                DockPane pane = null;
                for (; control != null; control = control.Parent)
                {
                    var content = control as IDockContent;
                    if (content != null)
                        content.DockHandler.ActiveWindowHandle = hWnd;

                    if (content != null && content.DockHandler.DockPanel == DockPanel)
                        return content.DockHandler.Pane;

                    pane = control as DockPane;
                    if (pane != null && pane.DockPanel == DockPanel)
                        break;
                }

                return pane;
            }

            private bool InRefreshActiveWindow { get; set; }

            private void RefreshActiveWindow()
            {
                SuspendFocusTracking();
                InRefreshActiveWindow = true;

                DockPane oldActivePane = ActivePane;
                IDockContent oldActiveContent = ActiveContent;
                IDockContent oldActiveDocument = ActiveDocument;

                SetActivePane();
                SetActiveContent();
                SetActiveDocumentPane();
                SetActiveDocument();
                DockPanel.AutoHideWindow.RefreshActivePane();

                ResumeFocusTracking();
                InRefreshActiveWindow = false;

                if (oldActiveContent != ActiveContent)
                    DockPanel.OnActiveContentChanged(EventArgs.Empty);
                if (oldActiveDocument != ActiveDocument)
                    DockPanel.OnActiveDocumentChanged(EventArgs.Empty);
                if (oldActivePane != ActivePane)
                    DockPanel.OnActivePaneChanged(EventArgs.Empty);
            }

            public DockPane ActivePane { get; private set; }

            private void SetActivePane()
            {
                var value = Win32Helper.IsRunningOnMono ? null : GetPaneFromHandle(NativeMethods.GetFocus());
                if (ActivePane == value)
                    return;

                ActivePane?.SetIsActivated(false);

                ActivePane = value;

                ActivePane?.SetIsActivated(true);
            }

            public IDockContent ActiveContent { get; private set; }

            private void SetActiveContent()
            {
                var value = ActivePane?.ActiveContent;

                if (ActiveContent == value)
                    return;

                if (ActiveContent != null)
                    ActiveContent.DockHandler.IsActivated = false;

                ActiveContent = value;

                if (ActiveContent != null)
                {
                    ActiveContent.DockHandler.IsActivated = true;
                    if (!DockHelper.IsDockStateAutoHide(ActiveContent.DockHandler.DockState))
                        AddLastToActiveList(ActiveContent);
                }
            }

            public DockPane ActiveDocumentPane { get; private set; }

            private void SetActiveDocumentPane()
            {
                DockPane value = null;

                if (ActivePane != null && ActivePane.DockState == DockState.Document)
                    value = ActivePane;

                if (value == null && DockPanel.DockWindows != null)
                {
                    if (ActiveDocumentPane == null)
                        value = DockPanel.DockWindows[DockState.Document].DefaultPane;
                    else if (ActiveDocumentPane.DockPanel != DockPanel || ActiveDocumentPane.DockState != DockState.Document)
                        value = DockPanel.DockWindows[DockState.Document].DefaultPane;
                    else
                        value = ActiveDocumentPane;
                }

                if (ActiveDocumentPane == value)
                    return;

                ActiveDocumentPane?.SetIsActiveDocumentPane(false);

                ActiveDocumentPane = value;

                ActiveDocumentPane?.SetIsActiveDocumentPane(true);
            }

            public IDockContent ActiveDocument { get; private set; }

            private void SetActiveDocument()
            {
                IDockContent value = ActiveDocumentPane?.ActiveContent;

                if (ActiveDocument != null && ActiveDocument == value)
                    return;

                ActiveDocument = value;
            }
        }

        private IFocusManager FocusManager => _mFocusManager;

        internal IContentFocusManager ContentFocusManager => _mFocusManager;

        internal void SaveFocus()
        {
            DummyControl.Focus();
        }

        [Browsable(false)]
        public IDockContent ActiveContent => FocusManager.ActiveContent;

        [Browsable(false)]
        public DockPane ActivePane => FocusManager.ActivePane;

        [Browsable(false)]
        public IDockContent ActiveDocument => FocusManager.ActiveDocument;

        [Browsable(false)]
        public DockPane ActiveDocumentPane => FocusManager.ActiveDocumentPane;

        private static readonly object ActiveDocumentChangedEvent = new object();
        [LocalizedCategory("Category_PropertyChanged")]
        [LocalizedDescription("DockPanel_ActiveDocumentChanged_Description")]
        public event EventHandler ActiveDocumentChanged
        {
            add { Events.AddHandler(ActiveDocumentChangedEvent, value); }
            remove { Events.RemoveHandler(ActiveDocumentChangedEvent, value); }
        }
        protected virtual void OnActiveDocumentChanged(EventArgs e)
        {
            EventHandler handler = (EventHandler)Events[ActiveDocumentChangedEvent];
            handler?.Invoke(this, e);
        }

        private static readonly object ActiveContentChangedEvent = new object();
        [LocalizedCategory("Category_PropertyChanged")]
        [LocalizedDescription("DockPanel_ActiveContentChanged_Description")]
        public event EventHandler ActiveContentChanged
        {
            add { Events.AddHandler(ActiveContentChangedEvent, value); }
            remove { Events.RemoveHandler(ActiveContentChangedEvent, value); }
        }

        protected void OnActiveContentChanged(EventArgs e)
        {
            EventHandler handler = (EventHandler)Events[ActiveContentChangedEvent];
            handler?.Invoke(this, e);
        }

        private static readonly object DocumentDraggedEvent = new object();
        [LocalizedCategory("Category_PropertyChanged")]
        [LocalizedDescription("DockPanel_ActiveContentChanged_Description")]
        public event EventHandler DocumentDragged
        {
            add { Events.AddHandler(DocumentDraggedEvent, value); }
            remove { Events.RemoveHandler(DocumentDraggedEvent, value); }
        }

        internal void OnDocumentDragged()
        {
            EventHandler handler = (EventHandler)Events[DocumentDraggedEvent];
            handler?.Invoke(this, EventArgs.Empty);
        }

        private static readonly object ActivePaneChangedEvent = new object();
        [LocalizedCategory("Category_PropertyChanged")]
        [LocalizedDescription("DockPanel_ActivePaneChanged_Description")]
        public event EventHandler ActivePaneChanged
        {
            add { Events.AddHandler(ActivePaneChangedEvent, value); }
            remove { Events.RemoveHandler(ActivePaneChangedEvent, value); }
        }
        protected virtual void OnActivePaneChanged(EventArgs e)
        {
            EventHandler handler = (EventHandler)Events[ActivePaneChangedEvent];
            handler?.Invoke(this, e);
        }
    }
}
