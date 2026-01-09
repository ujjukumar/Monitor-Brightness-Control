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
        private readonly CheckBox syncAllCheckBox;

        // System Tray Components
        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip trayMenu;
        private bool isExiting = false;

        // Managers & Forms
        private readonly AppSettings settings;
        private readonly HotkeyManager hotkeyManager;
        private OSDForm? osdForm;

        private readonly List<DisplayMonitor> activeMonitors = new();

        public MainForm()
        {
            Text = "External Monitor Brightness Control";
            Width = 450;
            Height = 300; // Increased height for checkbox
            MinimumSize = new Size(400, 300);
            
            if (System.IO.File.Exists("icon.ico"))
            {
                try
                {
                    Icon = new Icon("icon.ico");
                }
                catch
                {
                    Icon = SystemIcons.Application;
                }
            }
            else
            {
                Icon = SystemIcons.Application;
            }

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
                Icon = this.Icon,
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
                RowCount = 4, // Added row for CheckBox
                ColumnCount = 1,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // ComboBox
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // CheckBox
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Label
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // TrackBar
            Controls.Add(layout);
            
            // IMPORTANT: Bring layout to front to ensure it docks LAST
            layout.BringToFront();

            // Controls
            monitorComboBox = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            layout.Controls.Add(monitorComboBox, 0, 0);

            syncAllCheckBox = new CheckBox { Text = "Sync All Monitors", Dock = DockStyle.Top, AutoSize = true };
            syncAllCheckBox.CheckedChanged += (s, e) => {
                monitorComboBox.Enabled = !syncAllCheckBox.Checked;
            };
            layout.Controls.Add(syncAllCheckBox, 0, 1);

            brightnessLabel = new Label { Dock = DockStyle.Top, Text = "Brightness: --%", TextAlign = ContentAlignment.MiddleCenter, AutoSize = true };
            layout.Controls.Add(brightnessLabel, 0, 2);

            brightnessTrackBar = new TrackBar { Dock = DockStyle.Top, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            brightnessTrackBar.Scroll += (s, e) => RequestBrightnessUpdate();
            layout.Controls.Add(brightnessTrackBar, 0, 3);

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
            
            foreach (var monitor in activeMonitors)
            {
                monitor.Dispose();
            }
            activeMonitors.Clear();
            
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
            int brightness = brightnessTrackBar.Value;

            if (syncAllCheckBox.Checked)
            {
                var tasks = activeMonitors.Select(m => m.SetBrightnessAsync(brightness));
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                     System.Diagnostics.Debug.WriteLine($"Partial failure: {ex.Message}");
                }
            }
            else if (monitorComboBox.SelectedItem is DisplayMonitor selectedMonitor)
            {
                try
                {
                    await selectedMonitor.SetBrightnessAsync(brightness);
                }
                catch (Exception ex)
                {
                    // Silent fail or minimal logging to avoid spam
                    System.Diagnostics.Debug.WriteLine($"Failed: {ex.Message}");
                }
            }
        }

        private void RefreshMonitors()
        {
            // Dispose old monitors
            foreach (var m in activeMonitors) m.Dispose();
            activeMonitors.Clear();
            monitorComboBox.Items.Clear();

            try
            {
                var monitors = DisplayMonitor.GetMonitors();
                if (monitors.Length > 0)
                {
                    activeMonitors.AddRange(monitors);
                    monitorComboBox.Items.AddRange(monitors);
                    monitorComboBox.SelectedIndex = 0;
                    
                    brightnessTrackBar.Enabled = true;
                    
                    // Only show/enable sync if multiple monitors
                    if (monitors.Length > 1)
                    {
                        syncAllCheckBox.Enabled = true;
                        syncAllCheckBox.Visible = true;
                    }
                    else
                    {
                        syncAllCheckBox.Enabled = false;
                        syncAllCheckBox.Visible = false;
                        syncAllCheckBox.Checked = false;
                    }
                }
                else
                {
                    monitorComboBox.Items.Add("No compatible monitors found");
                    monitorComboBox.SelectedIndex = 0;
                    brightnessTrackBar.Enabled = false;
                    syncAllCheckBox.Enabled = false;
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
            
            // Always update if value changes or if we want to force show OSD
            brightnessTrackBar.Value = newValue;
            RequestBrightnessUpdate(); // This triggers the timer which calls ApplyBrightnessChange
            ShowOSD(newValue);
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
