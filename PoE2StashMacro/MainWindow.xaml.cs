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
using System.Windows.Controls;
using CheckBox = System.Windows.Controls.CheckBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;

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
        private CancellationTokenSource stashPusherCancellationToken;

        private DisengageReverse disengageReverse;
        private Thread disengageReverseThread;
        private CancellationTokenSource disengageReverseCancellationToken;

        private ItemAffixAlarm itemAffixAlarm;
        private Thread itemAffixAlarmThread;
        private CancellationTokenSource itemAffixAlarmCancellationToken;
        
        private bool isListening = false;
        private KeyboardHook keyboardHook;
        private List<Screen> screens;
        private DispatcherTimer timer;
        private Brush originalBackground;

        private MouseAutomation mouseAutomation;
        private OverlayWindow overlayWindow;

        private Keys disengageKey = Keys.E;
        private Keys startItemAffixKey = Keys.C;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_LCONTROL = 0xA2;

        public MainWindow()
        {
            InitializeComponent();

            overlayWindow = new OverlayWindow();
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

            mouseAutomation = new MouseAutomation(screens[MonitorComboBox.SelectedIndex]);
            keyboardHook = new KeyboardHook(mouseAutomation);
            keyboardHook.KeyUp += KeyboardHook_KeyUp;

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
            stashPusherCancellationToken = new CancellationTokenSource();
            stashPusher = new StashPusher(resolution, isQuad, isMapTab, mouseAutomation, stashPusherCancellationToken.Token, screens[MonitorComboBox.SelectedIndex]);

            stashPusherThread = new Thread(() =>
            {
                stashPusher.Process(cursorPos, MousePosLbl);
            });

            stashPusherThread.Start();
        }

        private void StartDisengageReverse(string resolution, Point cursorPos)
        {
            if (disengageReverse == null)
            {
                disengageReverseCancellationToken = new CancellationTokenSource();
                disengageReverse = new DisengageReverse(resolution, mouseAutomation, disengageReverseCancellationToken.Token, screens[MonitorComboBox.SelectedIndex], disengageKey);
            }            

            disengageReverseThread = new Thread(() =>
            {
                disengageReverse.Process(cursorPos, MousePosLbl);
            });

            disengageReverseThread.Start();
        }

        private void StartItemAffixAlarm()
        {
            if (itemAffixAlarmCancellationToken != null)
            {
                itemAffixAlarmCancellationToken.Cancel();
                itemAffixAlarmThread?.Join(); // Wait for the thread to finish
            }

            itemAffixAlarmCancellationToken = new CancellationTokenSource();
            itemAffixAlarm = new ItemAffixAlarm(mouseAutomation, itemAffixAlarmCancellationToken.Token);

            itemAffixAlarmThread = new Thread(() =>
            {
                MediaPlayer mediaPlayer = InitializeMediaPlayer();
                itemAffixAlarm.Process(mediaPlayer, overlayWindow, MousePosLbl);
            });

            itemAffixAlarmThread.Start();
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
                if (DisengageSkill.IsChecked == true)
                {
                    keyboardHook.AddKeyToSuppress(disengageKey);
                }
                if (AffixScanner.IsChecked == true)
                {
                    keyboardHook.AddKeyToSuppress(startItemAffixKey);
                }
            }
            else
            {
                keyboardHook.ClearKeyToSuppress();
                keyboardHook.UnhookKeyboard(); // Stop listening to global keyboard events
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Get the checkbox that triggered the event
            CheckBox checkedCheckBox = sender as CheckBox;

            // If a checkbox is checked, uncheck all others
            if (checkedCheckBox != null && checkedCheckBox.IsChecked == true)
            {
                if(checkedCheckBox.Name == "DisengageSkill")
                {
                    keyboardHook.ClearKeyToSuppress();
                    keyboardHook.AddKeyToSuppress(disengageKey);
                }
                else if (checkedCheckBox.Name == "AffixScanner")
                {
                    keyboardHook.ClearKeyToSuppress();
                    keyboardHook.AddKeyToSuppress(startItemAffixKey);
                }

                // Uncheck all other checkboxes
                foreach (var child in ((StackPanel)checkedCheckBox.Parent).Children)
                {
                    if (child is CheckBox checkBox && checkBox != checkedCheckBox)
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
        }

        private void KeyboardHook_KeyUp(Keys key)
        {
            if (key == disengageKey && DisengageSkill.IsChecked == true && !mouseAutomation.IsProgrammaticKeyPress())
            {
                int selectedIndex = MonitorComboBox.SelectedIndex;

                // Get mouse positions
                var (absoluteX, absoluteY, relativeX, relativeY) = mousePositionHandler.GetMousePositions(selectedIndex);

                // Update the label with absolute and relative positions
                MousePosLbl.Content = $"Mouse Position (Absolute): X={absoluteX}, Y={absoluteY}\n" +
                                      $"Mouse Position (Relative to Screen {selectedIndex + 1}): X={relativeX}, Y={relativeY}";

                CheckMousePositionInMonitor(new Point(absoluteX, absoluteY), screens[selectedIndex]);

                // Create DisengageReverse instance
                StartDisengageReverse(
                    screens[selectedIndex].Bounds.Width + "x" + screens[selectedIndex].Bounds.Height,
                    new Point(relativeX, relativeY)
                );
            }
            if (key == Keys.E && (IsQuadCheckBox.IsChecked == true || IsMapTab.IsChecked == true))
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
            if (key == Keys.C && AffixScanner.IsChecked == true && !mouseAutomation.IsProgrammaticKeyPress())
            {
                StartItemAffixAlarm();
            }
            else if (key == Keys.X)
            {
                if (stashPusherThread != null && stashPusherThread.IsAlive)
                {
                    stashPusherCancellationToken.Cancel();
                }
                if (disengageReverseThread != null && disengageReverseThread.IsAlive)
                {
                    disengageReverseCancellationToken.Cancel();
                }
                if (itemAffixAlarmThread != null && itemAffixAlarmThread.IsAlive)
                {
                    itemAffixAlarmCancellationToken.Cancel();
                }
            }
            else if (key == Keys.Q && DisengageSkill.IsChecked != true && !mouseAutomation.IsProgrammaticKeyPress())
            {
                if (DisengageSkill.IsChecked == true)
                {
                    IsQuadCheckBox.IsChecked = !IsQuadCheckBox.IsChecked;
                    IsMapTab.IsChecked = false;
                }
            }
            else if (key == Keys.M && DisengageSkill.IsChecked != true && !mouseAutomation.IsProgrammaticKeyPress())
            {
                IsQuadCheckBox.IsChecked = false;
                IsMapTab.IsChecked = !IsMapTab.IsChecked;
            }

            // SWITCHING
            if (key == Keys.E && AffixScanner.IsChecked == true && !mouseAutomation.IsProgrammaticKeyPress() && IsLeftCtrlPressed())
            {
                // Switch to Disengage
                // Uncheck all other checkboxes
                foreach (var child in checkBoxPanel.Children)
                {
                    if (child is CheckBox checkBox)
                    {
                        if (checkBox.Name != "DisengageSkill")
                        {
                            checkBox.IsChecked = false;
                        }
                        else
                        {
                            checkBox.IsChecked = true;
                        }
                    } 
                }

                keyboardHook.ClearKeyToSuppress();
                keyboardHook.AddKeyToSuppress(disengageKey);
            }

            if (key == Keys.C && DisengageSkill.IsChecked == true && !mouseAutomation.IsProgrammaticKeyPress() && IsLeftCtrlPressed())
            {
                // Switch to Affix Scanner
                // Uncheck all other checkboxes
                foreach (var child in checkBoxPanel.Children)
                {
                    if (child is CheckBox checkBox)
                    {
                        if (checkBox.Name != "AffixScanner")
                        {
                            checkBox.IsChecked = false;
                        }
                        else
                        {
                            checkBox.IsChecked = true;
                        }
                    }
                }

                keyboardHook.ClearKeyToSuppress();
                keyboardHook.AddKeyToSuppress(startItemAffixKey);
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

            overlayWindow.HideOverlay();
            overlayWindow.Close();

            keyboardHook.UnhookKeyboard();
            if (stashPusherThread != null && stashPusherThread.IsAlive) { stashPusherCancellationToken.Cancel(); }
            base.OnClosing(e);
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected item
            var selectedItem = MonitorComboBox.SelectedIndex;

            // Perform an action based on the selected item
            if (selectedItem > -1)
            {
                mouseAutomation = new MouseAutomation(screens[selectedItem]);
            }
        }
        private bool IsLeftCtrlPressed()
        {
            return (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0; // Check if the high-order bit is set
        }

        private MediaPlayer InitializeMediaPlayer()
        {
            MediaPlayer mediaPlayer = new MediaPlayer();

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

            return mediaPlayer;
        }
    }
}
