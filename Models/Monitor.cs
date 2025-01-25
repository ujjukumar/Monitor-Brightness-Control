using System.Runtime.InteropServices;

namespace BrightnessControl.Models
{
    public class Monitor
    {
        public required string Name { get; set; }
        public required string Path { get; set; }

        public override string ToString() => Name;

        public static Monitor[] GetMonitors()
        {
            var monitors = new List<Monitor>();

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
                            monitors.Add(new Monitor
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
