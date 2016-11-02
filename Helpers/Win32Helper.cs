using System.Drawing;

namespace System.Windows.Forms
{
    internal static class Win32Helper
    {
        public static bool IsRunningOnMono { get; } = Type.GetType("Mono.Runtime") != null;

        internal static Control ControlAtPoint(Point pt)
        {
            return Control.FromChildHandle(NativeMethods.WindowFromPoint(pt));
        }

        internal static uint MakeLong(int low, int high)
        {
            return (uint)((high << 16) + low);
        }
    }
}
