using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace siroRE
{
    internal class mouse
    {
        [DllImport("User32")] public extern static void mouse_event(int dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);
        [DllImport("User32")] public extern static void SetCursorPos(int x, int y);
        [DllImport("User32")] public extern static bool GetCursorPos(out Point p);
        [DllImport("User32")] public extern static int ShowCursor(bool bShow);
        [DllImport("USER32.DLL")] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("USER32.DLL")] public static extern bool SetForegroundWindow(IntPtr hWnd);

        public enum MouseEventTFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        static public void Click(int x, int y)
        {

            mouse_event((int)MouseEventTFlags.LEFTUP, 0, 0, 0, IntPtr.Zero);
            Thread.Sleep(10);

            Cursor.Position = new Point(x, y);
            mouse_event((int)(MouseEventTFlags.LEFTDOWN), 0, 0, 0, IntPtr.Zero);
            Thread.Sleep(500);
            mouse_event((int)MouseEventTFlags.LEFTUP, 0, 0, 0, IntPtr.Zero);

            Console.WriteLine("Mouse.Click " + x + ", " + y);
        }
    }
}