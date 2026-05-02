namespace SFXPlayer
{
    partial class TimeStamper
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.Length = new System.Windows.Forms.Label();
            this.volDisplayLabel = new System.Windows.Forms.Label();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.lblVolumeHeader = new System.Windows.Forms.Label();
            this.volSlider = new System.Windows.Forms.TrackBar();
            this.lblVolumeValue = new System.Windows.Forms.Label();
            this.lblSpeedHeader = new System.Windows.Forms.Label();
            this.speedSlider = new System.Windows.Forms.TrackBar();
            this.lblSpeedValue = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.volSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.speedSlider)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(760, 100);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.DoubleClick += new System.EventHandler(this.pictureBox1_DoubleClick);
            this.pictureBox1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox1_MouseDown);
            this.pictureBox1.SizeChanged += new System.EventHandler(this.pictureBox1_SizeChanged);
            // 
            // Length
            // 
            this.Length = new System.Windows.Forms.Label();
            this.Length.AutoSize = true;
            this.Length.Location = new System.Drawing.Point(12, 125);
            this.Length.Name = "Length";
            this.Length.Size = new System.Drawing.Size(60, 13);
            this.Length.TabIndex = 1;
            this.Length.Text = "00:00.00";
            // 
            // volDisplayLabel
            // 
            this.volDisplayLabel = new System.Windows.Forms.Label();
            this.volDisplayLabel.AutoSize = true;
            this.volDisplayLabel.Location = new System.Drawing.Point(100, 125);
            this.volDisplayLabel.Name = "volDisplayLabel";
            this.volDisplayLabel.Size = new System.Drawing.Size(60, 13);
            this.volDisplayLabel.TabIndex = 2;
            this.volDisplayLabel.Text = "Volume: 50";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 150);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(200, 160);
            this.listBox1.TabIndex = 3;
            this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            this.listBox1.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
            // 
            // trackBar1
            // 
            this.trackBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar1.Location = new System.Drawing.Point(12, 118);
            this.trackBar1.Maximum = 1000;
            this.trackBar1.Minimum = 0;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(760, 45);
            this.trackBar1.TabIndex = 4;
            this.trackBar1.TickFrequency = 10;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // lblVolumeHeader
            // 
            this.lblVolumeHeader.AutoSize = true;
            this.lblVolumeHeader.Location = new System.Drawing.Point(222, 153);
            this.lblVolumeHeader.Name = "lblVolumeHeader";
            this.lblVolumeHeader.TabIndex = 5;
            this.lblVolumeHeader.Text = "Volume:";
            // 
            // volSlider
            // 
            this.volSlider.Location = new System.Drawing.Point(222, 170);
            this.volSlider.Maximum = 100;
            this.volSlider.Minimum = 0;
            this.volSlider.Name = "volSlider";
            this.volSlider.Size = new System.Drawing.Size(510, 45);
            this.volSlider.TabIndex = 6;
            this.volSlider.TickFrequency = 10;
            this.volSlider.Value = 50;
            this.volSlider.Scroll += new System.EventHandler(this.volSlider_Scroll);
            // 
            // lblVolumeValue
            // 
            this.lblVolumeValue.AutoSize = true;
            this.lblVolumeValue.Location = new System.Drawing.Point(740, 178);
            this.lblVolumeValue.Name = "lblVolumeValue";
            this.lblVolumeValue.TabIndex = 7;
            this.lblVolumeValue.Text = "50";
            // 
            // lblSpeedHeader
            // 
            this.lblSpeedHeader.AutoSize = true;
            this.lblSpeedHeader.Location = new System.Drawing.Point(222, 222);
            this.lblSpeedHeader.Name = "lblSpeedHeader";
            this.lblSpeedHeader.TabIndex = 8;
            this.lblSpeedHeader.Text = "Speed:";
            // 
            // speedSlider
            // 
            this.speedSlider.Location = new System.Drawing.Point(222, 240);
            this.speedSlider.Maximum = 200;
            this.speedSlider.Minimum = 10;
            this.speedSlider.Name = "speedSlider";
            this.speedSlider.Size = new System.Drawing.Size(510, 45);
            this.speedSlider.TabIndex = 9;
            this.speedSlider.TickFrequency = 10;
            this.speedSlider.Value = 100;
            this.speedSlider.Scroll += new System.EventHandler(this.speedSlider_Scroll);
            // 
            // lblSpeedValue
            // 
            this.lblSpeedValue.AutoSize = true;
            this.lblSpeedValue.Location = new System.Drawing.Point(740, 248);
            this.lblSpeedValue.Name = "lblSpeedValue";
            this.lblSpeedValue.TabIndex = 10;
            this.lblSpeedValue.Text = "1.00x";
            // 
            // TimeStamper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 380);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.volDisplayLabel);
            this.Controls.Add(this.Length);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblVolumeHeader);
            this.Controls.Add(this.volSlider);
            this.Controls.Add(this.lblVolumeValue);
            this.Controls.Add(this.lblSpeedHeader);
            this.Controls.Add(this.speedSlider);
            this.Controls.Add(this.lblSpeedValue);
            this.Name = "TimeStamper";
            this.Text = "Time Stamper";
            this.Load += new System.EventHandler(this.TimeStamper_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.volSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.speedSlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label Length;
        private System.Windows.Forms.Label volDisplayLabel;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label lblVolumeHeader;
        private System.Windows.Forms.TrackBar volSlider;
        private System.Windows.Forms.Label lblVolumeValue;
        private System.Windows.Forms.Label lblSpeedHeader;
        private System.Windows.Forms.TrackBar speedSlider;
        private System.Windows.Forms.Label lblSpeedValue;
    }
}
