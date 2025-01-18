using Label = System.Windows.Forms.Label;
using Application = System.Windows.Forms.Application;
using System.Runtime.InteropServices;

namespace BrightnessControl
{
    public class MainForm : Form
    {
        private readonly ComboBox monitorComboBox;
        private readonly TrackBar brightnessTrackBar;
        private readonly Label brightnessLabel;
        private readonly Button refreshButton;

        private const int HOTKEY_ID = 1;
        private const int WM_HOTKEY = 0x0312;

        public MainForm()
        {
            Text = "External Monitor Brightness Control";
            Width = 400;
            Height = 220;

            monitorComboBox = new ComboBox { Left = 20, Top = 20, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(monitorComboBox);

            brightnessLabel = new Label { Left = 20, Top = 60, Width = 340, Text = "Brightness: 0%" };
            Controls.Add(brightnessLabel);

            brightnessTrackBar = new TrackBar { Left = 20, Top = 90, Width = 340, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            brightnessTrackBar.Scroll += (s, e) => UpdateBrightness();
            Controls.Add(brightnessTrackBar);

            refreshButton = new Button { Left = 20, Top = 135, Width = 340, Text = "Refresh Monitors" };
            refreshButton.Click += (s, e) => RefreshMonitors();
            Controls.Add(refreshButton);

            RefreshMonitors();
            RegisterHotKey();
        }

        private void UpdateBrightness()
        {
            if (monitorComboBox.SelectedItem is MonitorInfo selectedMonitor)
            {
                int brightness = brightnessTrackBar.Value;
                selectedMonitor.SetBrightness(brightness);
                brightnessLabel.Text = $"Brightness: {brightness}%";
            }
        }

        private void RefreshMonitors()
        {
            monitorComboBox.Items.Clear();

            var monitors = MonitorInfo.GetMonitors();
            if (monitors.Length is not 0)
            {
                monitorComboBox.Items.AddRange(monitors);
                monitorComboBox.SelectedIndex = 0;
            }
            else
            {
                monitorComboBox.Items.Add("No monitors detected");
                monitorComboBox.SelectedIndex = 0;
            }
        }

        private void RegisterHotKey()
        {
            // Register Ctrl + Shift + Up Arrow to increase brightness
            RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (int)Keys.Up);
            // Register Ctrl + Shift + Down Arrow to decrease brightness
            RegisterHotKey(Handle, HOTKEY_ID + 1, MOD_CONTROL | MOD_SHIFT, (int)Keys.Down);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID)
                {
                    // Increase brightness
                    ChangeBrightness(5);
                }
                else if (id == HOTKEY_ID + 1)
                {
                    // Decrease brightness
                    ChangeBrightness(-5);
                }
            }
            base.WndProc(ref m);
        }

        private void ChangeBrightness(int change)
        {
            if (monitorComboBox.SelectedItem is MonitorInfo)
            {
                int newBrightness = brightnessTrackBar.Value + change;
                newBrightness = Math.Max(brightnessTrackBar.Minimum, Math.Min(brightnessTrackBar.Maximum, newBrightness));
                brightnessTrackBar.Value = newBrightness;
                UpdateBrightness();
            }
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
    }

    public class MonitorInfo
    {
        public required string Name { get; set; }
        public required string Path { get; set; }

        public override string ToString() => Name;

        public static MonitorInfo[] GetMonitors()
        {
            var monitors = new List<MonitorInfo>();

            bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                uint physicalMonitorCount = 0;
                if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref physicalMonitorCount))
                {
                    var physicalMonitors = new PHYSICAL_MONITOR[physicalMonitorCount];
                    if (GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, physicalMonitors))
                    {
                        foreach (var physicalMonitor in physicalMonitors)
                        {
                            monitors.Add(new MonitorInfo
                            {
                                Name = physicalMonitor.szPhysicalMonitorDescription,
                                Path = physicalMonitor.hPhysicalMonitor.ToString()
                            });
                        }
                    }
                }
                return true;
            }

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

            return [.. monitors];
        }

        public void SetBrightness(int brightness)
        {
            var hMonitor = new IntPtr(long.Parse(Path));
            if (!SetVCPFeature(hMonitor, 0x10, (uint)brightness))
            {
                throw new InvalidOperationException("Failed to set monitor brightness.");
            }
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
