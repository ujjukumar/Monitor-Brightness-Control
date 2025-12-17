using System.Runtime.InteropServices;

namespace BrightnessControl.Models
{
    public class DisplayMonitor
    {
        public required string Name { get; set; }
        public required string Path { get; set; }

        private const byte VCP_LUMINANCE = 0x10;

        public override string ToString() => Name;

        public static DisplayMonitor[] GetMonitors()
        {
            var monitors = new List<DisplayMonitor>();

            bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                uint physicalMonitorCount = 0;
                try
                {
                    if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref physicalMonitorCount))
                    {
                        var physicalMonitors = new PHYSICAL_MONITOR[physicalMonitorCount];
                        if (GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorCount, physicalMonitors))
                        {
                            foreach (var physicalMonitor in physicalMonitors)
                            {
                                monitors.Add(new DisplayMonitor
                                {
                                    Name = physicalMonitor.szPhysicalMonitorDescription,
                                    Path = physicalMonitor.hPhysicalMonitor.ToString()
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors during enumeration to ensure at least some monitors might be found
                }
                return true;
            }

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);

            return [.. monitors];
        }

        public async Task SetBrightnessAsync(int brightness)
        {
            await Task.Run(() => SetBrightness(brightness));
        }

        public void SetBrightness(int brightness)
        {
            try
            {
                var hMonitor = new IntPtr(long.Parse(Path));
                if (!SetVCPFeature(hMonitor, VCP_LUMINANCE, (uint)brightness))
                {
                    // If setting fails, we throw to let the caller handle it (e.g. show a message or log)
                    // But we wrap it in a clearer exception
                    throw new IOException($"Failed to set brightness on monitor '{Name}'. The monitor might not support DDC/CI or the driver may be unresponsive.");
                }
            }
            catch (Exception ex) when (ex is not IOException)
            {
                throw new IOException($"Error communicating with monitor '{Name}': {ex.Message}", ex);
            }
        }

        // --- P/Invoke Definitions ---

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
