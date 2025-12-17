using System.Runtime.InteropServices;
using BrightnessControl.Models;
using Timer = System.Windows.Forms.Timer;

namespace BrightnessControl
{
    public class MainForm : Form
    {
        private readonly ComboBox monitorComboBox;
        private readonly TrackBar brightnessTrackBar;
        private readonly Label brightnessLabel;
        private readonly Button refreshButton;
        private readonly Timer debounceTimer;

        private const int HOTKEY_ID = 1;
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        public MainForm()
        {
            Text = "External Monitor Brightness Control";
            Width = 450;
            Height = 250;
            MinimumSize = new Size(400, 250);

            // Use TableLayoutPanel for better layout management
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 4,
                ColumnCount = 1,
                AutoSize = true
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Label
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // TrackBar (take available space)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Button
            Controls.Add(layout);

            // Monitor Selector
            monitorComboBox = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            layout.Controls.Add(monitorComboBox, 0, 0);

            // Brightness Label
            brightnessLabel = new Label { Dock = DockStyle.Top, Text = "Brightness: --%", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
            layout.Controls.Add(brightnessLabel, 0, 1);

            // Brightness TrackBar
            brightnessTrackBar = new TrackBar { Dock = DockStyle.Top, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            brightnessTrackBar.Scroll += (s, e) => RequestBrightnessUpdate();
            layout.Controls.Add(brightnessTrackBar, 0, 2);

            // Refresh Button
            refreshButton = new Button { Dock = DockStyle.Top, Text = "Refresh Monitors", Height = 40, AutoSize = true };
            refreshButton.Click += (s, e) => RefreshMonitors();
            layout.Controls.Add(refreshButton, 0, 3);

            // Debounce Timer setup
            debounceTimer = new Timer { Interval = 150 }; // 150ms debounce
            debounceTimer.Tick += async (s, e) => await ApplyBrightnessChange();

            RefreshMonitors();
            RegisterHotKeys();
        }

        private void RequestBrightnessUpdate()
        {
            brightnessLabel.Text = $"Brightness: {brightnessTrackBar.Value}%";
            // Restart timer to debounce
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private async Task ApplyBrightnessChange()
        {
            debounceTimer.Stop();

            if (monitorComboBox.SelectedItem is not DisplayMonitor selectedMonitor) return;

            try
            {
                // Disable UI slightly or just show loading? Maybe overkill for brightness.
                // We'll just run it. If it fails, we show a tooltip or status.
                int brightness = brightnessTrackBar.Value;
                await selectedMonitor.SetBrightnessAsync(brightness);
            }
            catch (Exception ex)
            {
                // In a real app, maybe a StatusStrip is better than a MessageBox for this
                MessageBox.Show($"Failed to change brightness: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshMonitors()
        {
            monitorComboBox.Items.Clear();
            try
            {
                var monitors = DisplayMonitor.GetMonitors();
                if (monitors.Length > 0)
                {
                    monitorComboBox.Items.AddRange(monitors);
                    monitorComboBox.SelectedIndex = 0;
                    brightnessTrackBar.Enabled = true;
                }
                else
                {
                    monitorComboBox.Items.Add("No compatible monitors found");
                    monitorComboBox.SelectedIndex = 0;
                    brightnessTrackBar.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing monitors: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegisterHotKeys()
        {
            try
            {
                RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Keys.Up);
                RegisterHotKey(Handle, HOTKEY_ID + 1, MOD_CONTROL | MOD_SHIFT, (int)Keys.Down);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not register hotkeys: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                // Execute async on UI thread fire-and-forget style for hotkeys (or properly await if possible, but WndProc is void)
                // We'll helper method
                if (id == HOTKEY_ID) ChangeBrightnessStep(5);
                else if (id == HOTKEY_ID + 1) ChangeBrightnessStep(-5);
            }
            base.WndProc(ref m);
        }

        private void ChangeBrightnessStep(int step)
        {
            int newValue = Math.Clamp(brightnessTrackBar.Value + step, brightnessTrackBar.Minimum, brightnessTrackBar.Maximum);
            if (newValue != brightnessTrackBar.Value)
            {
                brightnessTrackBar.Value = newValue;
                RequestBrightnessUpdate();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
    }
}
