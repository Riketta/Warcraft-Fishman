using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Fishman
{
    class WinApi
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        public static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CURSORINFO pci);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, UInt32 wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, UInt32 Msg, UInt32 wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("gdi32.dll")]
        public static extern IntPtr DeleteDC(IntPtr hDC);

        // for mouse_event
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const UInt32 WM_LBUTTONDOWN = 0x0201;
        public const UInt32 WM_LBUTTONUP = 0x0202;
        public const UInt32 CURSOR_SHOWING = 0x1;


        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public Int32 cbSize;        // Specifies the size, in bytes, of the structure.
                                        // The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).
            public Int32 flags;      // Specifies the cursor state. This parameter can be one of the following values:
                                     //    0  	       The cursor is hidden.
                                     //    CURSOR_SHOWING    The cursor is showing.
            public IntPtr hCursor;        // Handle to the cursor.
            public POINT ptScreenPos;           // A POINT structure that receives the screen coordinates of the cursor.
        }

        public struct POINT
        {
            public Int32 x;
            public Int32 y;
        }

       /// <summary>
       ///     Brings the thread that created the specified window into the foreground and activates the window. Keyboard input is
       ///     directed to the window, and various visual cues are changed for the user. The system assigns a slightly higher
       ///     priority to the thread that created the foreground window than it does to other threads.
       ///     <para>See for https://msdn.microsoft.com/en-us/library/windows/desktop/ms633539%28v=vs.85%29.aspx more information.</para>
       /// </summary>
       /// <param name="hWnd">
       ///     C++ ( hWnd [in]. Type: HWND )<br />A handle to the window that should be activated and brought to the foreground.
       /// </param>
       /// <returns>
       ///     <c>true</c> or nonzero if the window was brought to the foreground, <c>false</c> or zero If the window was not
       ///     brought to the foreground.
       /// </returns>
       /// <remarks>
       ///     The system restricts which processes can set the foreground window. A process can set the foreground window only if
       ///     one of the following conditions is true:
       ///     <list type="bullet">
       ///         <listheader>
       ///             <term>Conditions</term><description></description>
       ///         </listheader>
       ///         <item>The process is the foreground process.</item>
       ///         <item>The process was started by the foreground process.</item>
       ///         <item>The process received the last input event.</item>
       ///         <item>There is no foreground process.</item>
       ///         <item>The process is being debugged.</item>
       ///         <item>The foreground process is not a Modern Application or the Start Screen.</item>
       ///         <item>The foreground is not locked (see LockSetForegroundWindow).</item>
       ///         <item>The foreground lock time-out has expired (see SPI_GETFOREGROUNDLOCKTIMEOUT in SystemParametersInfo).</item>
       ///         <item>No menus are active.</item>
       ///     </list>
       ///     <para>
       ///         An application cannot force a window to the foreground while the user is working with another window.
       ///         Instead, Windows flashes the taskbar button of the window to notify the user.
       ///     </para>
       ///     <para>
       ///         A process that can set the foreground window can enable another process to set the foreground window by
       ///         calling the AllowSetForegroundWindow function. The process specified by dwProcessId loses the ability to set
       ///         the foreground window the next time the user generates input, unless the input is directed at that process, or
       ///         the next time a process calls AllowSetForegroundWindow, unless that process is specified.
       ///     </para>
       ///     <para>
       ///         The foreground process can disable calls to SetForegroundWindow by calling the LockSetForegroundWindow
       ///         function.
       ///     </para>
       /// </remarks>
        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Enumeration for virtual keys.
        /// </summary>
        public enum VirtualKeys : ushort
        {
            None = 0x00,
            LeftButton = 0x01,
            RightButton = 0x02,
            Cancel = 0x03,
            MiddleButton = 0x04,
            ExtraButton1 = 0x05,
            ExtraButton2 = 0x06,
            Back = 0x08,
            Tab = 0x09,
            Clear = 0x0C,
            Return = 0x0D,
            Shift = 0x10,
            Control = 0x11,
            Menu = 0x12,
            Pause = 0x13,
            CapsLock = 0x14,
            Kana = 0x15,
            Hangeul = 0x15,
            Hangul = 0x15,
            Junja = 0x17,
            Final = 0x18,
            Hanja = 0x19,
            Kanji = 0x19,
            Escape = 0x1B,
            Convert = 0x1C,
            NonConvert = 0x1D,
            Accept = 0x1E,
            ModeChange = 0x1F,
            Space = 0x20,
            Prior = 0x21,
            Next = 0x22,
            End = 0x23,
            Home = 0x24,
            Left = 0x25,
            Up = 0x26,
            Right = 0x27,
            Down = 0x28,
            Select = 0x29,
            Print = 0x2A,
            Execute = 0x2B,
            Snapshot = 0x2C,
            Insert = 0x2D,
            Delete = 0x2E,
            Help = 0x2F,
            N0 = 0x30,
            N1 = 0x31,
            N2 = 0x32,
            N3 = 0x33,
            N4 = 0x34,
            N5 = 0x35,
            N6 = 0x36,
            N7 = 0x37,
            N8 = 0x38,
            N9 = 0x39,
            A = 0x41,
            B = 0x42,
            C = 0x43,
            D = 0x44,
            E = 0x45,
            F = 0x46,
            G = 0x47,
            H = 0x48,
            I = 0x49,
            J = 0x4A,
            K = 0x4B,
            L = 0x4C,
            M = 0x4D,
            N = 0x4E,
            O = 0x4F,
            P = 0x50,
            Q = 0x51,
            R = 0x52,
            S = 0x53,
            T = 0x54,
            U = 0x55,
            V = 0x56,
            W = 0x57,
            X = 0x58,
            Y = 0x59,
            Z = 0x5A,
            LeftWindows = 0x5B,
            RightWindows = 0x5C,
            Application = 0x5D,
            Sleep = 0x5F,
            Numpad0 = 0x60,
            Numpad1 = 0x61,
            Numpad2 = 0x62,
            Numpad3 = 0x63,
            Numpad4 = 0x64,
            Numpad5 = 0x65,
            Numpad6 = 0x66,
            Numpad7 = 0x67,
            Numpad8 = 0x68,
            Numpad9 = 0x69,
            Multiply = 0x6A,
            Add = 0x6B,
            Separator = 0x6C,
            Subtract = 0x6D,
            Decimal = 0x6E,
            Divide = 0x6F,
            F1 = 0x70,
            F2 = 0x71,
            F3 = 0x72,
            F4 = 0x73,
            F5 = 0x74,
            F6 = 0x75,
            F7 = 0x76,
            F8 = 0x77,
            F9 = 0x78,
            F10 = 0x79,
            F11 = 0x7A,
            F12 = 0x7B,
            F13 = 0x7C,
            F14 = 0x7D,
            F15 = 0x7E,
            F16 = 0x7F,
            F17 = 0x80,
            F18 = 0x81,
            F19 = 0x82,
            F20 = 0x83,
            F21 = 0x84,
            F22 = 0x85,
            F23 = 0x86,
            F24 = 0x87,
            NumLock = 0x90,
            ScrollLock = 0x91,
            NEC_Equal = 0x92,
            Fujitsu_Jisho = 0x92,
            Fujitsu_Masshou = 0x93,
            Fujitsu_Touroku = 0x94,
            Fujitsu_Loya = 0x95,
            Fujitsu_Roya = 0x96,
            LeftShift = 0xA0,
            RightShift = 0xA1,
            LeftControl = 0xA2,
            RightControl = 0xA3,
            LeftMenu = 0xA4,
            RightMenu = 0xA5,
            BrowserBack = 0xA6,
            BrowserForward = 0xA7,
            BrowserRefresh = 0xA8,
            BrowserStop = 0xA9,
            BrowserSearch = 0xAA,
            BrowserFavorites = 0xAB,
            BrowserHome = 0xAC,
            VolumeMute = 0xAD,
            VolumeDown = 0xAE,
            VolumeUp = 0xAF,
            MediaNextTrack = 0xB0,
            MediaPrevTrack = 0xB1,
            MediaStop = 0xB2,
            MediaPlayPause = 0xB3,
            LaunchMail = 0xB4,
            LaunchMediaSelect = 0xB5,
            LaunchApplication1 = 0xB6,
            LaunchApplication2 = 0xB7,
            OEM1 = 0xBA,
            OEMPlus = 0xBB,
            OEMComma = 0xBC,
            OEMMinus = 0xBD,
            OEMPeriod = 0xBE,
            OEM2 = 0xBF,
            OEM3 = 0xC0,
            OEM4 = 0xDB,
            OEM5 = 0xDC,
            OEM6 = 0xDD,
            OEM7 = 0xDE,
            OEM8 = 0xDF,
            OEMAX = 0xE1,
            OEM102 = 0xE2,
            ICOHelp = 0xE3,
            ICO00 = 0xE4,
            ProcessKey = 0xE5,
            ICOClear = 0xE6,
            Packet = 0xE7,
            OEMReset = 0xE9,
            OEMJump = 0xEA,
            OEMPA1 = 0xEB,
            OEMPA2 = 0xEC,
            OEMPA3 = 0xED,
            OEMWSCtrl = 0xEE,
            OEMCUSel = 0xEF,
            OEMATTN = 0xF0,
            OEMFinish = 0xF1,
            OEMCopy = 0xF2,
            OEMAuto = 0xF3,
            OEMENLW = 0xF4,
            OEMBackTab = 0xF5,
            ATTN = 0xF6,
            CRSel = 0xF7,
            EXSel = 0xF8,
            EREOF = 0xF9,
            Play = 0xFA,
            Zoom = 0xFB,
            Noname = 0xFC,
            PA1 = 0xFD,
            OEMClear = 0xFE
        }
    }
}
