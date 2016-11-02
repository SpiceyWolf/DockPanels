using System.Drawing;
using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Forms
{
    public sealed class DockPanelExtender
    {
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public interface IDockPaneFactory
        {
            DockPane CreateDockPane(IDockContent content, DockState visibleState, bool show);

            [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
            DockPane CreateDockPane(IDockContent content, FloatWindow floatWindow, bool show);

            DockPane CreateDockPane(IDockContent content, DockPane previousPane, DockAlignment alignment,
                                    double proportion, bool show);

            [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
            DockPane CreateDockPane(IDockContent content, Rectangle floatWindowBounds, bool show);
        }

        public interface IDockPaneSplitterControlFactory
        {
            DockPane.SplitterControlBase CreateSplitterControl(DockPane pane);
        }
        
        public interface IDockWindowSplitterControlFactory
        {
            SplitterBase CreateSplitterControl();
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public interface IFloatWindowFactory
        {
            FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane);
            FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds);
        }

        public interface IDockWindowFactory
        {
            DockWindow CreateDockWindow(DockPanel dockPanel, DockState dockState);
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public interface IDockPaneCaptionFactory
        {
            DockPaneCaption CreateDockPaneCaption(DockPane pane);
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public interface IDockPaneStripFactory
        {
            DockPaneStrip CreateDockPaneStrip(DockPane pane);
        }

        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        public interface IAutoHideStripFactory
        {
            AutoHideStrip CreateAutoHideStrip(DockPanel panel);
        }

        public interface IAutoHideWindowFactory
        {
            DockPanel.AutoHideWindowControl CreateAutoHideWindow(DockPanel panel);
        }

        public interface IPaneIndicatorFactory
        {
            DockPanel.IPaneIndicator CreatePaneIndicator();
        }

        public interface IPanelIndicatorFactory
        {
            DockPanel.IPanelIndicator CreatePanelIndicator(DockStyle style);
        }

        public interface IDockOutlineFactory
        {
            DockOutlineBase CreateDockOutline();
        }

        #region DefaultDockPaneFactory

        private class DefaultDockPaneFactory : IDockPaneFactory
        {
            public DockPane CreateDockPane(IDockContent content, DockState visibleState, bool show)
            {
                return new DockPane(content, visibleState, show);
            }

            public DockPane CreateDockPane(IDockContent content, FloatWindow floatWindow, bool show)
            {
                return new DockPane(content, floatWindow, show);
            }

            public DockPane CreateDockPane(IDockContent content, DockPane prevPane, DockAlignment alignment,
                                           double proportion, bool show)
            {
                return new DockPane(content, prevPane, alignment, proportion, show);
            }

            public DockPane CreateDockPane(IDockContent content, Rectangle floatWindowBounds, bool show)
            {
                return new DockPane(content, floatWindowBounds, show);
            }
        }

        #endregion

        #region DefaultDockPaneSplitterControlFactory

        private class DefaultDockPaneSplitterControlFactory : IDockPaneSplitterControlFactory
        {
            public DockPane.SplitterControlBase CreateSplitterControl(DockPane pane)
            {
                return new DockPane.DefaultSplitterControl(pane);
            }
        }

        #endregion
        
        #region DefaultDockWindowSplitterControlFactory

        private class DefaultDockWindowSplitterControlFactory : IDockWindowSplitterControlFactory
        {
            public SplitterBase CreateSplitterControl()
            {
                return new DockWindow.DefaultSplitterControl();
            }
        }

        #endregion

        #region DefaultFloatWindowFactory

        private class DefaultFloatWindowFactory : IFloatWindowFactory
        {
            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
            {
                return new FloatWindow(dockPanel, pane);
            }

            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
            {
                return new FloatWindow(dockPanel, pane, bounds);
            }
        }

        #endregion

        #region DefaultDockWindowFactory

        private class DefaultDockWindowFactory : IDockWindowFactory
        {
            public DockWindow CreateDockWindow(DockPanel dockPanel, DockState dockState)
            {
                return new DefaultDockWindow(dockPanel, dockState);
            }
        }

        #endregion

        #region DefaultDockPaneCaptionFactory

        private class DefaultDockPaneCaptionFactory : IDockPaneCaptionFactory
        {
            public DockPaneCaption CreateDockPaneCaption(DockPane pane)
            {
                return new DockPaneCaption(pane);
            }
        }

        #endregion

        #region DefaultDockPaneTabStripFactory

        private class DefaultDockPaneStripFactory : IDockPaneStripFactory
        {
            public DockPaneStrip CreateDockPaneStrip(DockPane pane)
            {
                return new DockPaneStrip(pane);
            }
        }

        #endregion

        #region DefaultAutoHideStripFactory

        private class DefaultAutoHideStripFactory : IAutoHideStripFactory
        {
            public AutoHideStrip CreateAutoHideStrip(DockPanel panel)
            {
                return new AutoHideStrip(panel);
            }
        }

        #endregion

        #region DefaultAutoHideWindowFactory

        public class DefaultAutoHideWindowFactory : IAutoHideWindowFactory
        {
            public DockPanel.AutoHideWindowControl CreateAutoHideWindow(DockPanel panel)
            {
                return new DockPanel.DefaultAutoHideWindowControl(panel);
            }
        }

        #endregion

        public class DefaultPaneIndicatorFactory : IPaneIndicatorFactory
        {
            public DockPanel.IPaneIndicator CreatePaneIndicator()
            {
                return new DockPanel.DefaultPaneIndicator();
            }
        }

        public class DefaultPanelIndicatorFactory : IPanelIndicatorFactory
        {
            public DockPanel.IPanelIndicator CreatePanelIndicator(DockStyle style)
            {
                return new DockPanel.DefaultPanelIndicator(style);
            }
        }

        public class DefaultDockOutlineFactory : IDockOutlineFactory
        {
            public DockOutlineBase CreateDockOutline()
            {
                return new DockPanel.DefaultDockOutline();
            }
        }

        internal DockPanelExtender(DockPanel dockPanel)
        {
            DockPanel = dockPanel;
        }

        private DockPanel DockPanel { get; }

        private IDockPaneFactory _mDockPaneFactory;

        public IDockPaneFactory DockPaneFactory
        {
            get { return _mDockPaneFactory ?? (_mDockPaneFactory = new DefaultDockPaneFactory()); }
            set
            {
                if (DockPanel.Panes.Count > 0)
                    throw new InvalidOperationException();

                _mDockPaneFactory = value;
            }
        }

        private IDockPaneSplitterControlFactory _mDockPaneSplitterControlFactory;

        public IDockPaneSplitterControlFactory DockPaneSplitterControlFactory
        {
            get
            {
                return _mDockPaneSplitterControlFactory ??
                       (_mDockPaneSplitterControlFactory = new DefaultDockPaneSplitterControlFactory());
            }

            set
            {
                if (DockPanel.Panes.Count > 0)
                {
                    throw new InvalidOperationException();
                }

                _mDockPaneSplitterControlFactory = value;
            }
        }
        
        private IDockWindowSplitterControlFactory _mDockWindowSplitterControlFactory;

        public IDockWindowSplitterControlFactory DockWindowSplitterControlFactory
        {
            get
            {
                return _mDockWindowSplitterControlFactory ??
                       (_mDockWindowSplitterControlFactory = new DefaultDockWindowSplitterControlFactory());
            }

            set
            {
                _mDockWindowSplitterControlFactory = value;
                DockPanel.ReloadDockWindows();
            }
        }

        private IFloatWindowFactory _mFloatWindowFactory;

        public IFloatWindowFactory FloatWindowFactory
        {
            get { return _mFloatWindowFactory ?? (_mFloatWindowFactory = new DefaultFloatWindowFactory()); }
            set
            {
                if (DockPanel.FloatWindows.Count > 0)
                    throw new InvalidOperationException();

                _mFloatWindowFactory = value;
            }
        }

        private IDockWindowFactory _mDockWindowFactory;

        public IDockWindowFactory DockWindowFactory
        {
            get { return _mDockWindowFactory ?? (_mDockWindowFactory = new DefaultDockWindowFactory()); }
            set
            {
                _mDockWindowFactory = value;
                DockPanel.ReloadDockWindows();
            }
        }

        private IDockPaneCaptionFactory _mDockPaneCaptionFactory;

        public IDockPaneCaptionFactory DockPaneCaptionFactory
        {
            get { return _mDockPaneCaptionFactory ?? (_mDockPaneCaptionFactory = new DefaultDockPaneCaptionFactory()); }
            set
            {
                if (DockPanel.Panes.Count > 0)
                    throw new InvalidOperationException();

                _mDockPaneCaptionFactory = value;
            }
        }

        private IDockPaneStripFactory _mDockPaneStripFactory;

        public IDockPaneStripFactory DockPaneStripFactory
        {
            get { return _mDockPaneStripFactory ?? (_mDockPaneStripFactory = new DefaultDockPaneStripFactory()); }
            set
            {
                if (DockPanel.Contents.Count > 0)
                    throw new InvalidOperationException();

                _mDockPaneStripFactory = value;
            }
        }

        private IAutoHideStripFactory _mAutoHideStripFactory;

        public IAutoHideStripFactory AutoHideStripFactory
        {
            get { return _mAutoHideStripFactory ?? (_mAutoHideStripFactory = new DefaultAutoHideStripFactory()); }
            set
            {
                if (DockPanel.Contents.Count > 0)
                    throw new InvalidOperationException();

                if (_mAutoHideStripFactory == value)
                    return;

                _mAutoHideStripFactory = value;
                DockPanel.ResetAutoHideStripControl();
            }
        }

        private IAutoHideWindowFactory _mAutoHideWindowFactory;
        
        public IAutoHideWindowFactory AutoHideWindowFactory
        {
            get { return _mAutoHideWindowFactory ?? (_mAutoHideWindowFactory = new DefaultAutoHideWindowFactory()); }
            set
            {
                if (DockPanel.Contents.Count > 0)
                {
                    throw new InvalidOperationException();
                }

                if (_mAutoHideWindowFactory == value)
                {
                    return;
                }

                _mAutoHideWindowFactory = value;
                DockPanel.ResetAutoHideStripWindow();
            }
        }

        private IPaneIndicatorFactory _mPaneIndicatorFactory;

        public IPaneIndicatorFactory PaneIndicatorFactory
        {
            get { return _mPaneIndicatorFactory ?? (_mPaneIndicatorFactory = new DefaultPaneIndicatorFactory()); }
            set { _mPaneIndicatorFactory = value; }
        }

        private IPanelIndicatorFactory _mPanelIndicatorFactory;

        public IPanelIndicatorFactory PanelIndicatorFactory
        {
            get { return _mPanelIndicatorFactory ?? (_mPanelIndicatorFactory = new DefaultPanelIndicatorFactory()); }
            set { _mPanelIndicatorFactory = value; }
        }

        private IDockOutlineFactory _mDockOutlineFactory;

        public IDockOutlineFactory DockOutlineFactory
        {
            get { return _mDockOutlineFactory ?? (_mDockOutlineFactory = new DefaultDockOutlineFactory()); }
            set { _mDockOutlineFactory = value; }
        }
    }
}
