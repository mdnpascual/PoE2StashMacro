using System;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PoE2StashMacro
{
    internal class ItemAffixAlarm
    {
        private MouseAutomation mouseAutomation;
        private CancellationToken cancellation;
        private MediaPlayer mediaPlayer;
        private OverlayWindow overlayWindow;

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

        public ItemAffixAlarm(OverlayWindow overlayWindow, MouseAutomation mouseAutomation, CancellationToken cancellationToken)
        {
            this.overlayWindow = overlayWindow;
            this.mouseAutomation = mouseAutomation;
            this.cancellation = cancellationToken;
            InitializeMediaPlayer();
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

        private void InitializeMediaPlayer()
        {
            mediaPlayer = new MediaPlayer();

            // Get the current assembly
            var assembly = Assembly.GetExecutingAssembly();

            // Specify the resource name (update with your actual project namespace)
            string resourceName = "PoE2StashMacro.noot.mp3"; 

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    // Create a temporary file
                    string tempFilePath = Path.Combine(Path.GetTempPath(), "noot.mp3");

                    // Check if the file already exists and delete it
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }

                    using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream); // Copy the stream to the temporary file
                    }

                    // Open the temporary file in the MediaPlayer
                    mediaPlayer.Open(new Uri(tempFilePath, UriKind.Absolute));
                }
                else
                {
                    MessageBox.Show("Sound file not found.");
                }
            }
        }

        public void Process(System.Windows.Controls.Label label)
        {
            ItemAffixParser parser = new ItemAffixParser();
            Console.Write(parser.items);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Clipboard.Clear();
            });

            string previousClipboardText = string.Empty;

            List<double> executionTimes = new List<double>();
            const int numExecutionsToCalc = 20;

            while (!cancellation.IsCancellationRequested)
            {
                if (IsLeftCtrlPressed())
                {
                    mouseAutomation.Sleep(250);
                    continue;
                }
                string currentClipboardText = string.Empty;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    currentClipboardText = Clipboard.GetText();
                });

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

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        label.Content = String.Format("Average processing: {0:F2} µs", averageExecutionTime);

                        if (result.Count > 0)
                        {
                            PlayAlertSound();
                            POINT cursorPos;
                            GetCursorPos(out cursorPos);
                            System.Windows.Point point = new System.Windows.Point(cursorPos.X / scalingFactor, cursorPos.Y / scalingFactor);
                            overlayWindow.ShowOverlay(string.Join("\n", result), point);
                        }

                    });

                }

                mouseAutomation.PressKeyAsync(Keys.C, Keys.LControlKey, Keys.LMenu).Wait();

                mouseAutomation.Sleep(250);
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
    }
}
