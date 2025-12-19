using System.Runtime.InteropServices;
using BrightnessControl.Core.Models;
using BrightnessControl.Core.Settings;
using Timer = System.Windows.Forms.Timer;

namespace BrightnessControl
{
    public class MainForm : Form
    {
        private readonly ComboBox monitorComboBox;
        private readonly TrackBar brightnessTrackBar;
        private readonly Label brightnessLabel;
        private readonly Timer debounceTimer;
        private readonly MenuStrip menuStrip;

        // System Tray Components
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip trayMenu;
        private bool isExiting = false;

        // Managers & Forms
        private readonly AppSettings settings;
        private readonly HotkeyManager hotkeyManager;
        private OSDForm? osdForm;

        public MainForm()
        {
            Text = "External Monitor Brightness Control";
            Width = 450;
            Height = 250;
            MinimumSize = new Size(400, 250);
            Icon = SystemIcons.Application;

            // Load Settings & Hotkeys
            settings = SettingsManager.Load();
            hotkeyManager = new HotkeyManager(Handle);
            hotkeyManager.HotkeyPressed += OnHotkeyPressed;

            // --- Menu Strip Setup ---
            menuStrip = new MenuStrip { Dock = DockStyle.Top }; // Explicitly Dock Top
            
            // File Menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => ExitApplication());
            menuStrip.Items.Add(fileMenu);

            // Options Menu
            var optionsMenu = new ToolStripMenuItem("&Options");
            optionsMenu.DropDownItems.Add("&Refresh Monitors", null, (s, e) => RefreshMonitors());
            optionsMenu.DropDownItems.Add("&Settings / Hotkeys", null, (s, e) => OpenSettings());
            menuStrip.Items.Add(optionsMenu);

            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
            
            // --- System Tray Setup ---
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Monitor Brightness Control",
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // Layout
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1,
                // Removed AutoSize = true to rely on Dock=Fill logic completely
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Label
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // TrackBar
            Controls.Add(layout);
            
            // IMPORTANT: Bring layout to front to ensure it docks LAST, 
            // after the menu strip (at the back) has claimed the top space.
            layout.BringToFront();

            // Controls
            monitorComboBox = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            layout.Controls.Add(monitorComboBox, 0, 0);

            brightnessLabel = new Label { Dock = DockStyle.Top, Text = "Brightness: --%", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
            layout.Controls.Add(brightnessLabel, 0, 1);

            brightnessTrackBar = new TrackBar { Dock = DockStyle.Top, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            brightnessTrackBar.Scroll += (s, e) => RequestBrightnessUpdate();
            layout.Controls.Add(brightnessTrackBar, 0, 2);

            // Timer
            debounceTimer = new Timer { Interval = 150 };
            debounceTimer.Tick += async (s, e) => await ApplyBrightnessChange();

            RefreshMonitors();
            hotkeyManager.RegisterHotkeys(settings);
        }

        private void OnHotkeyPressed(int id)
        {
            if (id == HotkeyManager.IncreaseId) ChangeBrightnessStep(5);
            else if (id == HotkeyManager.DecreaseId) ChangeBrightnessStep(-5);
        }

        private void OpenSettings()
        {
            using (var form = new SettingsForm(settings))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    hotkeyManager.RegisterHotkeys(settings);
                }
            }
        }

        private void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            isExiting = true;
            notifyIcon.Visible = false;
            hotkeyManager.Dispose();
            osdForm?.Close();
            Application.Exit();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized) Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!isExiting)
            {
                e.Cancel = true;
                Hide();
                notifyIcon.ShowBalloonTip(1000, "Minimized", "Running in tray.", ToolTipIcon.Info);
            }
            base.OnFormClosing(e);
        }

        private void RequestBrightnessUpdate()
        {
            brightnessLabel.Text = $"Brightness: {brightnessTrackBar.Value}%";
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private async Task ApplyBrightnessChange()
        {
            debounceTimer.Stop();
            if (monitorComboBox.SelectedItem is not DisplayMonitor selectedMonitor) return;

            try
            {
                int brightness = brightnessTrackBar.Value;
                await selectedMonitor.SetBrightnessAsync(brightness);
            }
            catch (Exception ex)
            {
                if (Visible)
                    MessageBox.Show($"Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show($"Error refreshing: {ex.Message}", "Error");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (hotkeyManager != null && hotkeyManager.ProcessMessage(ref m))
                return;
                
            base.WndProc(ref m);
        }

        private void ChangeBrightnessStep(int step)
        {
            int newValue = Math.Clamp(brightnessTrackBar.Value + step, brightnessTrackBar.Minimum, brightnessTrackBar.Maximum);
            if (newValue != brightnessTrackBar.Value)
            {
                brightnessTrackBar.Value = newValue;
                RequestBrightnessUpdate();
                ShowOSD(newValue);
            }
        }

        private void ShowOSD(int brightness)
        {
            if (osdForm == null || osdForm.IsDisposed)
            {
                osdForm = new OSDForm();
            }
            osdForm.ShowBrightness(brightness);
        }
    }
}
