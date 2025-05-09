using System;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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

            while (!cancellation.IsCancellationRequested)
            {
                string currentClipboardText = string.Empty;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    currentClipboardText = Clipboard.GetText();
                });

                if (currentClipboardText != previousClipboardText)
                {
                    previousClipboardText = currentClipboardText;

                    var result = parser.ParseString(currentClipboardText);
                    if (result.Count > 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            PlayAlertSound();
                            POINT cursorPos;
                            GetCursorPos(out cursorPos);
                            System.Windows.Point point = new System.Windows.Point(cursorPos.X / scalingFactor, cursorPos.Y / scalingFactor);
                            overlayWindow.ShowOverlay(string.Join("\n", result), point);
                        });
                    } else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            label.Content = "";
                        });
                    }
                    
                }

                mouseAutomation.PressKeyAsync(Keys.C, Keys.LControlKey, Keys.LMenu).Wait();

                mouseAutomation.Sleep(100);
            }
        }

        private void PlayAlertSound()
        {
            mediaPlayer.Stop();
            mediaPlayer.Play();
        }
    }
}
