namespace System.Windows.Forms
{
    using System.ComponentModel;

    partial class DockPanel
    {
        [LocalizedCategory("Category_Docking")]
        [LocalizedDescription("DockPanel_DockPanelSkin")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public DockPanelSkin Skin => _mDockPanelTheme.Skin;

        private readonly DockTheme _mDockPanelTheme;
    }
}
