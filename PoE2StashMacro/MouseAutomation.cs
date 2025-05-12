using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace PoE2StashMacro
{
    public class MouseAutomation
    {
        private Screen screen;
        public MouseAutomation(Screen screen)
        {
            this.screen = screen;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private const uint KEYEVENTF_KEYUP = 0x0002;

        protected bool isProgrammaticKeyPress = false;

        /*
        public void MouseMove(int x, int y)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = x;
            inputs[0].mi.dy = y;
            inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
        */

        public void MouseMove(int x, int y)
        {
            // Get the screen dimensions
            int screenWidth = screen.Bounds.Width;
            int screenHeight = screen.Bounds.Height;

            // Normalize the coordinates
            int normalizedX = (x * 65536) / screenWidth;
            int normalizedY = (y * 65536) / screenHeight;

            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dx = normalizedX;
            inputs[0].mi.dy = normalizedY;
            inputs[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public void MouseLeftClick()
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTUP;
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public async Task PressKeyAsync(Keys key, Keys modifier, Keys modifier2)
        {
            isProgrammaticKeyPress = true;
            if (modifier != Keys.None)
            {
                keybd_event((byte)modifier, 0, 0, UIntPtr.Zero);
                await Task.Delay(16);
            }

            if (modifier2 != Keys.None)
            {
                keybd_event((byte)modifier2, 0, 0, UIntPtr.Zero);
                await Task.Delay(16);
            }

            // Main key down
            keybd_event((byte)key, 0, 0, UIntPtr.Zero);
            await Task.Delay(50);

            // Main key up
            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(16);

            if (modifier != Keys.None)
            {
                keybd_event((byte)modifier, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                await Task.Delay(16);
            }

            if (modifier2 != Keys.None)
            {
                keybd_event((byte)modifier2, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                await Task.Delay(16);
            }

            await Task.Delay(16);
            isProgrammaticKeyPress = false;
        }

        public async Task MoveMouseAndPressKeyAsync(Point oppositeCursorPos, Point origPos, Keys key)
        {
            isProgrammaticKeyPress = true;
            bool running = true;
            // Start a task to continuously move the mouse to the opposite position
            var moveTask = Task.Run(async () =>
            {
                while (running)
                {
                    MouseMove(oppositeCursorPos.X, oppositeCursorPos.Y);
                    await Task.Delay(2);
                }
            });

            await Task.Delay(16);

            keybd_event((byte)key, 0, 0, UIntPtr.Zero);
            await Task.Delay(16);

            keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(66);

            // Stop the mouse movement task
            running = false;
            moveTask.Wait();

            // Move the mouse back to the original position
            bool running2 = true;
            var moveTask2 = Task.Run(async () =>
            {
                while (running2)
                {
                    MouseMove(origPos.X, origPos.Y);
                    await Task.Delay(2);
                }
            });

            await Task.Delay(20);

            running2 = false;
            moveTask2.Wait();

            isProgrammaticKeyPress = false;
        }

        // Considered as a Lock so no keys can't be pressed
        public bool IsProgrammaticKeyPress()
        {
            return isProgrammaticKeyPress;
        }
    }
}
