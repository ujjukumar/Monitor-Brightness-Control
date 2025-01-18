using Label = System.Windows.Forms.Label;
using Application = System.Windows.Forms.Application;
using System.Runtime.InteropServices;

namespace BrightnessControl
{
    public class MainForm : Form
    {
        private ComboBox monitorComboBox;
        private TrackBar brightnessTrackBar;
        private Label brightnessLabel;
        private Button refreshButton;

        public MainForm()
        {
            Text = "External Monitor Brightness Control";
            Width = 400;
            Height = 200;

            monitorComboBox = new ComboBox { Left = 20, Top = 20, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(monitorComboBox);

            brightnessLabel = new Label { Left = 20, Top = 60, Width = 340, Text = "Brightness: 0%" };
            Controls.Add(brightnessLabel);

            brightnessTrackBar = new TrackBar { Left = 20, Top = 90, Width = 340, Minimum = 0, Maximum = 100, TickFrequency = 10 };
            brightnessTrackBar.Scroll += (s, e) => UpdateBrightness();
            Controls.Add(brightnessTrackBar);

            refreshButton = new Button { Left = 20, Top = 130, Width = 340, Text = "Refresh Monitors" };
            refreshButton.Click += (s, e) => RefreshMonitors();
            Controls.Add(refreshButton);

            RefreshMonitors();
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
            if (monitors.Any())
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

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
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

            return monitors.ToArray();
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
