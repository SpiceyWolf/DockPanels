using System.Drawing;
using System.Drawing.Drawing2D;

namespace System.Windows.Forms {
    public static class DrawingRoutines {
        public static void SafelyDrawLinearGradient(Rectangle rectangle, Color startColor, Color endColor,
            LinearGradientMode mode, Graphics graphics)
        {
            if (rectangle.Width > 0 && rectangle.Height > 0)
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(rectangle, startColor, endColor, mode))
                {
                    graphics.FillRectangle(brush, rectangle);
                }
            }
        }
    }
}
