using BrightnessControl.Core.Settings;

namespace BrightnessControl
{
    public class SettingsForm : Form
    {
        private readonly AppSettings settings;
        private readonly TextBox txtIncrease;
        private readonly TextBox txtDecrease;
        private readonly Button btnSave;
        private readonly Button btnCancel;

        private uint tempIncreaseMods;
        private int tempIncreaseKey;
        private uint tempDecreaseMods;
        private int tempDecreaseKey;

        public SettingsForm(AppSettings currentSettings)
        {
            settings = currentSettings;
            Text = "Settings";
            Width = 350;
            Height = 250;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            // Initialize temp vars with current settings
            tempIncreaseMods = settings.IncreaseBrightnessModifiers;
            tempIncreaseKey = settings.IncreaseBrightnessKey;
            tempDecreaseMods = settings.DecreaseBrightnessModifiers;
            tempDecreaseKey = settings.DecreaseBrightnessKey;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 5,
                ColumnCount = 2,
                AutoSize = true
            };
            Controls.Add(layout);

            // Instructions
            var instructions = new Label { Text = "Click in the box and press your hotkey:", AutoSize = true };
            layout.Controls.Add(instructions, 0, 0);
            layout.SetColumnSpan(instructions, 2);

            // Increase
            layout.Controls.Add(new Label { Text = "Increase Brightness:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            txtIncrease = new TextBox { ReadOnly = true, Width = 150 };
            txtIncrease.KeyDown += (s, e) => CaptureHotkey(e, ref tempIncreaseMods, ref tempIncreaseKey, txtIncrease);
            UpdateTextBox(txtIncrease, tempIncreaseMods, tempIncreaseKey);
            layout.Controls.Add(txtIncrease, 1, 1);

            // Decrease
            layout.Controls.Add(new Label { Text = "Decrease Brightness:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
            txtDecrease = new TextBox { ReadOnly = true, Width = 150 };
            txtDecrease.KeyDown += (s, e) => CaptureHotkey(e, ref tempDecreaseMods, ref tempDecreaseKey, txtDecrease);
            UpdateTextBox(txtDecrease, tempDecreaseMods, tempDecreaseKey);
            layout.Controls.Add(txtDecrease, 1, 2);

            // Buttons
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Height = 40 };
            btnCancel = new Button { Text = "Cancel" };
            btnCancel.Click += (s, e) => Close();
            
            btnSave = new Button { Text = "Save" };
            btnSave.Click += SaveAndClose;

            btnPanel.Controls.Add(btnCancel);
            btnPanel.Controls.Add(btnSave);
            layout.Controls.Add(btnPanel, 0, 4);
            layout.SetColumnSpan(btnPanel, 2);
        }

        private void CaptureHotkey(KeyEventArgs e, ref uint mods, ref int key, TextBox textBox)
        {
            e.SuppressKeyPress = true; // Don't type
            
            // Ignore single modifier presses
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin)
                return;

            uint currentMods = 0;
            if (e.Alt) currentMods |= 1;
            if (e.Control) currentMods |= 2;
            if (e.Shift) currentMods |= 4;
            // Win key logic is tricky in WinForms KeyDown without hooks, sticking to Std modifiers for now

            mods = currentMods;
            key = (int)e.KeyCode;

            UpdateTextBox(textBox, mods, key);
        }

        private void UpdateTextBox(TextBox txt, uint mods, int key)
        {
            var parts = new List<string>();
            if ((mods & 2) != 0) parts.Add("Ctrl");
            if ((mods & 4) != 0) parts.Add("Shift");
            if ((mods & 1) != 0) parts.Add("Alt");
            parts.Add(((Keys)key).ToString());

            txt.Text = string.Join(" + ", parts);
        }

        private void SaveAndClose(object? sender, EventArgs e)
        {
            settings.IncreaseBrightnessModifiers = tempIncreaseMods;
            settings.IncreaseBrightnessKey = tempIncreaseKey;
            settings.DecreaseBrightnessModifiers = tempDecreaseMods;
            settings.DecreaseBrightnessKey = tempDecreaseKey;

            SettingsManager.Save(settings);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
