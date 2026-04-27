namespace SFXPlayer
{
    partial class PlayStrip {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            bnStopAll = new System.Windows.Forms.CheckBox();
            bnFile = new System.Windows.Forms.PictureBox();
            timer1 = new System.Windows.Forms.Timer(components);
            tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            lbIndex = new System.Windows.Forms.Label();
            tbDescription = new System.Windows.Forms.TextBox();
            bnPlay = new System.Windows.Forms.PictureBox();
            bnVolume = new System.Windows.Forms.PictureBox();
            bnPreview = new System.Windows.Forms.PictureBox();
            bnEdit = new System.Windows.Forms.PictureBox();
            Delete = new System.Windows.Forms.ContextMenuStrip(components);
            deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pnlAutoPlay = new System.Windows.Forms.FlowLayoutPanel();
            cbAutoPlay = new System.Windows.Forms.CheckBox();
            lblPauseMs = new System.Windows.Forms.Label();
            nudPauseMs = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)bnFile).BeginInit();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)bnPlay).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bnVolume).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bnPreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)bnEdit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPauseMs).BeginInit();
            pnlAutoPlay.SuspendLayout();
            SuspendLayout();
            // 
            // bnStopAll
            // 
            bnStopAll.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            bnStopAll.Location = new System.Drawing.Point(518, 12);
            bnStopAll.Margin = new System.Windows.Forms.Padding(10, 12, 10, 12);
            bnStopAll.Name = "bnStopAll";
            bnStopAll.Size = new System.Drawing.Size(23, 28);
            bnStopAll.TabIndex = 4;
            toolTip1.SetToolTip(bnStopAll, "Stops All Other Sounds");
            bnStopAll.UseVisualStyleBackColor = true;
            // 
            // bnFile
            // 
            bnFile.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            bnFile.Image = Properties.Resources.SoundFile2_18;
            bnFile.Location = new System.Drawing.Point(378, 6);
            bnFile.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bnFile.Name = "bnFile";
            bnFile.Size = new System.Drawing.Size(35, 40);
            bnFile.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            bnFile.TabIndex = 5;
            bnFile.TabStop = false;
            toolTip1.SetToolTip(bnFile, "File");
            bnFile.Click += bnFile_Click;
            // 
            // timer1
            // 
            timer1.Interval = 400;
            timer1.Tick += timer1_Tick;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel1, 0, 0);
            tableLayoutPanel2.Controls.Add(pnlAutoPlay, 0, 1);
            tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
            tableLayoutPanel2.Size = new System.Drawing.Size(643, 90);
            tableLayoutPanel2.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 8;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            tableLayoutPanel1.Controls.Add(lbIndex, 0, 0);
            tableLayoutPanel1.Controls.Add(tbDescription, 1, 0);
            tableLayoutPanel1.Controls.Add(bnPlay, 4, 0);
            tableLayoutPanel1.Controls.Add(bnStopAll, 5, 0);
            tableLayoutPanel1.Controls.Add(bnFile, 2, 0);
            tableLayoutPanel1.Controls.Add(bnVolume, 7, 0);
            tableLayoutPanel1.Controls.Add(bnPreview, 3, 0);
            tableLayoutPanel1.Controls.Add(bnEdit, 6, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            tableLayoutPanel1.Size = new System.Drawing.Size(643, 52);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // lbIndex
            // 
            lbIndex.AutoSize = true;
            lbIndex.Dock = System.Windows.Forms.DockStyle.Fill;
            lbIndex.Location = new System.Drawing.Point(5, 0);
            lbIndex.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            lbIndex.Name = "lbIndex";
            lbIndex.Size = new System.Drawing.Size(42, 52);
            lbIndex.TabIndex = 0;
            lbIndex.Text = "000";
            lbIndex.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tbDescription
            // 
            tbDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            tbDescription.Location = new System.Drawing.Point(57, 6);
            tbDescription.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            tbDescription.Name = "tbDescription";
            tbDescription.Size = new System.Drawing.Size(311, 31);
            tbDescription.TabIndex = 1;
            // 
            // bnPlay
            // 
            bnPlay.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            bnPlay.Image = Properties.Resources.Play2_18;
            bnPlay.Location = new System.Drawing.Point(468, 6);
            bnPlay.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bnPlay.Name = "bnPlay";
            bnPlay.Size = new System.Drawing.Size(33, 38);
            bnPlay.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            bnPlay.TabIndex = 2;
            bnPlay.TabStop = false;
            bnPlay.Click += bnPlay_Click;
            // 
            // bnVolume
            // 
            bnVolume.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            bnVolume.Location = new System.Drawing.Point(603, 6);
            bnVolume.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bnVolume.Name = "bnVolume";
            bnVolume.Size = new System.Drawing.Size(33, 38);
            bnVolume.TabIndex = 6;
            bnVolume.TabStop = false;
            bnVolume.Text = "Volume";
            bnVolume.Click += bnVolume_Click;
            // 
            // bnPreview
            // 
            bnPreview.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            bnPreview.Image = Properties.Resources.Headphones2_18;
            bnPreview.Location = new System.Drawing.Point(423, 6);
            bnPreview.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bnPreview.Name = "bnPreview";
            bnPreview.Size = new System.Drawing.Size(35, 40);
            bnPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            bnPreview.TabIndex = 7;
            bnPreview.TabStop = false;
            bnPreview.Click += bnPreview_Click;
            // 
            // bnEdit
            // 
            bnEdit.Location = new System.Drawing.Point(558, 6);
            bnEdit.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            bnEdit.Name = "bnEdit";
            bnEdit.Size = new System.Drawing.Size(33, 38);
            bnEdit.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            bnEdit.TabIndex = 8;
            bnEdit.TabStop = false;
            bnEdit.Click += bnEdit_Click;
            // 
            // Delete
            // 
            Delete.ImageScalingSize = new System.Drawing.Size(24, 24);
            Delete.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { deleteToolStripMenuItem });
            Delete.Name = "Delete";
            Delete.Size = new System.Drawing.Size(201, 48);
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
            deleteToolStripMenuItem.Text = "Delete Cue";
            deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // pnlAutoPlay
            // 
            pnlAutoPlay.Controls.Add(cbAutoPlay);
            pnlAutoPlay.Controls.Add(lblPauseMs);
            pnlAutoPlay.Controls.Add(nudPauseMs);
            pnlAutoPlay.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlAutoPlay.Location = new System.Drawing.Point(0, 52);
            pnlAutoPlay.Margin = new System.Windows.Forms.Padding(0);
            pnlAutoPlay.Name = "pnlAutoPlay";
            pnlAutoPlay.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            pnlAutoPlay.Size = new System.Drawing.Size(643, 38);
            pnlAutoPlay.TabIndex = 2;
            pnlAutoPlay.WrapContents = false;
            // 
            // cbAutoPlay
            // 
            cbAutoPlay.AutoSize = true;
            cbAutoPlay.Dock = System.Windows.Forms.DockStyle.None;
            cbAutoPlay.Location = new System.Drawing.Point(8, 9);
            cbAutoPlay.Margin = new System.Windows.Forms.Padding(3, 9, 8, 9);
            cbAutoPlay.Name = "cbAutoPlay";
            cbAutoPlay.Size = new System.Drawing.Size(150, 19);
            cbAutoPlay.TabIndex = 9;
            cbAutoPlay.Text = "Auto-run to next cue";
            cbAutoPlay.UseVisualStyleBackColor = true;
            cbAutoPlay.CheckedChanged += new System.EventHandler(this.cbAutoPlay_CheckedChanged);
            // 
            // lblPauseMs
            // 
            lblPauseMs.AutoSize = true;
            lblPauseMs.Dock = System.Windows.Forms.DockStyle.None;
            lblPauseMs.Location = new System.Drawing.Point(166, 11);
            lblPauseMs.Margin = new System.Windows.Forms.Padding(3, 11, 3, 9);
            lblPauseMs.Name = "lblPauseMs";
            lblPauseMs.Size = new System.Drawing.Size(62, 15);
            lblPauseMs.TabIndex = 10;
            lblPauseMs.Text = "Pause (ms):";
            // 
            // nudPauseMs
            // 
            nudPauseMs.Location = new System.Drawing.Point(234, 8);
            nudPauseMs.Margin = new System.Windows.Forms.Padding(3, 8, 3, 8);
            nudPauseMs.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            nudPauseMs.Name = "nudPauseMs";
            nudPauseMs.Size = new System.Drawing.Size(80, 23);
            nudPauseMs.TabIndex = 11;
            nudPauseMs.ValueChanged += new System.EventHandler(this.nudPauseMs_ValueChanged);
            // 
            // PlayStrip
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            ContextMenuStrip = Delete;
            Controls.Add(tableLayoutPanel2);
            DoubleBuffered = true;
            Margin = new System.Windows.Forms.Padding(0);
            Name = "PlayStrip";
            Size = new System.Drawing.Size(643, 90);
            Load += PlayStrip_Load;
            MouseDown += MouseDownHandler;
            Resize += PlayStrip_Resize;
            ((System.ComponentModel.ISupportInitialize)bnFile).EndInit();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)bnPlay).EndInit();
            ((System.ComponentModel.ISupportInitialize)bnVolume).EndInit();
            ((System.ComponentModel.ISupportInitialize)bnPreview).EndInit();
            ((System.ComponentModel.ISupportInitialize)bnEdit).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPauseMs).EndInit();
            pnlAutoPlay.ResumeLayout(false);
            pnlAutoPlay.PerformLayout();
            ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label lbIndex;
        private System.Windows.Forms.TextBox tbDescription;
        private System.Windows.Forms.PictureBox bnPlay;
        private System.Windows.Forms.CheckBox bnStopAll;
        private System.Windows.Forms.PictureBox bnFile;
        private System.Windows.Forms.PictureBox bnVolume;
        private System.Windows.Forms.PictureBox bnPreview;
        private System.Windows.Forms.PictureBox bnEdit;
        private System.Windows.Forms.ContextMenuStrip Delete;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.FlowLayoutPanel pnlAutoPlay;
        private System.Windows.Forms.CheckBox cbAutoPlay;
        private System.Windows.Forms.Label lblPauseMs;
        private System.Windows.Forms.NumericUpDown nudPauseMs;
    }
}
