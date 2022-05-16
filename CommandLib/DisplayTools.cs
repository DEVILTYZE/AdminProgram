using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CommandLib
{
    static class DisplayTools
    {
        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private enum DeviceCap
        {
            Desktopvertres = 117,
            Desktophorzres = 118
        }

        public static Size GetPhysicalDisplaySize()
        {
            var g = Graphics.FromHwnd(IntPtr.Zero);
            var desktop = g.GetHdc();

            var physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.Desktopvertres);
            var physicalScreenWidth = GetDeviceCaps(desktop, (int)DeviceCap.Desktophorzres);

            return new Size(physicalScreenWidth, physicalScreenHeight);
        }


    }
}