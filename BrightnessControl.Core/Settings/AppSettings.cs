namespace BrightnessControl.Core.Settings
{
    public class AppSettings
    {
        public bool IsFirstRun { get; set; } = true;

        // Modifiers: None=0, Alt=1, Control=2, Shift=4, Win=8
        public uint IncreaseBrightnessModifiers { get; set; } = 2 | 4; // Ctrl + Shift
        public int IncreaseBrightnessKey { get; set; } = 38; // Up Arrow

        public uint DecreaseBrightnessModifiers { get; set; } = 2 | 4; // Ctrl + Shift
        public int DecreaseBrightnessKey { get; set; } = 40; // Down Arrow
    }
}
