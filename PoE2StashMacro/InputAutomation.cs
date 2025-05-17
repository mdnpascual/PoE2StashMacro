using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static PoE2StashMacro.InputAutomation;
using Point = System.Drawing.Point;

namespace PoE2StashMacro
{
    public class InputAutomation
    {
        Screen screen;
        protected bool isProgrammaticKeyPress = false;
        int repeatCount = 3;

        public InputAutomation(Screen screen)
        {
            this.screen = screen;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type; // Input type (0 for keyboard, 1 for mouse)
            public INPUTUNION u;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk; // Virtual key code
            public ushort wScan; // Hardware scan code
            public uint dwFlags; // Flags for the input
            public uint time; // Timestamp for the event
            public IntPtr dwExtraInfo; // Additional information
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx; // X coordinate
            public int dy; // Y coordinate
            public uint mouseData; // Mouse data
            public uint dwFlags; // Flags for mouse input
            public uint time; // Timestamp for the event
            public IntPtr dwExtraInfo; // Additional information
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HWDINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HWDINPUT hi;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        // Considered as a Lock so no keys can't be pressed
        public bool IsProgrammaticKeyPress()
        {
            return isProgrammaticKeyPress;
        }

        public void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public void MouseMove(Point pos, int delay)
        {
            INPUT[] inputs = new INPUT[1];
            Array.Clear(inputs, 0, inputs.Length);

            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dx = (int)(pos.X * (65536.0 / screen.Bounds.Width));
            inputs[0].u.mi.dy = (int)(pos.Y * (65536.0 / screen.Bounds.Height));
            inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Task.Delay(delay).Wait();
        }

        public void MouseLeftClick()
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTUP;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public void KeyboardDown(Keys key, int delay)
        {
            INPUT[] inputs = new INPUT[1];
            Array.Clear(inputs, 0, inputs.Length);

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)key;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Task.Delay(delay).Wait();
        }

        public void KeyboardUp(Keys key, int delay)
        {
            INPUT[] inputs = new INPUT[1];
            Array.Clear(inputs, 0, inputs.Length);

            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)key;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Task.Delay(delay).Wait();
        }

        public void ReverseDisengageAction(Point oppositeCursorPos, Point origPos, Keys key)
        {
            isProgrammaticKeyPress = true;

            // 1. Move mouse to oppositeCursorPos
            for (int i = 0; i < repeatCount; i++) { MouseMove(oppositeCursorPos, 1); }

            // 2. Key down on key
            KeyboardDown(key, 10);

            // 3. Key up on key
            KeyboardUp(key, 50);

            // 4. Move mouse back to origPos
            for (int i = 0; i < repeatCount; i++) { MouseMove(origPos, 1); }

            isProgrammaticKeyPress = false;
        }

        public void LeftAltCopy()
        {
            isProgrammaticKeyPress = true;

            INPUT[] inputs = new INPUT[3];
            Array.Clear(inputs, 0, inputs.Length);

            // 1. Key down on Left Alt (LMenu)
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)Keys.LMenu;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            // 2. Key down on Left Control (LControlKey)
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = (ushort)Keys.LControlKey;
            inputs[1].u.ki.wScan = 0;
            inputs[1].u.ki.dwFlags = 0; // Key down
            inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

            // 3. Key down on C
            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].u.ki.wVk = (ushort)Keys.C;
            inputs[2].u.ki.wScan = 0;
            inputs[2].u.ki.dwFlags = 0; // Key down
            inputs[2].u.ki.dwExtraInfo = IntPtr.Zero;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Task.Delay(30).Wait();

            // 4. Key up on C
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)Keys.C;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            // 5. Key up on Left Control (LControlKey)
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = (ushort)Keys.LControlKey;
            inputs[1].u.ki.wScan = 0;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

            // 6. Key up on Left Alt (LMenu)
            inputs[2].type = INPUT_KEYBOARD;
            inputs[2].u.ki.wVk = (ushort)Keys.LMenu;
            inputs[2].u.ki.wScan = 0;
            inputs[2].u.ki.dwFlags = KEYEVENTF_KEYUP;
            inputs[2].u.ki.dwExtraInfo = IntPtr.Zero;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            Task.Delay(50).Wait();

            isProgrammaticKeyPress = false;
        }

        public void ClickAtPos(Point position)
        {
            isProgrammaticKeyPress = true;

            // 1. Move mouse to the specified position
            MouseMove(position, 15);

            // 2. Mouse left button down
            MouseLeftClick();

            Task.Delay(100).Wait();

            isProgrammaticKeyPress = false;
        }
    }
}
