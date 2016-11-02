using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace System.Windows.Forms
{
    internal sealed class DockTheme : Component, ITheme
    {
        public DockPanelSkin Skin;

        public DockTheme(DockPanel settings)
        {
            var skin = new DockPanelSkin();

            skin.AutoHideStripSkin.DockStripGradient.StartColor = settings.AutoHideTabStripStartColor;
            skin.AutoHideStripSkin.DockStripGradient.EndColor = settings.AutoHideTabStripEndColor;
            skin.AutoHideStripSkin.TabGradient.TextColor = settings.AutoHideTabStripTextColor;

            skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.StartColor = settings.DocumentTabStripStartColor;
            skin.DockPaneStripSkin.DocumentGradient.DockStripGradient.EndColor = settings.DocumentTabStripEndColor;

            skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.StartColor = settings.ActiveDocumentTabStartColor;
            skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.EndColor = settings.ActiveDocumentTabEndColor;
            skin.DockPaneStripSkin.DocumentGradient.ActiveTabGradient.TextColor = settings.ActiveDocumentTabTextColor;

            skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.StartColor = settings.InActiveDocumentTabStartColor;
            skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.EndColor = settings.InActiveDocumentTabEndColor;
            skin.DockPaneStripSkin.DocumentGradient.InactiveTabGradient.TextColor = settings.InActiveDocumentTabTextColor;

            skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.StartColor = settings.ToolTabStripStartColor;
            skin.DockPaneStripSkin.ToolWindowGradient.DockStripGradient.EndColor = settings.ToolTabStripEndColor;

            skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.StartColor = settings.ActiveToolTabStartColor;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.EndColor = settings.ActiveToolTabEndColor;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveTabGradient.TextColor = settings.ActiveToolTabTextColor;

            skin.DockPaneStripSkin.ToolWindowGradient.InactiveTabGradient.StartColor = settings.InActiveToolTabStartColor;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveTabGradient.EndColor = settings.InActiveToolTabEndColor;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveTabGradient.TextColor = settings.InActiveToolTabTextColor;
            
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.StartColor = settings.ActiveToolCaptionStartColor;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.EndColor = settings.ActiveToolCaptionEndColor;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.LinearGradientMode = settings.ActiveToolCaptionLinearGradientMode;
            skin.DockPaneStripSkin.ToolWindowGradient.ActiveCaptionGradient.TextColor = settings.ActiveToolCaptionTextColor;

            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.StartColor = settings.InActiveToolCaptionStartColor;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.EndColor = settings.InActiveToolCaptionEndColor;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.LinearGradientMode = settings.InActiveToolCaptionLinearGradientMode;
            skin.DockPaneStripSkin.ToolWindowGradient.InactiveCaptionGradient.TextColor = settings.InActiveToolCaptionTextColor;

            Skin = skin;
        }

        /// <summary>
        /// Applies the specified theme to the dock panel.
        /// </summary>
        /// <param name="dockPanel">The dock panel.</param>
        public void Apply(DockPanel dockPanel)
        {
            if (dockPanel == null)
            {
                throw new NullReferenceException("dockPanel");
            }

            Measures.SplitterSize = 4;
            dockPanel.Extender.DockPaneCaptionFactory = null;
            dockPanel.Extender.AutoHideStripFactory = null;
            dockPanel.Extender.AutoHideWindowFactory = null;
            dockPanel.Extender.DockPaneStripFactory = null;
            dockPanel.Extender.DockPaneSplitterControlFactory = null;
            dockPanel.Extender.DockWindowSplitterControlFactory = null;
            dockPanel.Extender.DockWindowFactory = null;
            dockPanel.Extender.PaneIndicatorFactory = null;
            dockPanel.Extender.PanelIndicatorFactory = null;
            dockPanel.Extender.DockOutlineFactory = null;
        }
    }
}