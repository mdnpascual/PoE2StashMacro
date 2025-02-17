using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Interop;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows.Media;
using System.Linq;

using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using System.Threading;

namespace PoE2StashMacro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppSettings appSettings;
        private MousePositionHandler mousePositionHandler;

        private StashPusher stashPusher;
        private Thread stashPusherThread;
        private CancellationTokenSource cancellationTokenSource;

        private bool isListening = false;
        private KeyboardHook keyboardHook;
        private List<Screen> screens;
        private DispatcherTimer timer;
        private Brush originalBackground;

        public MainWindow()
        {
            InitializeComponent();
            keyboardHook = new KeyboardHook();
            keyboardHook.KeyUp += KeyboardHook_KeyUp;

            // Initialize settings
            appSettings = new AppSettings();
            appSettings.LoadSettings();

            // Get all connected monitors and populate the ComboBox
            screens = new List<Screen>(Screen.AllScreens);
            mousePositionHandler = new MousePositionHandler(screens);
            PopulateMonitorComboBox();

            // Check for screen changes
            if (!MonitorNamesMatch())
            {
                MessageBox.Show("Screens changed, the dropdown selection has been cleared.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                MonitorComboBox.SelectedIndex = -1; // Clear selection
            }
            else
            {
                MonitorComboBox.SelectedIndex = appSettings.SelectedMonitorIndex >= 0 ? appSettings.SelectedMonitorIndex : 0;
            }

            // Set the checkbox state
            IsQuadCheckBox.IsChecked = appSettings.IsQuad;

            // Initialize background timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            originalBackground = this.Background;
        }

        private void StartStashPusher(string resolution, bool isQuad, bool isMapTab, Point cursorPos)
        {
            MouseAutomation mouseAutomation = new MouseAutomation(screens[MonitorComboBox.SelectedIndex]);
            cancellationTokenSource = new CancellationTokenSource();
            stashPusher = new StashPusher(resolution, isQuad, isMapTab, mouseAutomation, cancellationTokenSource.Token, screens[MonitorComboBox.SelectedIndex]);

            stashPusherThread = new Thread(() =>
            {
                stashPusher.Process(cursorPos, MousePosLbl);
            });

            stashPusherThread.Start();
        }

        private bool MonitorNamesMatch()
        {
            var currentMonitorNames = new List<string>();
            foreach (var screen in screens)
            {
                currentMonitorNames.Add(screen.DeviceName);
            }
            return currentMonitorNames.SequenceEqual(appSettings.MonitorNames);
        }

        private void PopulateMonitorComboBox()
        {
            MonitorComboBox.Items.Clear();
            for (int i = 0; i < screens.Count; i++)
            {
                var screen = screens[i];
                var monitorInfo = $"{i + 1}: {screen.DeviceName} ({screen.Bounds.Width}x{screen.Bounds.Height})";
                MonitorComboBox.Items.Add(monitorInfo);
            }

            // Save current monitor names for future reference
            appSettings.MonitorNames = screens.Select(s => s.DeviceName).ToArray();

            if (MonitorComboBox.Items.Count == 0)
                MonitorComboBox.Items.Add("Choose monitor to track"); // Default entry
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            isListening = !isListening; // Toggle the listening state
            StartBtn.Content = isListening ? "Stop Listening" : "Start Listening";

            if (isListening)
            {
                keyboardHook.HookKeyboard(); // Start listening to global keyboard events
            }
            else
            {
                keyboardHook.UnhookKeyboard(); // Stop listening to global keyboard events
            }
        }

        private void KeyboardHook_KeyUp(Keys key)
        {
            if (key == Keys.E)
            {
                if (MonitorComboBox.SelectedItem != null)
                {
                    int selectedIndex = MonitorComboBox.SelectedIndex;

                    // Get mouse positions
                    var (absoluteX, absoluteY, relativeX, relativeY) = mousePositionHandler.GetMousePositions(selectedIndex);

                    // Update the label with absolute and relative positions
                    MousePosLbl.Content = $"Mouse Position (Absolute): X={absoluteX}, Y={absoluteY}\n" +
                                          $"Mouse Position (Relative to Screen {selectedIndex + 1}): X={relativeX}, Y={relativeY}";

                    CheckMousePositionInMonitor(new Point(absoluteX, absoluteY), screens[selectedIndex]);

                    // Create StashPusher instance
                    StartStashPusher(
                        screens[selectedIndex].Bounds.Width + "x" + screens[selectedIndex].Bounds.Height,
                        IsQuadCheckBox.IsChecked ?? false,
                        IsMapTab.IsChecked ?? false,
                        new Point(relativeX, relativeY));
                }   
            }
            else if (key == Keys.X)
            {
                if (stashPusherThread != null && stashPusherThread.IsAlive)
                {
                    cancellationTokenSource.Cancel();
                }
            }
            else if (key == Keys.Q)
            {
                IsQuadCheckBox.IsChecked = !IsQuadCheckBox.IsChecked;
                IsMapTab.IsChecked = false;
            }
            else if (key == Keys.M)
            {
                IsQuadCheckBox.IsChecked = false;
                IsMapTab.IsChecked = !IsMapTab.IsChecked;
            }
        }

        private void CheckMousePositionInMonitor(Point mousePosition, Screen selectedScreen)
        {
            if (selectedScreen.Bounds.Contains(mousePosition))
            {
                ChangeBackgroundColor();
            }
        }

        private void ChangeBackgroundColor()
        {
            this.Background = System.Windows.Media.Brushes.Green; // Change background to green
            timer.Start(); // Start the timer to revert the color
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop(); // Stop the timer
            this.Background = originalBackground; // Revert to the original background color
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Save selected monitor index
            appSettings.SelectedMonitorIndex = MonitorComboBox.SelectedIndex;
            appSettings.IsQuad = IsQuadCheckBox.IsChecked ?? false; // Save checkbox state
            appSettings.SaveSettings();

            keyboardHook.UnhookKeyboard();
            if (stashPusherThread != null && stashPusherThread.IsAlive) { cancellationTokenSource.Cancel(); }
            base.OnClosing(e);
        }
    }
}
