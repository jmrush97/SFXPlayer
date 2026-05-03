using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SFXPlayer.classes;
using SFXPlayer.Properties;

namespace SFXPlayer
{
    partial class SFXPlayer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SFXPlayer));
            this.CueList = new System.Windows.Forms.TableLayoutPanel();
            this.dlgOpenAudioFile = new System.Windows.Forms.OpenFileDialog();
            this.rtMainText = new System.Windows.Forms.RichTextBox();
            this.bnStopAll = new System.Windows.Forms.Button();
            this.ProgressTimer = new System.Windows.Forms.Timer(this.components);
            this.bnPlayNext = new System.Windows.Forms.Button();
            this.bnAddCue = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.bnDeleteCue = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.createSampleProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportShowFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importShowFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.transportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.previousCueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextCueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.autoLoadLastsfxCuelistToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.confirmDeleteCueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.audioMidiDevicesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ScrollTimer = new System.Windows.Forms.Timer(this.components);
            this.bnPrev = new System.Windows.Forms.Button();
            this.bnNext = new System.Windows.Forms.Button();
            this.rtPrevMainText = new System.Windows.Forms.RichTextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusBar = new System.Windows.Forms.ToolStripStatusLabel();
            this.WebLink = new System.Windows.Forms.ToolStripStatusLabel();
            this.playbackProgressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.playbackTimeLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.trackInfoLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.DeviceChangeTimer = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.bnMIDI = new System.Windows.Forms.ToolStripDropDownButton();
            this.cbMIDI = new System.Windows.Forms.ToolStripComboBox();
            this.bnPreview = new System.Windows.Forms.ToolStripDropDownButton();
            this.cbPreview = new System.Windows.Forms.ToolStripComboBox();
            this.bnPlayback = new System.Windows.Forms.ToolStripDropDownButton();
            this.cbPlayback = new System.Windows.Forms.ToolStripComboBox();
            this.recentAudioFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentAudioFilesSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // CueList
            // 
            this.CueList.AllowDrop = true;
            this.CueList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CueList.AutoScroll = true;
            this.CueList.BackColor = System.Drawing.Color.FromArgb(17, 17, 34);
            this.CueList.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 556F));
            this.CueList.Location = new System.Drawing.Point(305, 34);
            this.CueList.Name = "CueList";
            this.CueList.Size = new System.Drawing.Size(556, 394);
            this.CueList.TabIndex = 4;
            this.CueList.Scroll += new System.Windows.Forms.ScrollEventHandler(this.CueList_Scroll);
            this.CueList.ClientSizeChanged += new System.EventHandler(this.CueList_ClientSizeChanged);
            this.CueList.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.CueList_ControlAdded);
            this.CueList.ControlRemoved += new System.Windows.Forms.ControlEventHandler(this.CueList_ControlRemoved);
            this.CueList.DragDrop += new System.Windows.Forms.DragEventHandler(this.CueList_DragDrop);
            this.CueList.DragEnter += new System.Windows.Forms.DragEventHandler(this.CueList_DragEnter);
            this.CueList.DragOver += new System.Windows.Forms.DragEventHandler(this.CueList_DragOver);
            this.CueList.DragLeave += new System.EventHandler(this.CueList_DragLeave);
            this.CueList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CueList_MouseDown);
            // 
            // dlgOpenAudioFile
            // 
            this.dlgOpenAudioFile.FileName = "openFileDialog1";
            // 
            // rtMainText
            // 
            this.rtMainText.Location = new System.Drawing.Point(12, 244);
            this.rtMainText.Name = "rtMainText";
            this.rtMainText.BackColor = System.Drawing.Color.FromArgb(22, 33, 62);
            this.rtMainText.ForeColor = System.Drawing.Color.White;
            this.rtMainText.Size = new System.Drawing.Size(287, 178);
            this.rtMainText.TabIndex = 2;
            this.rtMainText.Text = "";
            this.rtMainText.TextChanged += new System.EventHandler(this.rtMainText_TextChanged);
            // 
            // bnStopAll
            // 
            this.bnStopAll.BackColor = System.Drawing.Color.FromArgb(192, 57, 43);
            this.bnStopAll.ForeColor = System.Drawing.Color.White;
            this.bnStopAll.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bnStopAll.Location = new System.Drawing.Point(12, 142);
            this.bnStopAll.Name = "bnStopAll";
            this.bnStopAll.Size = new System.Drawing.Size(177, 81);
            this.bnStopAll.TabIndex = 17;
            this.bnStopAll.Text = "STOP";
            this.toolTip1.SetToolTip(this.bnStopAll, "Esc");
            this.bnStopAll.UseVisualStyleBackColor = false;
            this.bnStopAll.Click += new System.EventHandler(this.bnStopAll_Click);
            // 
            // ProgressTimer
            // 
            this.ProgressTimer.Tick += new System.EventHandler(this.ProgressTimer_Tick);
            // 
            // bnPlayNext
            // 
            this.bnPlayNext.BackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.bnPlayNext.ForeColor = System.Drawing.Color.White;
            this.bnPlayNext.Location = new System.Drawing.Point(256, 172);
            this.bnPlayNext.Name = "bnPlayNext";
            this.bnPlayNext.Size = new System.Drawing.Size(48, 21);
            this.bnPlayNext.TabIndex = 3;
            this.bnPlayNext.Text = "&Go ˃";
            this.toolTip1.SetToolTip(this.bnPlayNext, "F5");
            this.bnPlayNext.UseVisualStyleBackColor = false;
            this.bnPlayNext.Click += new System.EventHandler(this.bnPlayNext_Click);
            // 
            // bnAddCue
            // 
            this.bnAddCue.Location = new System.Drawing.Point(232, 174);
            this.bnAddCue.Name = "bnAddCue";
            this.bnAddCue.Size = new System.Drawing.Size(18, 19);
            this.bnAddCue.TabIndex = 6;
            this.bnAddCue.Text = "+";
            this.bnAddCue.BackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            this.bnAddCue.ForeColor = System.Drawing.Color.White;
            this.toolTip1.SetToolTip(this.bnAddCue, "Add Cue");
            this.bnAddCue.UseVisualStyleBackColor = true;
            this.bnAddCue.Click += new System.EventHandler(this.bnAddCue_Click);
            // 
            // bnDeleteCue
            // 
            this.bnDeleteCue.Location = new System.Drawing.Point(215, 174);
            this.bnDeleteCue.Name = "bnDeleteCue";
            this.bnDeleteCue.Size = new System.Drawing.Size(18, 19);
            this.bnDeleteCue.TabIndex = 7;
            this.bnDeleteCue.Text = "-";
            this.bnDeleteCue.BackColor = System.Drawing.Color.FromArgb(85, 85, 102);
            this.bnDeleteCue.ForeColor = System.Drawing.Color.White;
            this.toolTip1.SetToolTip(this.bnDeleteCue, "Delete Cue");
            this.bnDeleteCue.UseVisualStyleBackColor = true;
            this.bnDeleteCue.Click += new System.EventHandler(this.bnDeleteCue_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.BackColor = System.Drawing.Color.FromArgb(15, 15, 30);
            this.menuStrip1.ForeColor = System.Drawing.Color.FromArgb(200, 200, 240);
            this.menuStrip1.Renderer = new System.Windows.Forms.ToolStripProfessionalRenderer(new global::SFXPlayer.classes.DarkToolStripColorTable());
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.transportToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(338, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator1,
            this.recentAudioFilesSeparator,
            this.recentAudioFilesToolStripMenuItem,
            this.createSampleProjectToolStripMenuItem,
            this.exportShowFileToolStripMenuItem,
            this.importShowFileToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(207, 6);
            // 
            // recentAudioFilesSeparator
            // 
            this.recentAudioFilesSeparator.Name = "recentAudioFilesSeparator";
            this.recentAudioFilesSeparator.Size = new System.Drawing.Size(207, 6);
            this.recentAudioFilesSeparator.Visible = false;
            // 
            // recentAudioFilesToolStripMenuItem
            // 
            this.recentAudioFilesToolStripMenuItem.Name = "recentAudioFilesToolStripMenuItem";
            this.recentAudioFilesToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.recentAudioFilesToolStripMenuItem.Text = "Recent &Audio Files";
            this.recentAudioFilesToolStripMenuItem.Visible = false;
            // 
            // createSampleProjectToolStripMenuItem
            // 
            this.createSampleProjectToolStripMenuItem.Name = "createSampleProjectToolStripMenuItem";
            this.createSampleProjectToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.createSampleProjectToolStripMenuItem.Text = "Create &Sample Project...";
            this.createSampleProjectToolStripMenuItem.Click += new System.EventHandler(this.createSampleProjectToolStripMenuItem_Click);
            // 
            // exportShowFileToolStripMenuItem
            // 
            this.exportShowFileToolStripMenuItem.Name = "exportShowFileToolStripMenuItem";
            this.exportShowFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.exportShowFileToolStripMenuItem.Text = "&Export Show File";
            this.exportShowFileToolStripMenuItem.Click += new System.EventHandler(this.exportShowFileToolStripMenuItem_Click);
            // 
            // importShowFileToolStripMenuItem
            // 
            this.importShowFileToolStripMenuItem.Name = "importShowFileToolStripMenuItem";
            this.importShowFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.importShowFileToolStripMenuItem.Text = "&Import Show File";
            this.importShowFileToolStripMenuItem.Click += new System.EventHandler(this.importShowFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(207, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // transportToolStripMenuItem
            // 
            this.transportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.playToolStripMenuItem,
            this.stopAllToolStripMenuItem,
            this.previousCueToolStripMenuItem,
            this.nextCueToolStripMenuItem,
            this.toolStripSeparator3});
            this.transportToolStripMenuItem.Name = "transportToolStripMenuItem";
            this.transportToolStripMenuItem.Size = new System.Drawing.Size(68, 20);
            this.transportToolStripMenuItem.Text = "&Transport";
            // 
            // playToolStripMenuItem
            // 
            this.playToolStripMenuItem.Name = "playToolStripMenuItem";
            this.playToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.playToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.playToolStripMenuItem.Text = "&Play";
            this.playToolStripMenuItem.Click += new System.EventHandler(this.playToolStripMenuItem_Click);
            // 
            // stopAllToolStripMenuItem
            // 
            this.stopAllToolStripMenuItem.Name = "stopAllToolStripMenuItem";
            this.stopAllToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F4;
            this.stopAllToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.stopAllToolStripMenuItem.Text = "Stop &All";
            this.stopAllToolStripMenuItem.Click += new System.EventHandler(this.stopAllToolStripMenuItem_Click);
            // 
            // previousCueToolStripMenuItem
            // 
            this.previousCueToolStripMenuItem.Name = "previousCueToolStripMenuItem";
            this.previousCueToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.previousCueToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.previousCueToolStripMenuItem.Text = "Previous Cue";
            this.previousCueToolStripMenuItem.Click += new System.EventHandler(this.previousCueToolStripMenuItem_Click);
            // 
            // nextCueToolStripMenuItem
            // 
            this.nextCueToolStripMenuItem.Name = "nextCueToolStripMenuItem";
            this.nextCueToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F7;
            this.nextCueToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.nextCueToolStripMenuItem.Text = "Next Cue";
            this.nextCueToolStripMenuItem.Click += new System.EventHandler(this.nextCueToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(159, 6);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoLoadLastsfxCuelistToolStripMenuItem,
            this.confirmDeleteCueToolStripMenuItem,
            this.audioMidiDevicesToolStripMenuItem});
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // autoLoadLastsfxCuelistToolStripMenuItem
            // 
            this.autoLoadLastsfxCuelistToolStripMenuItem.Checked = true;
            this.autoLoadLastsfxCuelistToolStripMenuItem.CheckOnClick = true;
            this.autoLoadLastsfxCuelistToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoLoadLastsfxCuelistToolStripMenuItem.Name = "autoLoadLastsfxCuelistToolStripMenuItem";
            this.autoLoadLastsfxCuelistToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.autoLoadLastsfxCuelistToolStripMenuItem.Text = "Auto load last .sfx cue-list";
            this.autoLoadLastsfxCuelistToolStripMenuItem.Click += new System.EventHandler(this.autoLoadLastsfxCuelistToolStripMenuItem_Click);
            // 
            // confirmDeleteCueToolStripMenuItem
            // 
            this.confirmDeleteCueToolStripMenuItem.CheckOnClick = true;
            this.confirmDeleteCueToolStripMenuItem.Name = "confirmDeleteCueToolStripMenuItem";
            this.confirmDeleteCueToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.confirmDeleteCueToolStripMenuItem.Text = "Confirm delete cue";
            this.confirmDeleteCueToolStripMenuItem.Click += new System.EventHandler(this.confirmDeleteCueToolStripMenuItem_Click);
            // 
            // audioMidiDevicesToolStripMenuItem
            // 
            this.audioMidiDevicesToolStripMenuItem.Name = "audioMidiDevicesToolStripMenuItem";
            this.audioMidiDevicesToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.audioMidiDevicesToolStripMenuItem.Text = "Audio && MIDI Devices...";
            this.audioMidiDevicesToolStripMenuItem.Click += new System.EventHandler(this.audioMidiDevicesToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.aboutToolStripMenuItem.Text = "&About SFX Player...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // ScrollTimer
            // 
            this.ScrollTimer.Interval = 50;
            this.ScrollTimer.Tick += new System.EventHandler(this.ScrollTimer_Tick);
            // 
            // bnPrev
            // 
            this.bnPrev.Location = new System.Drawing.Point(256, 146);
            this.bnPrev.Name = "bnPrev";
            this.bnPrev.Size = new System.Drawing.Size(23, 23);
            this.bnPrev.TabIndex = 26;
            this.bnPrev.Text = "˄";
            this.bnPrev.BackColor = System.Drawing.Color.FromArgb(85, 85, 102);
            this.bnPrev.ForeColor = System.Drawing.Color.White;
            this.bnPrev.Click += new System.EventHandler(this.bnPrev_Click);
            // 
            // bnNext
            // 
            this.bnNext.Location = new System.Drawing.Point(256, 199);
            this.bnNext.Name = "bnNext";
            this.bnNext.Size = new System.Drawing.Size(23, 23);
            this.bnNext.TabIndex = 27;
            this.bnNext.Text = "˅";
            this.bnNext.BackColor = System.Drawing.Color.FromArgb(85, 85, 102);
            this.bnNext.ForeColor = System.Drawing.Color.White;
            this.bnNext.Click += new System.EventHandler(this.bnNext_Click);
            // 
            // rtPrevMainText
            // 
            this.rtPrevMainText.Location = new System.Drawing.Point(12, 34);
            this.rtPrevMainText.Name = "rtPrevMainText";
            this.rtPrevMainText.BackColor = System.Drawing.Color.FromArgb(58, 58, 74);
            this.rtPrevMainText.ForeColor = System.Drawing.Color.FromArgb(170, 170, 204);
            this.rtPrevMainText.Size = new System.Drawing.Size(287, 102);
            this.rtPrevMainText.TabIndex = 28;
            this.rtPrevMainText.Text = "";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.pictureBox1.Location = new System.Drawing.Point(284, 164);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(576, 3);
            this.pictureBox1.TabIndex = 29;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Visible = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.pictureBox2.Location = new System.Drawing.Point(284, 193);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(576, 3);
            this.pictureBox2.TabIndex = 30;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Visible = false;
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.Color.FromArgb(15, 15, 30);
            this.statusStrip.ForeColor = System.Drawing.Color.FromArgb(200, 200, 240);
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusBar,
            this.trackInfoLabel,
            this.playbackProgressBar,
            this.playbackTimeLabel,
            this.WebLink});
            this.statusStrip.Location = new System.Drawing.Point(0, 428);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(861, 22);
            this.statusStrip.TabIndex = 31;
            this.statusStrip.Text = "statusStrip1";
            // 
            // statusBar
            // 
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(798, 17);
            this.statusBar.Spring = true;
            this.statusBar.Text = "toolStripStatusLabel1";
            this.statusBar.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // WebLink
            // 
            this.WebLink.Name = "WebLink";
            this.WebLink.Size = new System.Drawing.Size(48, 17);
            this.WebLink.Text = "Remote";
            this.WebLink.Click += new System.EventHandler(this.WebLink_Click);
            // 
            // playbackProgressBar
            // 
            this.playbackProgressBar.Name = "playbackProgressBar";
            this.playbackProgressBar.Size = new System.Drawing.Size(120, 16);
            this.playbackProgressBar.Minimum = 0;
            this.playbackProgressBar.Maximum = 1000;
            this.playbackProgressBar.Value = 0;
            this.playbackProgressBar.ToolTipText = "Playback progress";
            // 
            // playbackTimeLabel
            // 
            this.playbackTimeLabel.Name = "playbackTimeLabel";
            this.playbackTimeLabel.Size = new System.Drawing.Size(90, 17);
            this.playbackTimeLabel.Text = "0:00 / 0:00";
            this.playbackTimeLabel.ToolTipText = "Position / -Remaining";
            // 
            // trackInfoLabel
            // 
            this.trackInfoLabel.Name = "trackInfoLabel";
            this.trackInfoLabel.AutoSize = true;
            this.trackInfoLabel.Text = "";
            this.trackInfoLabel.ToolTipText = "File path";
            // 
            // DeviceChangeTimer
            // 
            this.DeviceChangeTimer.Interval = 25;
            this.DeviceChangeTimer.Tick += new System.EventHandler(this.DeviceChangeTimer_Tick);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.BackColor = System.Drawing.Color.FromArgb(15, 15, 30);
            this.toolStrip1.ForeColor = System.Drawing.Color.FromArgb(200, 200, 240);
            this.toolStrip1.Renderer = new System.Windows.Forms.ToolStripProfessionalRenderer(new global::SFXPlayer.classes.DarkToolStripColorTable());
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bnMIDI,
            this.bnPreview,
            this.bnPlayback});
            this.toolStrip1.Location = new System.Drawing.Point(299, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(87, 31);
            this.toolStrip1.TabIndex = 36;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // bnMIDI
            // 
            this.bnMIDI.BackColor = System.Drawing.Color.Red;
            this.bnMIDI.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bnMIDI.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cbMIDI});
            this.bnMIDI.Name = "bnMIDI";
            this.bnMIDI.ShowDropDownArrow = false;
            this.bnMIDI.Size = new System.Drawing.Size(48, 28);
            this.bnMIDI.Text = "MIDI";
            this.bnMIDI.ToolTipText = "MIDI Out";
            // 
            // cbMIDI
            // 
            this.cbMIDI.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbMIDI.DropDownWidth = 221;
            this.cbMIDI.Name = "cbMIDI";
            this.cbMIDI.Size = new System.Drawing.Size(121, 23);
            this.cbMIDI.SelectedIndexChanged += new System.EventHandler(this.cbMIDI_SelectedIndexChanged);
            // 
            // bnPreview
            // 
            this.bnPreview.BackColor = System.Drawing.Color.Red;
            this.bnPreview.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bnPreview.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cbPreview});
            this.bnPreview.Name = "bnPreview";
            this.bnPreview.ShowDropDownArrow = false;
            this.bnPreview.Size = new System.Drawing.Size(58, 28);
            this.bnPreview.Text = "Preview";
            this.bnPreview.ToolTipText = "Preview Device";
            // 
            // cbPreview
            // 
            this.cbPreview.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPreview.DropDownWidth = 221;
            this.cbPreview.Name = "cbPreview";
            this.cbPreview.Size = new System.Drawing.Size(121, 23);
            this.cbPreview.SelectedIndexChanged += new System.EventHandler(this.cbPreview_SelectedIndexChanged);
            // 
            // bnPlayback
            // 
            this.bnPlayback.BackColor = System.Drawing.Color.Red;
            this.bnPlayback.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.bnPlayback.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cbPlayback});
            this.bnPlayback.Name = "bnPlayback";
            this.bnPlayback.ShowDropDownArrow = false;
            this.bnPlayback.Size = new System.Drawing.Size(65, 28);
            this.bnPlayback.Text = "Playback";
            this.bnPlayback.ToolTipText = "Playback Device";
            // 
            // cbPlayback
            // 
            this.cbPlayback.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPlayback.DropDownWidth = 221;
            this.cbPlayback.Name = "cbPlayback";
            this.cbPlayback.Size = new System.Drawing.Size(121, 23);
            this.cbPlayback.SelectedIndexChanged += new System.EventHandler(this.cbPlayback_SelectedIndexChanged);
            // 
            // lbPrevCueInfo
            // 
            this.lbPrevCueInfo = new System.Windows.Forms.Label();
            this.lbPrevCueInfo.AutoSize = false;
            this.lbPrevCueInfo.Location = new System.Drawing.Point(12, 136);
            this.lbPrevCueInfo.Name = "lbPrevCueInfo";
            this.lbPrevCueInfo.Size = new System.Drawing.Size(287, 30);
            this.lbPrevCueInfo.TabIndex = 40;
            // 
            // lbNextCueInfo
            // 
            this.lbNextCueInfo = new System.Windows.Forms.Label();
            this.lbNextCueInfo.AutoSize = false;
            this.lbNextCueInfo.Location = new System.Drawing.Point(12, 400);
            this.lbNextCueInfo.Name = "lbNextCueInfo";
            this.lbNextCueInfo.Size = new System.Drawing.Size(287, 30);
            this.lbNextCueInfo.TabIndex = 41;
            // 
            // SFXPlayer
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.ForeColor = System.Drawing.Color.FromArgb(224, 224, 224);
            this.ClientSize = new System.Drawing.Size(861, 450);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.rtPrevMainText);
            this.Controls.Add(this.lbPrevCueInfo);
            this.Controls.Add(this.lbNextCueInfo);
            this.Controls.Add(this.bnNext);
            this.Controls.Add(this.bnPrev);
            this.Controls.Add(this.bnDeleteCue);
            this.Controls.Add(this.bnAddCue);
            this.Controls.Add(this.bnPlayNext);
            this.Controls.Add(this.bnStopAll);
            this.Controls.Add(this.rtMainText);
            this.Controls.Add(this.CueList);
            this.Controls.Add(this.menuStrip1);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(875, 482);
            this.Name = "SFXPlayer";
            this.RightToLeftLayout = true;
            this.Text = "Form1";
            this.toolTip1.SetToolTip(this, "Playback");
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ControlAdded += new System.Windows.Forms.ControlEventHandler(this.Form1_ControlAdded);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void stopAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bnStopAll_Click(sender, e);
        }

        private void playToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bnPlayNext_Click(sender, e);
        }

        private void previousCueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bnPrev_Click(sender, e);
        }

        private void nextCueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bnNext_Click(sender, e);
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("It is recommended to disable system sounds\r\n" +
                            "right click on the windows speaker icon and choose \"Sounds\"\r\n" +
                            "then choose Sound Scheme: No Sounds", Application.ProductName);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                bnStopAll_Click(sender, e);
            }
        }

        private void Form1_ControlAdded(object sender, ControlEventArgs e)
        {
            FocusTrackLowestControls(e.Control);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _ = WebApp.StopAsync();
        }

        private void WebLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(WebLink.Text) { UseShellExecute = true });
        }

        private void DeviceChangeTimer_Tick(object sender, EventArgs e)
        {
            DeviceChangeTimer.Enabled = false;
            Debug.WriteLine("Device Changed");
            UpdateDevices();
        }

        private void CueList_MouseDown(object sender, MouseEventArgs e)
        {
            Point CueListMousePos = ScreenToChild(e.Location, CueList);
            Control selectedControl = CueList.GetChildAtPoint(CueListMousePos);
            Debug.WriteLine("CueList_MouseDown");
            if (selectedControl != null)
            {
                if (selectedControl.GetType() == typeof(PlayStrip))
                {
                    DoDragDrop(selectedControl, DragDropEffects.Move | DragDropEffects.Scroll);
                }
            }
        }

        private void CueList_DragLeave(object sender, EventArgs e)
        {
            UnHighlightControl();
        }

        private void CueList_DragOver(object sender, DragEventArgs e)
        {
            AddZone = false;
            ReplaceZone = false;

            Point CueListMousePos = ScreenToChild(new Point(e.X, e.Y), CueList);
            int Ypos = CueListMousePos.Y - CueList.AutoScrollPosition.Y;
            Control ctl = CueList.GetChildAtPoint(CueListMousePos);
            PlayStrip ps = ctl as PlayStrip;

            if (e.Data.GetDataPresent(typeof(PlayStrip)))
            {
                // When reordering PlayStrips allow dropping anywhere in the list
                AddZone = true;
                e.Effect = DragDropEffects.Move;
                if (ps != null && ps != (PlayStrip)e.Data.GetData(typeof(PlayStrip)))
                    HighlightControl(ps);
                else if (ctl is Spacer)
                    HighlightControl(ctl);
                else
                    UnHighlightControl();
            }
            else if (ps != null && ReplaceOK)
            {
                HighlightControl(ps);
                ReplaceZone = true;
                e.Effect = DragDropEffects.Move;
            }
            else if (AddOK && Math.Abs(Ypos % CueListSpacing) < SpacerControlHeight)
            {
                UnHighlightControl(); HighlightControl(null);
                AddZone = true;
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                UnHighlightControl();
                e.Effect = DragDropEffects.None;
            }
        }

        private void CueList_DragEnter(object sender, DragEventArgs e)
        {
            ReplaceOK = AddOK = false;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (CheckAllFilesAreAudio(files))
                {
                    AddOK = true;
                    ReplaceOK = (files.Length == 1);
                }
            }
            else if (e.Data.GetDataPresent(typeof(PlayStrip)))
            {
                AddOK = true;
            }
        }

        private void CueList_DragDrop(object sender, DragEventArgs e)
        {
            Point CueListMousePos = ScreenToChild(new Point(e.X, e.Y), CueList);
            int index = (CueListMousePos.Y - CueList.AutoScrollPosition.Y - TOPGAP) / CueListSpacing + 1;
            index = Math.Max(index, 0);
            index = Math.Min(index, CurrentShow.Cues.Count);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                PlayStrip ps = LastHovered as PlayStrip;
                if (ps != null)
                {
                    string msg = "do you wish to replace the file" + Environment.NewLine;
                    msg += ps.SFX.ShortFileNameOnly + Environment.NewLine;
                    msg += "with" + Environment.NewLine;
                    msg += Path.GetFileName(files[0]) + Environment.NewLine;
                    msg += "in cue " + (ps.PlayStripIndex + 1).ToString("D3") + "?" + Environment.NewLine;
                    if (string.IsNullOrEmpty(ps.SFX.FileName) ||
                        MessageBox.Show(msg, "Replace File", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                    {
                        ps.SelectFile(files[0]);
                    }
                }
                else if (AddZone)
                {
                    foreach (string file in files)
                    {
                        SFX sfx = new SFX();
                        InsertPlaystrip(sfx, index).SelectFile(file);
                        CurrentShow.AddCue(sfx, index++);
                    }
                    PadCueList();
                    NextPlayCueChanged();
                }
            }
            else if (e.Data.GetDataPresent(typeof(PlayStrip)))
            {
                if (AddZone)
                {
                    PlayStrip ps = ((PlayStrip)e.Data.GetData(typeof(PlayStrip)));
                    int src = ps.PlayStripIndex;
                    int dest = index;
                    if (dest > src) dest--;
                    if (dest != src)
                    {
                        CurrentShow.Cues.Move(src, dest);
                        RemovePlaystrip(src);
                        InsertPlaystrip(ps.SFX, dest);
                        PadCueList();
                        NextPlayCueChanged();
                    }
                }
            }
            UnHighlightControl();
        }

        private void CueList_ControlRemoved(object sender, ControlEventArgs e)
        {
            if (e.Control is PlayStrip ps)
            {
                ps.Stop();
                ps.ReportStatus -= Ps_ReportStatus;
            }
            FocusUntrackLowestControls(e.Control);
        }

        private void CueList_ControlAdded(object sender, ControlEventArgs e)
        {
            FocusTrackLowestControls(e.Control);
        }

        private void cbPreview_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InitialisingDevices) return;
            if (CurrentPreviewDeviceIdx != cbPreview.SelectedIndex)
            {
                if (cbPreview.SelectedIndex != -1)
                {
                    if (Settings.Default.LastPreviewDevice != (string)cbPreview.SelectedItem)
                    {
                        Settings.Default.LastPreviewDevice = (string)cbPreview.SelectedItem;
                        Settings.Default.Save();
                        UpdateDevices();
                    }
                }
                else
                {
                    UpdateDevices();
                }
            }
        }

        private void cbPlayback_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InitialisingDevices) return;
            if (CurrentPlaybackDeviceIdx != cbPlayback.SelectedIndex)
            {
                if (cbPlayback.SelectedIndex != -1)
                {
                    if (Settings.Default.LastPlaybackDevice != (string)cbPlayback.SelectedItem)
                    {
                        Settings.Default.LastPlaybackDevice = (string)cbPlayback.SelectedItem;
                        Settings.Default.Save();
                        UpdateDevices();
                    }
                }
                else
                {
                    UpdateDevices();
                }
            }
        }

        private void cbMIDI_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (InitialisingDevices) return;
            if (CurrentMIDIDeviceIdx != cbMIDI.SelectedIndex)
            {
                if (cbMIDI.SelectedIndex != -1)
                {
                    if (Settings.Default.LastMidiDevice != (string)cbMIDI.SelectedItem)
                    {
                        Settings.Default.LastMidiDevice = (string)cbMIDI.SelectedItem;
                        Settings.Default.Save();
                        UpdateDevices();
                    }
                }
                else
                {
                    UpdateDevices();
                }
            }
        }

        private void bnPrev_Click(object sender, EventArgs e)
        {
            NextPlayCueIndex -= 1;
        }

        private void bnNext_Click(object sender, EventArgs e)
        {
            NextPlayCueIndex += 1;
        }

        private void autoLoadLastsfxCuelistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.AutoLoadLastSession = autoLoadLastsfxCuelistToolStripMenuItem.Checked;
            Settings.Default.Save();
        }

        private void confirmDeleteCueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.ConfirmDeleteCue = confirmDeleteCueToolStripMenuItem.Checked;
            Settings.Default.Save();
        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel CueList;
        private System.Windows.Forms.OpenFileDialog dlgOpenAudioFile;
        private System.Windows.Forms.RichTextBox rtMainText;
        private System.Windows.Forms.Button bnStopAll;
        private System.Windows.Forms.Timer ProgressTimer;
        private System.Windows.Forms.Button bnPlayNext;
        private System.Windows.Forms.Button bnAddCue;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button bnDeleteCue;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem createSampleProjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportShowFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importShowFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        internal System.Windows.Forms.Timer ScrollTimer;
        private System.Windows.Forms.ToolStripMenuItem transportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem playToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopAllToolStripMenuItem;
        private System.Windows.Forms.Button bnPrev;
        private System.Windows.Forms.Button bnNext;
        private System.Windows.Forms.RichTextBox rtPrevMainText;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentAudioFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator recentAudioFilesSeparator;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoLoadLastsfxCuelistToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem confirmDeleteCueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem audioMidiDevicesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem previousCueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nextCueToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusBar;
        private System.Windows.Forms.ToolStripStatusLabel WebLink;
        private System.Windows.Forms.ToolStripProgressBar playbackProgressBar;
        private System.Windows.Forms.ToolStripStatusLabel playbackTimeLabel;
        private System.Windows.Forms.ToolStripStatusLabel trackInfoLabel;
        private System.Windows.Forms.Timer DeviceChangeTimer;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton bnMIDI;
        private System.Windows.Forms.ToolStripComboBox cbMIDI;
        private System.Windows.Forms.ToolStripDropDownButton bnPreview;
        private System.Windows.Forms.ToolStripComboBox cbPreview;
        private System.Windows.Forms.ToolStripDropDownButton bnPlayback;
        private System.Windows.Forms.ToolStripComboBox cbPlayback;
        private System.Windows.Forms.Label lbPrevCueInfo;
        private System.Windows.Forms.Label lbNextCueInfo;
    }
}

