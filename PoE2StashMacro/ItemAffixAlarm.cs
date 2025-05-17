using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PoE2StashMacro
{
    internal class ItemAffixAlarm
    {
        private InputAutomation inputAutomation;
        private CancellationToken cancellation;
        private MediaPlayer mediaPlayer;
        private OverlayWindow overlayWindow;

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern void GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr hMem);

        private const uint CF_TEXT = 1; // Clipboard format for text

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_LCONTROL = 0xA2;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public float scalingFactor;

        public ItemAffixAlarm(InputAutomation inputAutomation, CancellationToken cancellationToken)
        {
            this.inputAutomation = inputAutomation;
            this.cancellation = cancellationToken;
            this.scalingFactor = GetDpiScalingFactor();
        }

        private float GetDpiScalingFactor()
        {
            var source = System.Windows.PresentationSource.FromVisual(Application.Current.MainWindow);
            if (source != null)
            {
                var dpiX = 96 * (float)source.CompositionTarget.TransformToDevice.M11;
                return dpiX / 96.0f; // 96 DPI is the standard
            }
            return 1.0f; // Default scaling factor
        }

        public void Process(MediaPlayer mediaPlayer, OverlayWindow overlayWindow, System.Windows.Controls.Label label)
        {
            this.mediaPlayer = mediaPlayer;
            this.overlayWindow = overlayWindow;
            ItemAffixParser parser = new ItemAffixParser();
            Console.Write(parser.items);
            ClearClipboard();

            string previousClipboardText = string.Empty;

            List<double> executionTimes = new List<double>();
            const int numExecutionsToCalc = 20;

            while (!cancellation.IsCancellationRequested)
            {
                if (IsLeftCtrlPressed())
                {
                    Task.Delay(100).Wait();
                    continue;
                }
                string currentClipboardText = string.Empty;

                currentClipboardText = GetClipboardText();

                if (currentClipboardText != previousClipboardText)
                {
                    previousClipboardText = currentClipboardText;

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var result = parser.ParseString(currentClipboardText);
                    stopwatch.Stop();
                    long executionTimeInTicks = stopwatch.ElapsedTicks;
                    double executionTimeInMilliseconds = (double)executionTimeInTicks / Stopwatch.Frequency * 1000000;

                    // Add the new execution time
                    if (executionTimes.Count >= numExecutionsToCalc)
                    {
                        executionTimes.RemoveAt(0); // Remove the oldest entry
                    }
                    executionTimes.Add(executionTimeInMilliseconds); // Add the new execution time

                    // Calculate the average execution time
                    double averageExecutionTime = (double)executionTimes.Average();

                    if (result.Count > 0)
                    {
                        PlayAlertSound();
                        POINT cursorPos;
                        GetCursorPos(out cursorPos);
                        System.Windows.Point point = new System.Windows.Point(cursorPos.X / scalingFactor, cursorPos.Y / scalingFactor);
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            this.overlayWindow.ShowOverlay(string.Join("\n", result), point);
                        }, System.Windows.Threading.DispatcherPriority.Send);
                    }

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        label.Content = String.Format("Average processing: {0:F2} µs", averageExecutionTime);
                    });

                }

                inputAutomation.LeftAltCopy();

                Task.Delay(250).Wait();
            }
        }

        private void PlayAlertSound()
        {
            mediaPlayer.Stop();
            mediaPlayer.Play();
        }

        private bool IsLeftCtrlPressed()
        {
            return (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0; // Check if the high-order bit is set
        }

        public static string GetClipboardText()
        {
            string result = string.Empty;

            if (OpenClipboard(IntPtr.Zero))
            {
                IntPtr hClipboardData = GetClipboardData(CF_TEXT);
                if (hClipboardData != IntPtr.Zero)
                {
                    IntPtr pData = GlobalLock(hClipboardData);
                    if (pData != IntPtr.Zero)
                    {
                        int size = GlobalSize(hClipboardData);
                        byte[] buffer = new byte[size];
                        Marshal.Copy(pData, buffer, 0, size);
                        GlobalUnlock(hClipboardData);

                        // Convert the byte array to a string
                        result = System.Text.Encoding.ASCII.GetString(buffer);
                    }
                }
                CloseClipboard();
            }

            return result;
        }

        public static void ClearClipboard()
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                CloseClipboard();
            }
        }
    }
}
