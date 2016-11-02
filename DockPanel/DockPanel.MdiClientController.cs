using System.Drawing;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace System.Windows.Forms
{
    partial class DockPanel
    {
        //  This class comes from Jacob Slusser's MdiClientController class:
        //  http://www.codeproject.com/cs/miscctrl/mdiclientcontroller.asp
        private sealed class MdiClientController : NativeWindow, IComponent
        {
            private bool _mAutoScroll = true;
            private BorderStyle _mBorderStyle = BorderStyle.Fixed3D;
            private Form _mParentForm;
            private ISite _mSite;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!disposing) return;
                Site?.Container?.Remove(this);
                Disposed?.Invoke(this, EventArgs.Empty);
            }

            public bool AutoScroll
            {
                private get { return _mAutoScroll; }
                set
                {
                    // By default the MdiClient control scrolls. It can appear though that
                    // there are no scrollbars by turning them off when the non-client
                    // area is calculated. I decided to expose this method following
                    // the .NET vernacular of an AutoScroll property.
                    _mAutoScroll = value;
                    if (MdiClient != null)
                        UpdateStyles();
                }
            }

            public BorderStyle BorderStyle
            {
                set
                {
                    // Error-check the enum.
                    if (!Enum.IsDefined(typeof(BorderStyle), value))
                        throw new InvalidEnumArgumentException();

                    _mBorderStyle = value;

                    if (MdiClient == null)
                        return;

                    // This property can actually be visible in design-mode,
                    // but to keep it consistent with the others,
                    // prevent this from being show at design-time.
                    if (Site != null && Site.DesignMode)
                        return;

                    // There is no BorderStyle property exposed by the MdiClient class,
                    // but this can be controlled by Win32 functions. A Win32 ExStyle
                    // of WS_EX_CLIENTEDGE is equivalent to a Fixed3D border and a
                    // Style of WS_BORDER is equivalent to a FixedSingle border.

                    // This code is inspired Jason Dori's article:
                    // "Adding designable borders to user controls".
                    // http://www.codeproject.com/cs/miscctrl/CsAddingBorders.asp

                    if (!Win32Helper.IsRunningOnMono)
                    {
                        // Get styles using Win32 calls
                        int style = NativeMethods.GetWindowLong(MdiClient.Handle, (int)Win32.GetWindowLongIndex.GWL_STYLE);
                        int exStyle = NativeMethods.GetWindowLong(MdiClient.Handle, (int)Win32.GetWindowLongIndex.GWL_EXSTYLE);

                        // Add or remove style flags as necessary.
                        switch (_mBorderStyle)
                        {
                            case BorderStyle.Fixed3D:
                                exStyle |= (int)Win32.WindowExStyles.WS_EX_CLIENTEDGE;
                                style &= ~(int)Win32.WindowStyles.WS_BORDER;
                                break;

                            case BorderStyle.FixedSingle:
                                exStyle &= ~(int)Win32.WindowExStyles.WS_EX_CLIENTEDGE;
                                style |= (int)Win32.WindowStyles.WS_BORDER;
                                break;

                            case BorderStyle.None:
                                style &= ~(int)Win32.WindowStyles.WS_BORDER;
                                exStyle &= ~(int)Win32.WindowExStyles.WS_EX_CLIENTEDGE;
                                break;
                        }

                        // Set the styles using Win32 calls
                        NativeMethods.SetWindowLong(MdiClient.Handle, (int)Win32.GetWindowLongIndex.GWL_STYLE, style);
                        NativeMethods.SetWindowLong(MdiClient.Handle, (int)Win32.GetWindowLongIndex.GWL_EXSTYLE, exStyle);
                    }

                    // Cause an update of the non-client area.
                    UpdateStyles();
                }
            }

            public MdiClient MdiClient { get; private set; }

            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Form ParentForm
            {
                get { return _mParentForm; }
                set
                {
                    // If the ParentForm has previously been set,
                    // unwire events connected to the old parent.
                    if (_mParentForm != null)
                    {
                        _mParentForm.HandleCreated -= ParentFormHandleCreated;
                        _mParentForm.MdiChildActivate -= ParentFormMdiChildActivate;
                    }

                    _mParentForm = value;

                    if (_mParentForm == null)
                        return;

                    // If the parent form has not been created yet,
                    // wait to initialize the MDI client until it is.
                    if (_mParentForm.IsHandleCreated)
                    {
                        InitializeMdiClient();
                        RefreshProperties();
                    }
                    else
                        _mParentForm.HandleCreated += ParentFormHandleCreated;

                    _mParentForm.MdiChildActivate += ParentFormMdiChildActivate;
                }
            }

            public ISite Site
            {
                get { return _mSite; }
                set
                {
                    _mSite = value;

                    if (_mSite == null)
                        return;

                    // If the component is dropped onto a form during design-time,
                    // set the ParentForm property.
                    var host = value.GetService(typeof(IDesignerHost)) as IDesignerHost;
                    var parent = host?.RootComponent as Form;
                    if (parent != null)
                        ParentForm = parent;
                }
            }

            public event EventHandler Disposed;

            public event EventHandler HandleAssigned;

            public event EventHandler MdiChildActivate;

            public event LayoutEventHandler Layout;

            private void OnHandleAssigned(EventArgs e)
            {
                // Raise the HandleAssigned event.
                HandleAssigned?.Invoke(this, e);
            }

            private void OnMdiChildActivate(EventArgs e)
            {
                // Raise the MdiChildActivate event
                MdiChildActivate?.Invoke(this, e);
            }

            private void OnLayout(LayoutEventArgs e)
            {
                // Raise the Layout event
                Layout?.Invoke(this, e);
            }

            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case (int)Win32.Msgs.WM_NCCALCSIZE:
                        // If AutoScroll is set to false, hide the scrollbars when the control
                        // calculates its non-client area.
                        if (!AutoScroll)
                        {
                            if (!Win32Helper.IsRunningOnMono)
                            {
                                NativeMethods.ShowScrollBar(m.HWnd, (int)Win32.ScrollBars.SB_BOTH, 0 /*false*/);
                            }
                        }

                        break;
                }

                base.WndProc(ref m);
            }

            private void ParentFormHandleCreated(object sender, EventArgs e)
            {
                // The form has been created, unwire the event, and initialize the MdiClient.
                _mParentForm.HandleCreated -= ParentFormHandleCreated;
                InitializeMdiClient();
                RefreshProperties();
            }

            private void ParentFormMdiChildActivate(object sender, EventArgs e)
            {
                OnMdiChildActivate(e);
            }

            private void MdiClientLayout(object sender, LayoutEventArgs e)
            {
                OnLayout(e);
            }

            private void MdiClientHandleDestroyed(object sender, EventArgs e)
            {
                // If the MdiClient handle has been released, drop the reference and
                // release the handle.
                if (MdiClient != null)
                {
                    MdiClient.HandleDestroyed -= MdiClientHandleDestroyed;
                    MdiClient = null;
                }

                ReleaseHandle();
            }

            private void InitializeMdiClient()
            {
                // If the mdiClient has previously been set, unwire events connected
                // to the old MDI.
                if (MdiClient != null)
                {
                    MdiClient.HandleDestroyed -= MdiClientHandleDestroyed;
                    MdiClient.Layout -= MdiClientLayout;
                }

                if (ParentForm == null)
                    return;

                // Get the MdiClient from the parent form.
                foreach (Control control in ParentForm.Controls)
                {
                    // If the form is an MDI container, it will contain an MdiClient control
                    // just as it would any other control.

                    MdiClient = control as MdiClient;
                    if (MdiClient == null)
                        continue;

                    // Assign the MdiClient Handle to the NativeWindow.
                    ReleaseHandle();
                    AssignHandle(MdiClient.Handle);

                    // Raise the HandleAssigned event.
                    OnHandleAssigned(EventArgs.Empty);

                    // Monitor the MdiClient for when its handle is destroyed.
                    MdiClient.HandleDestroyed += MdiClientHandleDestroyed;
                    MdiClient.Layout += MdiClientLayout;

                    break;
                }
            }

            private void RefreshProperties()
            {
                // Refresh all the properties
                BorderStyle = _mBorderStyle;
                AutoScroll = _mAutoScroll;
            }

            private void UpdateStyles()
            {
                // To show style changes, the non-client area must be repainted. Using the
                // control's Invalidate method does not affect the non-client area.
                // Instead use a Win32 call to signal the style has changed.
                if (!Win32Helper.IsRunningOnMono)
                    NativeMethods.SetWindowPos(MdiClient.Handle, IntPtr.Zero, 0, 0, 0, 0,
                        Win32.FlagsSetWindowPos.SWP_NOACTIVATE |
                        Win32.FlagsSetWindowPos.SWP_NOMOVE |
                        Win32.FlagsSetWindowPos.SWP_NOSIZE |
                        Win32.FlagsSetWindowPos.SWP_NOZORDER |
                        Win32.FlagsSetWindowPos.SWP_NOOWNERZORDER |
                        Win32.FlagsSetWindowPos.SWP_FRAMECHANGED);
            }
        }

        private MdiClientController _mMdiClientController;
        private MdiClientController GetMdiClientController()
        {
            if (_mMdiClientController != null) return _mMdiClientController;
            _mMdiClientController = new MdiClientController();
            _mMdiClientController.HandleAssigned += MdiClientHandleAssigned;
            _mMdiClientController.MdiChildActivate += ParentFormMdiChildActivate;

            return _mMdiClientController;
        }

        private void ParentFormMdiChildActivate(object sender, EventArgs e)
        {
            if (GetMdiClientController().ParentForm == null)
                return;

            var content = GetMdiClientController().ParentForm.ActiveMdiChild as IDockContent;
            if (content == null)
                return;

            if (content.DockHandler.DockPanel == this && content.DockHandler.Pane != null)
                content.DockHandler.Pane.ActiveContent = content;
        }

        private bool MdiClientExists => GetMdiClientController().MdiClient != null;

        private void SuspendMdiClientLayout()
        {
            if (GetMdiClientController().MdiClient != null)
                GetMdiClientController().MdiClient.SuspendLayout();
        }

        private void ResumeMdiClientLayout(bool perform)
        {
            if (GetMdiClientController().MdiClient != null)
                GetMdiClientController().MdiClient.ResumeLayout(perform);
        }

        private void PerformMdiClientLayout()
        {
            if (GetMdiClientController().MdiClient != null)
                GetMdiClientController().MdiClient.PerformLayout();
        }

        // Called when:
        // 1. DockPanel.Visible changed
        // 2. MdiClientController.Handle assigned
        private void SetMdiClient()
        {
            MdiClientController controller = GetMdiClientController();
            controller.AutoScroll = true;
            controller.BorderStyle = BorderStyle.Fixed3D;
            if (MdiClientExists)
                controller.MdiClient.Dock = DockStyle.Fill;
        }

        internal Rectangle RectangleToMdiClient(Rectangle rect)
        {
            return MdiClientExists ? GetMdiClientController().MdiClient.RectangleToClient(rect) : Rectangle.Empty;
        }
    }
}
