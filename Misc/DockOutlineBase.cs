using System.Drawing;

namespace System.Windows.Forms
{
    public abstract class DockOutlineBase
    {
        protected DockOutlineBase()
        {
            Init();
        }

        private void Init()
        {
            SetValues(Rectangle.Empty, null, DockStyle.None, -1);
            SaveOldValues();
        }

        protected Rectangle OldFloatWindowBounds { get; private set; }

        protected Control OldDockTo { get; private set; }

        protected DockStyle OldDock { get; private set; }

        protected int OldContentIndex { get; private set; }

        protected bool SameAsOldValue => FloatWindowBounds == OldFloatWindowBounds &&
                                         DockTo == OldDockTo &&
                                         Dock == OldDock &&
                                         ContentIndex == OldContentIndex;

        public Rectangle FloatWindowBounds { get; private set; }

        public Control DockTo { get; private set; }

        public DockStyle Dock { get; private set; }

        public int ContentIndex { get; private set; }

        public bool FlagFullEdge => ContentIndex != 0;

        public bool FlagTestDrop { get; set; }

        private void SaveOldValues()
        {
            OldDockTo = DockTo;
            OldDock = Dock;
            OldContentIndex = ContentIndex;
            OldFloatWindowBounds = FloatWindowBounds;
        }

        protected abstract void OnShow();

        protected abstract void OnClose();

        private void SetValues(Rectangle floatWindowBounds, Control dockTo, DockStyle dock, int contentIndex)
        {
            FloatWindowBounds = floatWindowBounds;
            DockTo = dockTo;
            Dock = dock;
            ContentIndex = contentIndex;
            FlagTestDrop = true;
        }

        private void TestChange()
        {
            if (FloatWindowBounds != OldFloatWindowBounds ||
                DockTo != OldDockTo ||
                Dock != OldDock ||
                ContentIndex != OldContentIndex)
                OnShow();
        }

        public void Show()
        {
            SaveOldValues();
            SetValues(Rectangle.Empty, null, DockStyle.None, -1);
            TestChange();
        }

        public void Show(DockPane pane, DockStyle dock)
        {
            SaveOldValues();
            SetValues(Rectangle.Empty, pane, dock, -1);
            TestChange();
        }

        public void Show(DockPane pane, int contentIndex)
        {
            SaveOldValues();
            SetValues(Rectangle.Empty, pane, DockStyle.Fill, contentIndex);
            TestChange();
        }

        public void Show(DockPanel dockPanel, DockStyle dock, bool fullPanelEdge)
        {
            SaveOldValues();
            SetValues(Rectangle.Empty, dockPanel, dock, fullPanelEdge ? -1 : 0);
            TestChange();
        }

        public void Show(Rectangle floatWindowBounds)
        {
            SaveOldValues();
            SetValues(floatWindowBounds, null, DockStyle.None, -1);
            TestChange();
        }

        public void Close()
        {
            OnClose();
        }
    }
}
