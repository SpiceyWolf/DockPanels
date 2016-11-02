namespace System.Windows.Forms
{
    public class DockContentEventArgs : EventArgs
    {
        public DockContentEventArgs(IDockContent content)
        {
            Content = content;
        }

        public IDockContent Content { get; }
    }
}
