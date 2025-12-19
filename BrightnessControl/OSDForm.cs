using Timer = System.Windows.Forms.Timer;

namespace BrightnessControl
{
    public class OSDForm : Form
    {
        private readonly ProgressBar progressBar;
        private readonly Label label;
        private readonly Timer hideTimer;

        public OSDForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Width = 300;
            Height = 60;
            BackColor = Color.Black;
            Opacity = 0.8;
            TopMost = true;
            ShowInTaskbar = false;

            // Make it click-through (optional, but good for OSD)
            // For simplicity in WinForms, we just ignore it.

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 2,
                ColumnCount = 1
            };
            Controls.Add(layout);

            label = new Label
            {
                Text = "Brightness: 50%",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            layout.Controls.Add(label, 0, 0);

            progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Dock = DockStyle.Fill,
                Height = 10
            };
            layout.Controls.Add(progressBar, 0, 1);

            hideTimer = new Timer { Interval = 1500 }; // 1.5 seconds
            hideTimer.Tick += (s, e) => HideOSD();
        }

        public void ShowBrightness(int value)
        {
            value = Math.Clamp(value, 0, 100);
            label.Text = $"Brightness: {value}%";
            progressBar.Value = value;
            
            // Re-center just in case resolution changed (optional)
            CenterToScreen();
            
            // Reset opacity and timer
            Opacity = 0.8;
            hideTimer.Stop();
            hideTimer.Start();
            
            // Show without stealing focus
            ShowInactiveTopmost();
        }

        private void HideOSD()
        {
            hideTimer.Stop();
            Hide();
        }

        private void ShowInactiveTopmost()
        {
            const int SW_SHOWNOACTIVATE = 4;
            const int HWND_TOPMOST = -1;
            const uint SWP_NOACTIVATE = 0x0010;
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOSIZE = 0x0001;

            SetWindowPos(Handle, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE);
            ShowWindow(Handle, SW_SHOWNOACTIVATE);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
