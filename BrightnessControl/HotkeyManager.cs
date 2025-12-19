using System.Runtime.InteropServices;
using BrightnessControl.Core.Settings;

namespace BrightnessControl
{
    public class HotkeyManager : IDisposable
    {
        private readonly IntPtr hWnd;

        public event Action<int>? HotkeyPressed;

        public const int IncreaseId = 1;
        public const int DecreaseId = 2;

        public HotkeyManager(IntPtr handle)
        {
            hWnd = handle;
        }

        public void RegisterHotkeys(AppSettings settings)
        {
            UnregisterHotkeys();

            try
            {
                RegisterHotKey(hWnd, IncreaseId, settings.IncreaseBrightnessModifiers, settings.IncreaseBrightnessKey);
                RegisterHotKey(hWnd, DecreaseId, settings.DecreaseBrightnessModifiers, settings.DecreaseBrightnessKey);
            }
            catch
            {
                // Key binding might fail if taken by another app
            }
        }

        public void UnregisterHotkeys()
        {
            UnregisterHotKey(hWnd, IncreaseId);
            UnregisterHotKey(hWnd, DecreaseId);
        }

        public bool ProcessMessage(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                HotkeyPressed?.Invoke(id);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            UnregisterHotkeys();
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
