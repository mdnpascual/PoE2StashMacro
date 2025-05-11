using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace PoE2StashMacro
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private DispatcherTimer hideTimer;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public OverlayWindow()
        {
            InitializeComponent();
            this.Hide(); // Start hidden
            InitializeTimer();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void InitializeTimer()
        {
            hideTimer = new DispatcherTimer();
            hideTimer.Interval = TimeSpan.FromSeconds(5);
            hideTimer.Tick += HideTimer_Tick;
        }
        private void HideTimer_Tick(object sender, EventArgs e)
        {
            HideOverlay();
        }

        public void ShowOverlay(string text, Point position)
        {
            OverlayText.Text = text;
            this.Left = position.X;
            this.Top = position.Y + 20;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.Show();

            hideTimer.Stop();
            hideTimer.Start();
        }

        public void HideOverlay()
        {
            this.Hide();
            hideTimer.Stop();
        }

        private void OverlayWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HideOverlay();
        }

        private void OverlayWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            // Get the current height of the overlay window
            double currentHeight = this.ActualHeight;

            // Calculate the new position to move the overlay up or down
            if (Mouse.GetPosition(this).Y < currentHeight / 2) // If the mouse is in the upper half
            {
                this.Top += currentHeight; // Move down
            }
            else // If the mouse is in the lower half
            {
                this.Top -= currentHeight; // Move up
            }
        }
    }
}
