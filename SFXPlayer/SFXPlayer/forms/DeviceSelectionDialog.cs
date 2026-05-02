using System;
using System.Drawing;
using System.Windows.Forms;

namespace SFXPlayer.forms
{
    public partial class DeviceSelectionDialog : Form
    {
        public string SelectedPlaybackDevice { get; private set; }
        public string SelectedPreviewDevice { get; private set; }
        public string SelectedMidiDevice { get; private set; }

        private Label lblPlayback;
        private ComboBox cbPlaybackDevice;
        private Label lblPreview;
        private ComboBox cbPreviewDevice;
        private Label lblMidi;
        private ComboBox cbMidiDevice;
        private Button btnOK;
        private Button btnCancel;

        public DeviceSelectionDialog(
            string[] playbackDevices,
            string[] previewDevices,
            string[] midiDevices,
            string currentPlayback,
            string currentPreview,
            string currentMidi)
        {
            InitializeComponent();

            // Populate combo boxes
            if (playbackDevices != null && playbackDevices.Length > 0)
            {
                cbPlaybackDevice.Items.AddRange(playbackDevices);
            }
            
            if (previewDevices != null && previewDevices.Length > 0)
            {
                cbPreviewDevice.Items.AddRange(previewDevices);
            }
            
            if (midiDevices != null && midiDevices.Length > 0)
            {
                cbMidiDevice.Items.AddRange(midiDevices);
            }

            // Set current selections
            if (!string.IsNullOrEmpty(currentPlayback) && cbPlaybackDevice.Items.Contains(currentPlayback))
            {
                cbPlaybackDevice.SelectedItem = currentPlayback;
            }
            else if (cbPlaybackDevice.Items.Count > 0)
            {
                cbPlaybackDevice.SelectedIndex = 0;
            }

            if (!string.IsNullOrEmpty(currentPreview) && cbPreviewDevice.Items.Contains(currentPreview))
            {
                cbPreviewDevice.SelectedItem = currentPreview;
            }
            else if (cbPreviewDevice.Items.Count > 0)
            {
                cbPreviewDevice.SelectedIndex = 0;
            }

            if (!string.IsNullOrEmpty(currentMidi) && cbMidiDevice.Items.Contains(currentMidi))
            {
                cbMidiDevice.SelectedItem = currentMidi;
            }
            else if (cbMidiDevice.Items.Count > 0)
            {
                cbMidiDevice.SelectedIndex = 0;
            }

            // Store initial selections
            SelectedPlaybackDevice = currentPlayback;
            SelectedPreviewDevice = currentPreview;
            SelectedMidiDevice = currentMidi;
        }

        private void InitializeComponent()
        {
            this.lblPlayback = new Label();
            this.cbPlaybackDevice = new ComboBox();
            this.lblPreview = new Label();
            this.cbPreviewDevice = new ComboBox();
            this.lblMidi = new Label();
            this.cbMidiDevice = new ComboBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.SuspendLayout();
            // 
            // lblPlayback
            // 
            this.lblPlayback.AutoSize = true;
            this.lblPlayback.Location = new Point(12, 15);
            this.lblPlayback.Name = "lblPlayback";
            this.lblPlayback.Size = new Size(100, 13);
            this.lblPlayback.TabIndex = 0;
            this.lblPlayback.Text = "Playback Device:";
            // 
            // cbPlaybackDevice
            // 
            this.cbPlaybackDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbPlaybackDevice.FormattingEnabled = true;
            this.cbPlaybackDevice.Location = new Point(120, 12);
            this.cbPlaybackDevice.Name = "cbPlaybackDevice";
            this.cbPlaybackDevice.Size = new Size(350, 21);
            this.cbPlaybackDevice.TabIndex = 1;
            // 
            // lblPreview
            // 
            this.lblPreview.AutoSize = true;
            this.lblPreview.Location = new Point(12, 45);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Size = new Size(90, 13);
            this.lblPreview.TabIndex = 2;
            this.lblPreview.Text = "Preview Device:";
            // 
            // cbPreviewDevice
            // 
            this.cbPreviewDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbPreviewDevice.FormattingEnabled = true;
            this.cbPreviewDevice.Location = new Point(120, 42);
            this.cbPreviewDevice.Name = "cbPreviewDevice";
            this.cbPreviewDevice.Size = new Size(350, 21);
            this.cbPreviewDevice.TabIndex = 3;
            // 
            // lblMidi
            // 
            this.lblMidi.AutoSize = true;
            this.lblMidi.Location = new Point(12, 75);
            this.lblMidi.Name = "lblMidi";
            this.lblMidi.Size = new Size(75, 13);
            this.lblMidi.TabIndex = 4;
            this.lblMidi.Text = "MIDI Device:";
            // 
            // cbMidiDevice
            // 
            this.cbMidiDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbMidiDevice.FormattingEnabled = true;
            this.cbMidiDevice.Location = new Point(120, 72);
            this.cbMidiDevice.Name = "cbMidiDevice";
            this.cbMidiDevice.Size = new Size(350, 21);
            this.cbMidiDevice.TabIndex = 5;
            // 
            // btnOK
            // 
            this.btnOK.Location = new Point(314, 110);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 23);
            this.btnOK.TabIndex = 6;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(395, 110);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // DeviceSelectionDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(484, 145);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbMidiDevice);
            this.Controls.Add(this.lblMidi);
            this.Controls.Add(this.cbPreviewDevice);
            this.Controls.Add(this.lblPreview);
            this.Controls.Add(this.cbPlaybackDevice);
            this.Controls.Add(this.lblPlayback);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DeviceSelectionDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Audio and MIDI Devices";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedPlaybackDevice = cbPlaybackDevice.SelectedItem?.ToString();
            SelectedPreviewDevice = cbPreviewDevice.SelectedItem?.ToString();
            SelectedMidiDevice = cbMidiDevice.SelectedItem?.ToString();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}