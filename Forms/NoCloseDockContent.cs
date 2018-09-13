namespace System.Windows.Forms
{
    public class NoCloseDockContent : DockContent
    {
        public NoCloseDockContent()
        {
            FormClosing += (sender, e) =>
            {
                DockState = DockState.Hidden;
                e.Cancel = true;
            };
        }
    }
}