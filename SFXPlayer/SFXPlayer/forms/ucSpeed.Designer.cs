namespace SFXPlayer
{
    partial class ucSpeed
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.tbSpeed = new System.Windows.Forms.TrackBar();
            this.lbSpeed = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tbSpeed)).BeginInit();
            this.SuspendLayout();
            // 
            // tbSpeed
            // 
            this.tbSpeed.Location = new System.Drawing.Point(0, 0);
            this.tbSpeed.Minimum = 10;
            this.tbSpeed.Maximum = 200;
            this.tbSpeed.Value = 100;
            this.tbSpeed.Name = "tbSpeed";
            this.tbSpeed.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tbSpeed.Size = new System.Drawing.Size(45, 130);
            this.tbSpeed.TabIndex = 0;
            this.tbSpeed.TickFrequency = 10;
            this.tbSpeed.Scroll += new System.EventHandler(this.tbSpeed_Scroll);
            this.tbSpeed.Enter += new System.EventHandler(this.tbSpeed_Enter);
            this.tbSpeed.Leave += new System.EventHandler(this.tbSpeed_Leave);
            // 
            // lbSpeed
            // 
            this.lbSpeed.Location = new System.Drawing.Point(0, 130);
            this.lbSpeed.Name = "lbSpeed";
            this.lbSpeed.Size = new System.Drawing.Size(47, 20);
            this.lbSpeed.TabIndex = 1;
            this.lbSpeed.Text = "1.00x";
            this.lbSpeed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lbSpeed.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5f);
            // 
            // ucSpeed
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.tbSpeed);
            this.Controls.Add(this.lbSpeed);
            this.Name = "ucSpeed";
            this.Size = new System.Drawing.Size(47, 151);
            ((System.ComponentModel.ISupportInitialize)(this.tbSpeed)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TrackBar tbSpeed;
        private System.Windows.Forms.Label lbSpeed;
    }
}
