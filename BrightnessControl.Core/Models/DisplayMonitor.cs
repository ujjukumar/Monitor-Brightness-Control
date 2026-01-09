using System.Runtime.InteropServices;

namespace BrightnessControl.Core.Models
{
    public class DisplayMonitor : IDisposable
    {
        public string DeviceName { get; }
        public string DeviceString { get; }
        public IntPtr Handle { get; private set; }

        private const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;
        private bool disposedValue;

        public DisplayMonitor(string deviceName, string deviceString, IntPtr handle)
        {
            DeviceName = deviceName;
            DeviceString = deviceString;
            Handle = handle;
        }

        public override string ToString()
        {
            return $"{DeviceString} ({DeviceName})";
        }

        public async Task SetBrightnessAsync(int brightness)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(DisplayMonitor));

            await Task.Run(() =>
            {
                if (!SetVCPFeature(Handle, 0x10, (uint)brightness))
                {
                    // Optionally log, but don't crash if one fails (e.g. monitor turned off)
                    // throw new InvalidOperationException("Failed to set monitor brightness."); 
                    System.Diagnostics.Debug.WriteLine($"Failed to set brightness for {DeviceString}");
                }
            });
        }

        public static DisplayMonitor[] GetMonitors()
        {
            var monitors = new List<DisplayMonitor>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
            {
                uint monitorCount;
                if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out monitorCount))
                {
                    var physicalMonitors = new PHYSICAL_MONITOR[monitorCount];
                    if (GetPhysicalMonitorsFromHMONITOR(hMonitor, monitorCount, physicalMonitors))
                    {
                        foreach (var monitor in physicalMonitors)
                        {
                            var info = new MONITORINFOEX();
                            info.cbSize = Marshal.SizeOf(info);
                            GetMonitorInfo(hMonitor, ref info);

                            monitors.Add(new DisplayMonitor(
                                info.szDevice,
                                new string(monitor.szPhysicalMonitorDescription).TrimEnd('\0'),
                                monitor.hPhysicalMonitor
                            ));
                        }
                        // We DO NOT destroy them here. The DisplayMonitor instance now owns the handle 
                        // and is responsible for calling DestroyPhysicalMonitors in Dispose().
                    }
                }
                return true;
            }, IntPtr.Zero);

            return monitors.ToArray();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (Handle != IntPtr.Zero)
                {
                    // DestroyPhysicalMonitors expects an array
                    var array = new PHYSICAL_MONITOR[] { new PHYSICAL_MONITOR { hPhysicalMonitor = Handle } };
                    DestroyPhysicalMonitors(1, array);
                    Handle = IntPtr.Zero;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~DisplayMonitor()
        {
            Dispose(disposing: false);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public char[] szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
    }
}
