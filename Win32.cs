
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LolLogin
{
    /// <summary>
    /// Summary description for Win32.
    /// </summary>
    public class Win32
    {
        // The WM_COMMAND message is sent when the user selects a command item from 
        // a menu, when a control sends a notification message to its parent window, 
        // or when an accelerator keystroke is translated.
        public const int WM_KEYDOWN = 0x100;
        public const int WM_KEYUP = 0x101;
        public const int WM_COMMAND = 0x111;
        public const int WM_LBUTTONDOWN = 0x201;
        public const int WM_LBUTTONUP = 0x202;
        public const int WM_LBUTTONDBLCLK = 0x203;
        public const int WM_RBUTTONDOWN = 0x204;
        public const int WM_RBUTTONUP = 0x205;
        public const int WM_RBUTTONDBLCLK = 0x206;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        // The FindWindow function retrieves a handle to the top-level window whose
        // class name and window name match the specified strings.
        // This function does not search child windows.
        // This function does not perform a case-sensitive search.
        [DllImport("User32.dll")]
        public static extern int FindWindow(string strClassName, string strWindowName);

        // The FindWindowEx function retrieves a handle to a window whose class name 
        // and window name match the specified strings.
        // The function searches child windows, beginning with the one following the
        // specified child window.
        // This function does not perform a case-sensitive search.
        [DllImport("User32.dll")]
        public static extern int FindWindowEx(
            int hwndParent,
            int hwndChildAfter,
            string strClassName,
            string strWindowName);


        // The SendMessage function sends the specified message to a window or windows. 
        // It calls the window procedure for the specified window and does not return
        // until the window procedure has processed the message. 
        [DllImport("User32.dll")]
        public static extern Int32 SendMessage(
            IntPtr hWnd,               // handle to destination window
            uint Msg,                // message
            IntPtr wParam,             // first message parameter
            IntPtr lParam);            // second message parameter


        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput
        public const int MOUSEEVENTF_MOVE = 0x0001;
        public const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const int MOUSEEVENTF_LEFTUP = 0x0004;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);

        //Import the SetForeground API to activate it
        [DllImport("User32.dll", EntryPoint = "SetForegroundWindow")]
        public static extern IntPtr SetForegroundWindowNative(IntPtr hWnd);


        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint ShellExecute(IntPtr hwnd, string strOperation, string strFile, string strParameters, string strDirectory, Int32 nShowCmd);


        public static void SendLeftClick(Point point)
        {
            // Set Cursor
            SetCursorPos(point.X, point.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(300);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void SendLeftDragAndHold(int startX, int startY, int amountToMoveX, int amountToMoveY)
        {
            SendLeftDragAndHold(startX, startY, amountToMoveX, amountToMoveY, 0, null);
        }

        private static void SendLeftDragAndHold(int startX, int startY, int amountToMoveX, int amountToMoveY, int releaseDelayMS, CancellationTokenSource cancellationToken)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Stopwatch delaySW = new Stopwatch();
            delaySW.Start();

            // Set Cursor
            SetCursorPos(startX, startY);

            // Click once
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(100);

            int currentX = 0;
            int currentY = 0;

            int xDirection = 1;
            if (amountToMoveX < 0)
                xDirection = -1;

            amountToMoveX = Math.Abs(amountToMoveX);

            int yDirection = 1;
            if (amountToMoveY < 0)
                yDirection = -1;

            amountToMoveY = Math.Abs(amountToMoveY);


            while (currentX < amountToMoveX - 5 || currentY < amountToMoveY - 5)
            {
                int offsetX = 0;
                int offsetY = 0;

                if (currentX < amountToMoveX - 5)
                {
                    offsetX = 5;
                    currentX += 5;
                }
                else if (currentX != amountToMoveX)
                {
                    offsetX = amountToMoveX - currentX;
                    currentX = amountToMoveX;
                }

                if (currentY < amountToMoveY - 5)
                {
                    offsetY = 5;
                    currentY += 5;
                }
                else if (currentY != amountToMoveY)
                {
                    offsetY = amountToMoveY - currentY;
                    currentY = amountToMoveY;
                }

                mouse_event(MOUSEEVENTF_MOVE, offsetX * xDirection, offsetY * yDirection, 0, 0);

                Thread.Sleep(10);
            }
        }

        public static void ReleaseLeftDragHold()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
    }
}