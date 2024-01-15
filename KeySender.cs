using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace LolLogin
{
    public static class KeySender
    {
        private static Random _random = new Random();

        //imports mouse_event function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        //imports keybd_event function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetAsyncKeyState(byte vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern Int16 GetKeyState(byte vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern Boolean BlockInput(Boolean fBlockIt);

        //declare consts for mouse messages
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;

        //declare consts for key scan codes
        public const byte VK_TAB = 0x09;
        public const byte VK_MENU = 0x12; // VK_MENU is Microsoft talk for the ALT key

        public const byte VK_RCONTROL = 0xA3;

        public const int KEYEVENTF_EXTENDEDKEY = 0x01;
        public const int KEYEVENTF_KEYUP = 0x02;

        //              Make    Break   Make    Break
        // 64	R CTRL	E0 1D	E0 9D	E0 14	E0 F0 14

        /// <summary>
        /// Only the shift modifier is currently supported. 
        /// TODO: Add support for alt/ctrl/windows keys in the future.
        /// </summary>
        /// <param name="keyToPress"></param>
        public static void SendKeyPressToActiveApplication(Keys keyToPress)
        {
            if ((keyToPress & Keys.Shift) == Keys.Shift)
                KeySender.keybd_event((Byte)Keys.ShiftKey, 0, 0, 0);

            if ((keyToPress & Keys.Control) == Keys.Control)
                KeySender.keybd_event((Byte)Keys.ControlKey, 0, 0, 0);

            KeySender.keybd_event((Byte)keyToPress, 0, 0, 0);

            // Make it look human.
            System.Threading.Thread.Sleep(50 + _random.Next(50));

            KeySender.keybd_event((Byte)keyToPress, 0, KeySender.KEYEVENTF_KEYUP, 0);

            if ((keyToPress & Keys.Shift) == Keys.Shift)
                KeySender.keybd_event((Byte)Keys.ShiftKey, 0, KeySender.KEYEVENTF_KEYUP, 0);

            if ((keyToPress & Keys.Control) == Keys.Control)
                KeySender.keybd_event((Byte)Keys.ControlKey, 0, KeySender.KEYEVENTF_KEYUP, 0);
        }

        public static void SendString(string value)
        {
            var keyConverter = new KeysConverter();
            var keyboardPointer = new KeyboardPointer();

            foreach (var letter in value)
            {
                if (keyboardPointer.GetKey(letter, out var key) == false)
                    throw new NullReferenceException($"couldn't convert {letter} from string {value}");

                KeySender.SendKeyPressToActiveApplication(key);
            }
        }

        public static void SendKeyDownToActiveApplication(Keys keyToPress)
        {
            KeySender.keybd_event((Byte)keyToPress, 0, 0, 0);
        }

        public static void SendKeyUpToActiveApplication(Keys keyToPress)
        {
            KeySender.keybd_event((Byte)keyToPress, 0, KeySender.KEYEVENTF_KEYUP, 0);
        }


        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;

        public const int KEYEVENTF_SCANCODE = 0x0008;

        //public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        //public const int KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBOARD_INPUT
        {
            public uint type;
            public ushort vk;
            public ushort scanCode;
            public uint flags;
            public uint time;
            public uint extrainfo;
            public uint padding1;
            public uint padding2;
        }

        [DllImport("User32.dll")]
        private static extern uint SendInput(uint numberOfInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] KEYBOARD_INPUT[] input, int structSize);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private const uint MapType_VirtualKeyToScanCode = 0;
        private const uint MapType_ScanCodeToVirtualKey = 1;
        private const uint MapType_VirtualKeyToUnshiftedScanCode = 2;
        private const uint MapType_ScanCodeToRightOrLeftHandVirtualKey = 3;







        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern short VkKeyScanEx(char ch, IntPtr dwhkl);
        [DllImport("user32.dll")]
        static extern bool UnloadKeyboardLayout(IntPtr hkl);
        [DllImport("user32.dll")]
        static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);
        public class KeyboardPointer : IDisposable
        {
            public KeyboardPointer() : this(CultureInfo.CurrentCulture)
            {
            }


            private readonly IntPtr pointer;
            public KeyboardPointer(int klid)
            {
                pointer = LoadKeyboardLayout(klid.ToString("X8"), 1);
            }
            public KeyboardPointer(CultureInfo culture)
              : this(culture.KeyboardLayoutId) { }
            public void Dispose()
            {
                UnloadKeyboardLayout(pointer);
                GC.SuppressFinalize(this);
            }
            ~KeyboardPointer()
            {
                UnloadKeyboardLayout(pointer);
            }
            // Converting to System.Windows.Forms.Key here, but
            // some other enumerations for similar tasks have the same
            // one-to-one mapping to the underlying Windows API values
            public bool GetKey(char character, out Keys key)
            {
                short keyNumber = VkKeyScanEx(character, pointer);
                if (keyNumber == -1)
                {
                    key = System.Windows.Forms.Keys.None;
                    return false;
                }
                key = (System.Windows.Forms.Keys)(((keyNumber & 0xFF00) << 8) | (keyNumber & 0xFF));
                return true;
            }
        }

        private static string DescribeKey(Keys key)
        {
            StringBuilder desc = new StringBuilder();
            if ((key & Keys.Shift) != Keys.None)
                desc.Append("Shift: ");
            if ((key & Keys.Control) != Keys.None)
                desc.Append("Control: ");
            if ((key & Keys.Alt) != Keys.None)
                desc.Append("Alt: ");
            return desc.Append(key & Keys.KeyCode).ToString();
        }
    }
}
