using System;
using System.Windows.Forms;

namespace SFXPlayer
{
    public partial class ucSpeed : UserControl
    {
        public event EventHandler SpeedChanged;
        public event EventHandler Done;

        public ucSpeed()
        {
            InitializeComponent();
        }

        /// <summary>Gets or sets the speed as a float (0.10 to 2.00).</summary>
        public float Speed
        {
            get { return tbSpeed.Value / 100f; }
            set
            {
                int clamped = Math.Max(tbSpeed.Minimum, Math.Min(tbSpeed.Maximum, (int)Math.Round(value * 100)));
                tbSpeed.Value = clamped;
                UpdateLabel();
            }
        }

        private void UpdateLabel()
        {
            lbSpeed.Text = (tbSpeed.Value / 100f).ToString("0.00") + "x";
        }

        private void tbSpeed_Scroll(object sender, EventArgs e)
        {
            UpdateLabel();
            SpeedChanged?.Invoke(this, e);
        }

        private void tbSpeed_Leave(object sender, EventArgs e)
        {
            Done?.Invoke(this, e);
        }

        private void tbSpeed_Enter(object sender, EventArgs e)
        {
            if (TopLevelControl is SFXPlayer mainForm)
                mainForm.ScrollTimer.Enabled = true;
        }
    }
}
