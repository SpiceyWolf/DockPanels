using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace System.Windows.Forms
{
    #region DockPanelSkin classes
    /// <summary>
    /// The skin to use when displaying the DockPanel.
    /// The skin allows custom gradient color schemes to be used when drawing the
    /// DockStrips and Tabs.
    /// </summary>
    public class DockPanelSkin
    {
        /// <summary>
        /// The skin used to display the auto hide strips and tabs.
        /// </summary>
        public AutoHideStripSkin AutoHideStripSkin { get; set; } = new AutoHideStripSkin();

        /// <summary>
        /// The skin used to display the Document and ToolWindow style DockStrips and Tabs.
        /// </summary>
        public DockPaneStripSkin DockPaneStripSkin { get; set; } = new DockPaneStripSkin();
    }

    /// <summary>
    /// The skin used to display the auto hide strip and tabs.
    /// </summary>
    public class AutoHideStripSkin
    {
        /// <summary>
        /// The gradient color skin for the DockStrips.
        /// </summary>
        public DockPanelGradient DockStripGradient { get; set; } = new DockPanelGradient();

        /// <summary>
        /// The gradient color skin for the Tabs.
        /// </summary>
        public TabGradient TabGradient { get; set; } = new TabGradient();

        /// <summary>
        /// The gradient color skin for the Tabs.
        /// </summary>
        public DockStripBackground DockStripBackground { get; set; } = new DockStripBackground();


        /// <summary>
        /// Font used in AutoHideStrip elements.
        /// </summary>
        [DefaultValue(typeof(SystemFonts), "MenuFont")]
        public Font TextFont { get; set; } = SystemFonts.MenuFont;
    }

    /// <summary>
    /// The skin used to display the document and tool strips and tabs.
    /// </summary>
    public class DockPaneStripSkin
    {
        /// <summary>
        /// The skin used to display the Document style DockPane strip and tab.
        /// </summary>
        public DockPaneStripGradient DocumentGradient { get; set; } = new DockPaneStripGradient();

        /// <summary>
        /// The skin used to display the ToolWindow style DockPane strip and tab.
        /// </summary>
        public DockPaneStripToolWindowGradient ToolWindowGradient { get; set; } = new DockPaneStripToolWindowGradient();

        /// <summary>
        /// Font used in DockPaneStrip elements.
        /// </summary>
        [DefaultValue(typeof(SystemFonts), "MenuFont")]
        public Font TextFont { get; set; } = SystemFonts.MenuFont;
    }

    /// <summary>
    /// The skin used to display the DockPane ToolWindow strip and tab.
    /// </summary>
    public class DockPaneStripToolWindowGradient : DockPaneStripGradient
    {
        /// <summary>
        /// The skin used to display the active ToolWindow caption.
        /// </summary>
        public TabGradient ActiveCaptionGradient { get; set; } = new TabGradient();

        /// <summary>
        /// The skin used to display the inactive ToolWindow caption.
        /// </summary>
        public TabGradient InactiveCaptionGradient { get; set; } = new TabGradient();
    }

    /// <summary>
    /// The skin used to display the DockPane strip and tab.
    /// </summary>
    public class DockPaneStripGradient
    {
        /// <summary>
        /// The gradient color skin for the DockStrip.
        /// </summary>
        public DockPanelGradient DockStripGradient { get; set; } = new DockPanelGradient();

        /// <summary>
        /// The skin used to display the active DockPane tabs.
        /// </summary>
        public TabGradient ActiveTabGradient { get; set; } = new TabGradient();

        public TabGradient HoverTabGradient { get; set; } = new TabGradient();

        /// <summary>
        /// The skin used to display the inactive DockPane tabs.
        /// </summary>
        public TabGradient InactiveTabGradient { get; set; } = new TabGradient();
    }

    /// <summary>
    /// The skin used to display the dock pane tab
    /// </summary>
    public class TabGradient : DockPanelGradient
    {
        /// <summary>
        /// The text color.
        /// </summary>
        [DefaultValue(typeof(SystemColors), "ControlText")]
        public Color TextColor { get; set; } = SystemColors.ControlText;
    }

        /// <summary>
    /// The skin used to display the dock pane tab
    /// </summary>
    public class DockStripBackground
    {
            //private LinearGradientMode m_linearGradientMode = LinearGradientMode.Horizontal;

        /// <summary>
        /// The beginning gradient color.
        /// </summary>
        [DefaultValue(typeof(SystemColors), "Control")]
        public Color StartColor { get; set; } = SystemColors.Control;

            /// <summary>
        /// The ending gradient color.
        /// </summary>
        [DefaultValue(typeof(SystemColors), "Control")]
        public Color EndColor { get; set; } = SystemColors.Control;
    }
    

    /// <summary>
    /// The gradient color skin.
    /// </summary>
    public class DockPanelGradient
    {
        /// <summary>
        /// The beginning gradient color.
        /// </summary>
        [DefaultValue(typeof(SystemColors), "Control")]
        public Color StartColor { get; set; } = SystemColors.Control;

        /// <summary>
        /// The ending gradient color.
        /// </summary>
        [DefaultValue(typeof(SystemColors), "Control")]
        public Color EndColor { get; set; } = SystemColors.Control;

        /// <summary>
        /// The gradient mode to display the colors.
        /// </summary>
        [DefaultValue(LinearGradientMode.Horizontal)]
        public LinearGradientMode LinearGradientMode { get; set; } = LinearGradientMode.Horizontal;
    }

    #endregion
}
