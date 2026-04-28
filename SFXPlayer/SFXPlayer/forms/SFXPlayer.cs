using static SFXPlayer.classes.SVGResources;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Midi;
using NAudio.Wave;
using SFXPlayer.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Runtime.InteropServices;
using Svg.FilterEffects;
using SFXPlayer.classes;

namespace SFXPlayer
{
    public partial class SFXPlayer : Form
    {
        const int TOPPLACEHOLDER = -1;
        const int BOTTOMPLACEHOLDER = -2;
        const string FileExtensions = "SFX Cue Files (*.sfx)|*.sfx";
        private bool InitialisingDevices = false;
        private XMLFileHandler<Show> ShowFileHandler = new XMLFileHandler<Show>();
        private const int WM_DEVICECHANGE = 0x0219;
        private readonly ConcurrentQueue<Action> _commandQueue = new ConcurrentQueue<Action>();
        /// <summary>
        /// Tracks all PlayStrip instances that are currently playing.
        /// Updated via PlayingStateChanged events so StopAll can stop them efficiently.
        /// </summary>
        private readonly HashSet<PlayStrip> _playingSounds = new HashSet<PlayStrip>();

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_DEVICECHANGE)
            {
                DeviceChangeTimer.Stop();       //force a restart
                DeviceChangeTimer.Start();
            }
            base.WndProc(ref m);
        }

        private Show _CurrentShow;
        private Show CurrentShow
        {
            get
            {
                return _CurrentShow;
            }
            set
            {
                if (_CurrentShow != null)
                {
                    _CurrentShow.ShowFileBecameDirty -= ShowFileHandler.SetDirty;
                    _CurrentShow.UpdateShow -= UpdateDisplay;
                    foreach (SFX sfx in _CurrentShow.Cues)
                    {
                        sfx.SFXBecameDirty -= ShowFileHandler.SetDirty;
                    }
                }
                _CurrentShow = value;
                ResetDisplay();
                if (_CurrentShow != null)
                {
                    _CurrentShow.ShowFileBecameDirty += ShowFileHandler.SetDirty;
                    _CurrentShow.UpdateShow += UpdateDisplay;
                    foreach (SFX sfx in _CurrentShow.Cues)
                    {
                        sfx.SFXBecameDirty += ShowFileHandler.SetDirty;
                    }
                }
            }
        }
        private readonly int CueListSpacing = new PlayStrip().Height + new Spacer().Height;
        private readonly int PlayStripControlHeight = new PlayStrip().Height;
        private readonly int SpacerControlHeight = new Spacer().Height;
        private readonly int TOPGAP = 5 * (new PlayStrip().Height + new Spacer().Height);
        private readonly ObservableCollection<string> PlayDevices = new ObservableCollection<string>();
        private readonly ObservableCollection<string> PreviewDevices = new ObservableCollection<string>();
        private MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
        public static Control lastFocused;
        string[] filters;

        public SFXPlayer()
        {
            // Initialize Forms Designer generated code.
            InitializeComponent();

            // Set up the status bar and other controls.
            InitializeControls();

            DeviceChangeTimer.Start();      //trigger a reload of the available audio/MIDI devices
        }

        // Sets up the status bar and other controls.
        private void InitializeControls()
        {
            ShowFileHandler.FileExtensions = FileExtensions;
            bnStopAll.Top = bnPrev.Top = CueList.Top + TOPGAP - bnPrev.Height;
            bnPlayNext.Top = CueList.Top + TOPGAP;
            bnPlayNext.Height = PlayStripControlHeight;
            bnPlayNext.BackColor = Settings.Default.ColourPlayerPlay;
            pictureBox1.BackColor = Settings.Default.ColourPlayerPlay;
            pictureBox2.BackColor = Settings.Default.ColourPlayerPlay;
            pictureBox1.Top = bnPlayNext.Top - pictureBox1.Height;
            pictureBox2.Top = bnPlayNext.Bottom;
            bnDeleteCue.Top = bnAddCue.Top = bnPlayNext.Top + (bnPlayNext.Height - bnAddCue.Height) / 2;
            bnNext.Top = CueList.Top + TOPGAP + bnPlayNext.Height;
            bnStopAll.Height = bnNext.Top + bnNext.Height - bnStopAll.Top;

            // Reserve space for detail label below rtPrevMainText
            const int detailLabelHeight = 32;
            rtPrevMainText.Height = bnStopAll.Top - bnStopAll.Margin.Top - detailLabelHeight - rtPrevMainText.Margin.Bottom - rtPrevMainText.Top;

            // Position prev cue detail label between rtPrevMainText and bnStopAll
            lbPrevCueInfo.Left = rtPrevMainText.Left;
            lbPrevCueInfo.Width = rtPrevMainText.Width;
            lbPrevCueInfo.Top = rtPrevMainText.Bottom + rtPrevMainText.Margin.Bottom;
            lbPrevCueInfo.Height = detailLabelHeight;
            lbPrevCueInfo.AutoSize = false;
            lbPrevCueInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5f);
            lbPrevCueInfo.BackColor = System.Drawing.Color.FromArgb(220, 220, 220);
            lbPrevCueInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            rtMainText.Top = bnStopAll.Bottom + rtMainText.Margin.Top + bnStopAll.Margin.Bottom;
            int nextInfoTop = Math.Min(statusStrip.Top - rtMainText.Margin.Bottom - detailLabelHeight, rtMainText.Top + rtPrevMainText.Height);
            rtMainText.Height = nextInfoTop - rtMainText.Top;

            // Position next cue detail label below rtMainText
            lbNextCueInfo.Left = rtMainText.Left;
            lbNextCueInfo.Width = rtMainText.Width;
            lbNextCueInfo.Top = nextInfoTop;
            lbNextCueInfo.Height = detailLabelHeight;
            lbNextCueInfo.AutoSize = false;
            lbNextCueInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.5f);
            lbNextCueInfo.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            lbNextCueInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            PlayStrip.OFD = dlgOpenAudioFile;
            autoLoadLastsfxCuelistToolStripMenuItem.Checked = Settings.Default.AutoLoadLastSession;
            confirmDeleteCueToolStripMenuItem.Checked = Settings.Default.ConfirmDeleteCue;
        }

        // Convenience method for setting message text in 
        // the status bar.
        public void ReportStatus(string statusMessage)
        {
            // If the caller passed in a message...

            if (this.statusStrip.InvokeRequired)
            {
                Action<string> d = new Action<string>(ReportStatus);
                this.Invoke(d, new object[] { statusMessage });
            }
            else
            {
                //if (string.IsNullOrEmpty(statusMessage)) {

                //} else {
                this.statusBar.Text = statusMessage;
                //Status.Text = statusMessage;
                //statusBar.Refresh();
                //}
            }
        }

        public PlayStrip PrevPlayCue
        {
            get
            {
                Point pt = new Point(0, bnPlayNext.Top - CueList.Top - CueListSpacing);
                Control ctl = CueList.GetChildAtPoint(pt);
                if (ctl is Spacer)
                {
                    int row = CueList.GetRow(ctl);
                    ctl = CueList.GetControlFromPosition(0, Math.Max(0, row - 1));
                }
                return ctl as PlayStrip;
            }
        }

        public PlayStrip NextPlayCue
        {
            get
            {
                Point pt = new Point(0, bnPlayNext.Top - CueList.Top);
                Control ctl = CueList.GetChildAtPoint(pt);
                if (ctl is Spacer)
                {
                    int row = CueList.GetRow(ctl);
                    ctl = CueList.GetControlFromPosition(0, Math.Max(0, row - 1));
                }
                return ctl as PlayStrip;
            }
        }

        public int NextPlayCueIndex
        {
            get
            {
                return -CueList.AutoScrollPosition.Y / (CueListSpacing);
            }
            set
            {
                int NewValue = value * (CueListSpacing);
                NewValue = Math.Min(NewValue, CueList.VerticalScroll.Maximum);
                Point NewPos = new Point(0, NewValue);
                CueList.AutoScrollPosition = NewPos;
                CueList.AutoScrollPosition = NewPos;        //verticalscroll update only worked when called twice, need to check this.
                NextPlayCueChanged();
            }
        }

        public event EventHandler<DisplaySettings> DisplayChanged;

        protected virtual void OnDisplayChanged(DisplaySettings e)
        {
            DisplayChanged?.Invoke(this, e);
        }

        private void UpdateWebApp()
        {
            PlayStrip next = NextPlayCue;
            PlayStrip prev = PrevPlayCue;
            DisplaySettings disp = new DisplaySettings()
            {
                Title = Text,
                PrevMainText = rtPrevMainText.Text,
                MainText = rtMainText.Text,
                TrackName = Path.GetFileName(next?.SFX.FileName),
                TrackInfo = BuildTrackInfoString(next),
                CurrentVolume = next?.SFX.Volume ?? 50,
                CurrentSpeed = next?.SFX.Speed ?? 1.0f,
                StopOthers = next?.SFX.StopOthers ?? false,
                CueNumber = next != null ? (next.PlayStripIndex + 1).ToString("D3") : "",
                CueDescription = next?.SFX.Description ?? "",
                CueFileName = Path.GetFileName(next?.SFX.FileName ?? ""),
                CueAutoRun = next?.SFX.AutoPlay ?? false,
                CuePauseSeconds = (next?.SFX.AutoPlayPauseMs ?? 0) / 1000.0,
                CueFadeInMs = next?.SFX.FadeInDurationMs ?? 0,
                CueFadeOutMs = next?.SFX.FadeOutDurationMs ?? 0,
                CueFadeCurve = (next?.SFX.FadeCurve ?? classes.FadeCurve.Linear) == classes.FadeCurve.Logarithmic ? "Logarithmic" : "Linear",
                PrevCueNumber = prev != null ? (prev.PlayStripIndex + 1).ToString("D3") : "",
                PrevCueDescription = prev?.SFX.Description ?? "",
                PrevCueFileName = Path.GetFileName(prev?.SFX.FileName ?? "")
            };
            OnDisplayChanged(disp);
        }

        private void NextPlayCueChanged()
        {
            rtPrevMainText.TextChanged -= rtPrevMainText_TextChanged;
            if (PrevPlayCue != null)
            {
                rtPrevMainText.Text = PrevPlayCue.SFX.MainText;
                rtPrevMainText.ReadOnly = false;
            }
            else
            {
                rtPrevMainText.Text = "";
                rtPrevMainText.ReadOnly = true;
            }
            rtPrevMainText.TextChanged += rtPrevMainText_TextChanged;
            rtMainText.TextChanged -= rtMainText_TextChanged;
            if (NextPlayCue != null)
            {
                rtMainText.Text = NextPlayCue.SFX.MainText;
                rtMainText.ReadOnly = false;
            }
            else
            {
                rtMainText.Text = "";
                rtMainText.ReadOnly = true;
            }
            rtMainText.TextChanged += rtMainText_TextChanged;
            UpdateCueDetailLabels();
            CurrentShow.NextPlayCueIndex = NextPlayCueIndex;
            UpdateTrackInfoLabel(null);
            UpdateWebApp();
        }

        private void UpdateCueDetailLabels()
        {
            PlayStrip prev = PrevPlayCue;
            PlayStrip next = NextPlayCue;
            lbPrevCueInfo.Text = FormatCueDetail(prev);
            lbNextCueInfo.Text = FormatCueDetail(next);
        }

        private static string FormatCueDetail(PlayStrip ps)
        {
            if (ps == null) return "";
            SFX s = ps.SFX;
            string num = (ps.PlayStripIndex + 1).ToString("D3");
            string desc = string.IsNullOrEmpty(s.Description) ? "(no description)" : s.Description;
            string file = string.IsNullOrEmpty(s.FileName) ? "(no file)" : Path.GetFileName(s.FileName);
            string autoRun = s.AutoPlay
                ? string.Format(" | Auto-run {0}s", (s.AutoPlayPauseMs / 1000.0).ToString("0.0"))
                : "";
            return string.Format("#{0}  {1}  |  {2}  Vol:{3}{4}", num, desc, file, s.Volume, autoRun);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AppLogger.Info("SFXPlayer.Form1_Load");
            Debug.WriteLine("Form1_Load");
            StartWebApp();
            ShowFileHandler.FileTitleUpdate += UpdateTitleBar;
            //Insert = new PlayStrip() { Width = 100, BackColor = Color.Blue, isPlaceholder = false };
            bnMIDI.Image = FromSvgResource("midi_port_icon_135398.svg");
            bnPreview.Image = FromSvgResource("headphones.svg");
            bnPlayback.Image = FromSvgResource("volume-up-fill.svg");
            UpdateDevices();

            ProgressTimer.Enabled = true;
            string[] args = Environment.GetCommandLineArgs();
            foreach (string cmd in args)
            {
                Debug.WriteLine(cmd);
            }
            FileNew();
            string StartFile = "";
            if (args.Length == 2)
            {
                StartFile = args[1];
            }
            else
            {
                if (autoLoadLastsfxCuelistToolStripMenuItem.Checked)
                {
                    StartFile = Settings.Default.LastSession;
                }
            }
            if (File.Exists(StartFile))
            {
                Show newShow;
                newShow = ShowFileHandler.LoadFromFile(StartFile);
                if (newShow != null)
                {
                    int tempNextPlayCueIndex = newShow.NextPlayCueIndex;
                    CurrentShow = newShow;
                    NextPlayCueIndex = tempNextPlayCueIndex;
                }
            }
            ResetDisplay();
            PreloadAll();

            //Form1_Resize(this, new EventArgs());
            //MouseWheel += CueList_MouseWheel;
            CueList.MouseWheel += CueList_MouseWheel;     //used to reset scrolling to whole units
            //CueList.ControlAdded += CueList_ControlAdded;


            FocusTrackLowestControls(Controls);     //used for pop-up volume control
            //ShowContainerControls(Controls);
            UpdateWebApp();
        }

        List<string> AudioOutDevices = new List<string>();
        List<string> CurrentAudioOutDevices = new List<string>();
        List<string> CurrentMIDIOutDevices = new List<string>();
        public static int CurrentPlaybackDeviceIdx = -1;
        public static int CurrentPreviewDeviceIdx = -1;
        public static int CurrentMIDIDeviceIdx = -1;

        private void UpdateDevices()
        {
            //something changed, so get a new list of devices
            //check whether output audio device is still in the list

            //WaveOut gives us truncated names but the index we need to open the device
            //combine these with the full names from mmdevices to populate the device list
            //add/remove devices so that we don't reset the list

            AudioOutDevices.Clear();
            {   //fill in AudioOutDevices
                List<WaveOutCapabilities> WaveOutDevices = new List<WaveOutCapabilities>();

                for (int n = 0; n < WaveOut.DeviceCount; n++)
                {
                    WaveOutDevices.Add(WaveOut.GetCapabilities(n));
                }

                var mmdeviceCollection = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var device in WaveOutDevices)
                {
                    string FN = mmdeviceCollection.Where(d => d.FriendlyName.Contains(device.ProductName)).Select(dd => dd.FriendlyName).FirstOrDefault();
                    if (string.IsNullOrEmpty(FN)) FN = device.ProductName;
                    AudioOutDevices.Add(FN);
                    Debug.WriteLine(FN);
                }
            }

            if (!AudioOutDevices.SequenceEqual(CurrentAudioOutDevices))
            {
                CurrentAudioOutDevices = new List<string>(AudioOutDevices);
                cbPlayback.Items.Clear();
                cbPlayback.Items.AddRange(CurrentAudioOutDevices.ToArray());
                cbPreview.Items.Clear();
                cbPreview.Items.AddRange(CurrentAudioOutDevices.ToArray());
            }

            //MAIN OUTPUT
            int currentSelection = CurrentPlaybackDeviceIdx;
            if (CurrentAudioOutDevices.Contains(Settings.Default.LastPlaybackDevice))
            {
                CurrentPlaybackDeviceIdx = CurrentAudioOutDevices.IndexOf(Settings.Default.LastPlaybackDevice);
            }
            else
            {
                CurrentPlaybackDeviceIdx = -1;      //device not available
            }
            if (CurrentPlaybackDeviceIdx >= 0)
            {
                //Handle any device change stuff here!
                bnPlayback.BackColor = Color.FromArgb(0, 192, 0);
                bnPlayback.ToolTipText = Settings.Default.LastPlaybackDevice;
                cbPlayback.SelectedIndex = CurrentPlaybackDeviceIdx;
            }
            else
            {
                //Handle any device change stuff here!
                bnPlayback.BackColor = Color.Red;
                bnPlayback.ToolTipText = $"Not connected ({Settings.Default.LastPlaybackDevice})";
                cbPlayback.SelectedIndex = CurrentPlaybackDeviceIdx;
            }
            if (currentSelection != CurrentPlaybackDeviceIdx)
            {
                //update playstrips with new output device
                PreloadAll();
            }

            //PREVIEW OUTPUT
            currentSelection = CurrentPreviewDeviceIdx;
            if (CurrentAudioOutDevices.Contains(Settings.Default.LastPreviewDevice))
            {
                CurrentPreviewDeviceIdx = CurrentAudioOutDevices.IndexOf(Settings.Default.LastPreviewDevice);
            }
            else
            {
                CurrentPreviewDeviceIdx = -1;      //device not available
            }
            if (CurrentPreviewDeviceIdx >= 0)
            {
                //Handle any device change stuff here!
                bnPreview.BackColor = Color.FromArgb(0, 192, 0);
                bnPreview.ToolTipText = Settings.Default.LastPreviewDevice;
                cbPreview.SelectedIndex = CurrentPreviewDeviceIdx;
            }
            else
            {
                //Handle any device change stuff here!
                bnPreview.BackColor = Color.Red;
                bnPreview.ToolTipText = $"Not connected ({Settings.Default.LastPreviewDevice})";
                cbPreview.SelectedIndex = CurrentPreviewDeviceIdx;
            }
            if (currentSelection != CurrentPreviewDeviceIdx)
            {
                //update playstrips with new preview device
                //not needed
            }

            var MIDIOutDevices = new List<string>();
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                MIDIOutDevices.Add(MidiOut.DeviceInfo(i).ProductName);
                Debug.WriteLine(MidiOut.DeviceInfo(i).ProductName);
            }

            if (!MIDIOutDevices.SequenceEqual(CurrentMIDIOutDevices))
            {
                CurrentMIDIOutDevices = new List<string>(MIDIOutDevices);
                cbMIDI.Items.Clear();
                cbMIDI.Items.AddRange(CurrentMIDIOutDevices.ToArray());
            }

            //MIDI OUTPUT
            currentSelection = CurrentMIDIDeviceIdx;
            if (CurrentMIDIOutDevices.Contains(Settings.Default.LastMidiDevice))
            {
                CurrentMIDIDeviceIdx = CurrentMIDIOutDevices.IndexOf(Settings.Default.LastMidiDevice);
            }
            else
            {
                CurrentMIDIDeviceIdx = -1;      //device not available
            }

            if (CurrentMIDIDeviceIdx >= 0)
            {
                //Handle any device change stuff here!
                bnMIDI.BackColor = Color.FromArgb(0, 192, 0);
                bnMIDI.ToolTipText = Settings.Default.LastMidiDevice;
                cbMIDI.SelectedIndex = CurrentMIDIDeviceIdx;
            }
            else
            {
                //Handle any device change stuff here!
                bnMIDI.BackColor = Color.Red;
                bnMIDI.ToolTipText = $"Not connected ({Settings.Default.LastMidiDevice})";
                cbMIDI.SelectedIndex = CurrentMIDIDeviceIdx;
            }
            if (currentSelection != CurrentMIDIDeviceIdx)
            {
                //update playstrips with new MIDI Out device
                if (CurrentMIDIDeviceIdx == -1)
                {
                    if (MIDIOut != null)
                    {
                        MIDIOut.Close();
                        MIDIOut.Dispose();
                        MIDIOut = null;
                    }
                }
                if (CurrentMIDIDeviceIdx != -1)
                {
                    MIDIOut = new MidiOut(CurrentMIDIDeviceIdx);
                }
            }
            InitialisingDevices = false;
        }
        public MidiOut MIDIOut;
        private void StartWebApp()
        {
            AppLogger.Info("SFXPlayer.StartWebApp");
            Debug.WriteLine("MouseWheelScrollLines = " + SystemInformation.MouseWheelScrollLines);

            // Start WebApp asynchronously
            _ = Task.Run(async () =>
            {
                await WebApp.StartAsync();

                // Update UI on the UI thread after WebApp starts
                this.Invoke(new Action(() =>
                {
                    string localIP = GetLocalIPAddress();

                    if (WebApp.Serving)
                    {
                        WebLink.IsLink = true;
                        WebLink.Text = "http://" + localIP + ":" + WebApp.wsPort + "/";
                    }
                    else
                    {
                        WebLink.IsLink = false;
                        WebLink.Text = "Web-App not available (run as admin)";
                    }
                }));
            });
        }

        private string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                return "localhost";
            }
        }

        private void AudioDeviceNotifications_AudioDevicesChanged(object sender, EventArgs e)
        {
            PlayDevices.Clear();
            PreviewDevices.Clear();
            var mmdeviceCollection = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            {
                foreach (var device in mmdeviceCollection)
                {
                    PlayDevices.Add(device.FriendlyName);
                    PreviewDevices.Add(device.FriendlyName);
                    Debug.WriteLine(device.ToString());
                }
            }


        }

        private void UpdateTitleBar(object sender, EventArgs e)
        {
            string Title;
            Title = ShowFileHandler.DisplayFileName;
            if (ShowFileHandler.Dirty) Title += "*";
            Title += " - ";
            Title += Application.ProductName;
            Text = Title;
        }

        private void ShowContainerControls(Control.ControlCollection controls)
        {
            foreach (Control ctl in controls)
            {
                if (ctl.Controls.Count > 0)
                {
                    Debug.WriteLine("  ** {0} {1}", ctl.Name, ctl);
                    ShowContainerControls(ctl.Controls);
                }
            }
        }

        private void FocusTrackLowestControls(Control control)
        {
            if (control.Controls.Count > 0)
            {
                FocusTrackLowestControls(control.Controls);
            }
            else
            {
                control.Enter += TrackFocused;
                //Debug.WriteLine(" *+ {0} {1}", control.Name, control);
            }
        }

        private void TrackFocused(object sender, EventArgs e)
        {
            lastFocused = sender as Control;
            Debug.WriteLine("lastFocused = {0}", lastFocused);
        }

        private void FocusTrackLowestControls(Control.ControlCollection controls)
        {
            foreach (Control ctl in controls)
            {
                if (ctl.Controls.Count > 0)
                {
                    FocusTrackLowestControls(ctl.Controls);
                }
                else
                {
                    ctl.Enter += TrackFocused;
                    //Debug.WriteLine(" *+ {0} {1}", ctl.Name, ctl);
                }
            }
        }

        private void FocusUntrackLowestControls(Control control)
        {
            if (control.Controls.Count > 0)
            {
                FocusUntrackLowestControls(control.Controls);
            }
            else
            {
                control.Enter -= TrackFocused;
                //Debug.WriteLine(" *- {0} {1}", control.Name, control);
            }
        }

        private void FocusUntrackLowestControls(Control.ControlCollection controls)
        {
            foreach (Control ctl in controls)
            {
                if (ctl.Controls.Count > 0)
                {
                    FocusUntrackLowestControls(ctl.Controls);
                }
                else
                {
                    ctl.Enter -= TrackFocused;
                    //Debug.WriteLine(" *- {0} {1}", ctl.Name, ctl);
                }
            }
        }

        //private void CueList_ControlAdded(object sender, ControlEventArgs e) {
        //    e.Control.MouseWheel += CueList_MouseWheel;
        //}

        private void CueList_MouseWheel(object sender, MouseEventArgs e)
        {
            //e = new HandledMouseEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta, true);
            //ReportStatus(((Control)sender).Name + " MouseWheel " + e.Delta.ToString("D"));
            Debug.WriteLine(((Control)sender).Name + " MouseWheel " + e.Delta.ToString("D"));
            ScrollTimer.Enabled = true;
        }

        //private void Ctl_MouseWheel(object sender, MouseEventArgs e) {
        //    ReportStatus(((Control)sender).Name + " MouseWheel " + e.Delta.ToString("D"));
        //    e.Handled = true;
        //    ((HandledMouseEventArgs)e).Handled = true;
        //}

        //private void Ctl_MouseWheel(object sender, EventArgs e) {
        //    ReportStatus(((Control)sender).Name + " " + ((Control)sender).GetType().ToString());
        //}

        void FileNew()
        {
            AppLogger.Info("SFXPlayer.FileNew");
            CurrentShow = new Show();
            ShowFileHandler.NewFile();
        }

        void FileOpen()
        {
            AppLogger.Info("SFXPlayer.FileOpen (dialog)");
            Show newShow;
            newShow = ShowFileHandler.LoadFromFile();
            if (newShow != null)
            {
                int tempNextPlayCueIndex = newShow.NextPlayCueIndex;
                CurrentShow = newShow;
                NextPlayCueIndex = tempNextPlayCueIndex;
            }
        }

        void FileOpen(string FileName)
        {
            AppLogger.Info($"SFXPlayer.FileOpen: \"{FileName}\"");
            Show oldShow = CurrentShow;
            if (oldShow != null) oldShow.ShowFileBecameDirty -= ShowFileHandler.SetDirty;
            CurrentShow = ShowFileHandler.LoadFromFile(FileName);
            if (CurrentShow != null)
            {
                int tempNextPlayCueIndex = CurrentShow.NextPlayCueIndex;
                CurrentShow.UpdateShow += UpdateDisplay;
                ResetDisplay();
                NextPlayCueIndex = tempNextPlayCueIndex;
            }
            else
            {
                CurrentShow = oldShow;
            }
            CurrentShow.ShowFileBecameDirty += ShowFileHandler.SetDirty;
        }

        void UpdateDisplay()
        {
        }

        void ResetDisplay()
        {
            ShowFileHandler.PushDirty();
            CueList.SuspendLayout();
            CueList.Controls.Clear();
            CueList.RowStyles.Clear();
            CueList.RowCount = 0;
            //add padding and spacers top and bottom
            Spacer sp;
            sp = new Spacer { Width = CueList.ClientSize.Width, Name = "Top" };
            sp.Paint += Highlight_Paint;
            //sp.BackColor = Color.LightCoral;
            CueList.RowCount++;
            CueList.Controls.Add(sp, 0, 0);
            sp = new Spacer { Width = CueList.ClientSize.Width, Name = "Top spacer" };
            sp.Paint += Highlight_Paint;
            //sp.BackColor = Color.LightSteelBlue;
            CueList.RowCount++;
            CueList.Controls.Add(sp, 0, 1);

            if (CurrentShow != null)
            {
                foreach (SFX sfx in CurrentShow.Cues)
                {
                    AddPlaystrip(sfx, CurrentShow.Cues.IndexOf(sfx));
                }
            }

            sp = new Spacer { Width = CueList.ClientSize.Width, Name = "Bottom" };
            sp.Paint += Highlight_Paint;
            //sp.BackColor = Color.LightSeaGreen;
            CueList.RowCount++;
            CueList.Controls.Add(sp, 0, CueList.RowCount - 1);

            CueList.ResumeLayout();
            PadCueList();
            if (CurrentShow != null)
            {
                NextPlayCueChanged();
            }

            CueList.VerticalScroll.SmallChange = CueListSpacing;
            CueList.VerticalScroll.LargeChange = 3 * CueList.VerticalScroll.SmallChange;
            ShowFileHandler.PopDirty();
            UpdateTitleBar(this, new EventArgs());
        }

        private void Highlight_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);
            PlayStrip ps = NextPlayCue;
            if (ps != null)
            {
                if (((Control)sender).Bottom == ps.Top)
                {
                    e.Graphics.DrawLine(Pens.Black, 0, ((Control)sender).Height - 1, ((Control)sender).Width, ((Control)sender).Height - 1);
                }
                if (((Control)sender).Top == ps.Bottom)
                {
                    e.Graphics.DrawLine(Pens.Black, 0, 0, ((Control)sender).Width, 0);
                }
            }
        }

        /// <summary>
        /// Add Playstrip to the CueList control
        /// </summary>
        /// <param name="sfx">null = placeholder</param>
        /// <param name="rowIndex">sfx Index or TOPPLACEHOLDER or BOTTOMPLACEHOLDER</param>
        private void SubscribePlaystripEvents(PlayStrip ps)
        {
            ps.StopAll += (s, e) => _commandQueue.Enqueue(() => StopAllPlayingSounds(s as PlayStrip));
            ps.ReportStatus += Ps_ReportStatus;
            ps.DeleteCue += Ps_DeleteCue;
            ps.AddCueBefore += Ps_AddCueBefore;
            ps.PlayingStateChanged += (s, isPlaying) =>
            {
                var strip = s as PlayStrip;
                if (strip == null) return;
                if (isPlaying) _playingSounds.Add(strip);
                else _playingSounds.Remove(strip);
            };
            ps.AutoPlayNext += (s, pauseMs) => _commandQueue.Enqueue(() =>
            {
                if (pauseMs > 0)
                    System.Threading.Tasks.Task.Delay(pauseMs).ContinueWith(_ =>
                    {
                        try { if (!IsDisposed) _commandQueue.Enqueue(() => bnPlayNext_Click(null, null)); }
                        catch (Exception) { }
                    });
                else
                    bnPlayNext_Click(null, null);
            });
        }

        private void AddPlaystrip(SFX sfx, int cueIndex)
        {
            PlayStrip ps = new PlayStrip(sfx) { Width = CueList.ClientSize.Width, PlayStripIndex = cueIndex };
            SubscribePlaystripEvents(ps);
            FocusTrackLowestControls(ps);
            Spacer sp = new Spacer { Width = CueList.ClientSize.Width };
            sp.Paint += Highlight_Paint;
            int rowIndex = TableRowFromCueIndex(cueIndex);
            CueList.RowCount++;
            CueList.Controls.Add(ps, 0, rowIndex);
            rowIndex++;
            CueList.RowCount++;
            CueList.Controls.Add(sp, 0, rowIndex);
        }

        private void Ps_ReportStatus(object sender, StatusEventArgs e)
        {
            if (e.Clear)
            {
                if (statusBar.Text == e.Status)
                {
                    ReportStatus("");
                }
            }
            else
            {
                ReportStatus(e.Status);
            }
        }

        private void Ps_DeleteCue(object sender, EventArgs e)
        {
            PlayStrip ps = sender as PlayStrip;
            if (ps == null) return;
            if (Settings.Default.ConfirmDeleteCue)
            {
                DialogResult response = MessageBox.Show(
                    string.Format("Delete Cue {0}?\r\n{1}", ps.PlayStripIndex + 1, ps.SFX.Description),
                    "Cue List", MessageBoxButtons.YesNo);
                if (response != DialogResult.Yes) return;
            }
            SFX sfxToRemove = ps.SFX;
            RemovePlaystrip(ps.PlayStripIndex);
            CurrentShow.RemoveCue(sfxToRemove);
            PadCueList();
            NextPlayCueChanged();
        }

        private void Ps_AddCueBefore(object sender, EventArgs e)
        {
            PlayStrip ps = sender as PlayStrip;
            if (ps == null) return;
            int insertAt = ps.PlayStripIndex;
            SFX sfx = new SFX();
            InsertPlaystrip(sfx, insertAt);
            PadCueList();
            CurrentShow.AddCue(sfx, insertAt);
            NextPlayCueChanged();
        }

        private PlayStrip InsertPlaystrip(SFX sfx, int cueIndex)
        {
            PlayStrip ps;
            CueList.SuspendLayout();
            int rowIndex = TableRowFromCueIndex(cueIndex);
            CueList.RowCount += 2;      //add the 2 new rows
            foreach (Control ctl in CueList.Controls)
            {
                if (CueList.GetRow(ctl) >= rowIndex)
                {
                    CueList.SetRow(ctl, CueList.GetRow(ctl) + 2);
                    ps = ctl as PlayStrip;
                    if (ps != null)
                    {
                        ps.PlayStripIndex += 1;
                    }
                }
            }
            ps = new PlayStrip(sfx) { Width = CueList.ClientSize.Width, PlayStripIndex = cueIndex };
            SubscribePlaystripEvents(ps);
            FocusTrackLowestControls(ps);
            Spacer sp = new Spacer { Width = CueList.ClientSize.Width };
            sp.Paint += Highlight_Paint;
            CueList.Controls.Add(ps, 0, rowIndex);
            CueList.Controls.Add(sp, 0, rowIndex + 1);
            CueList.ResumeLayout();
            return ps;
        }

        private void RemovePlaystrip(int cueIndex)
        {
            int rowIndex = TableRowFromCueIndex(cueIndex);
            CueList.SuspendLayout();
            PlayStrip removedPs = CueList.GetControlFromPosition(0, rowIndex) as PlayStrip;
            if (removedPs != null)
            {
                removedPs.Stop();
                _playingSounds.Remove(removedPs);
                removedPs.ReportStatus -= Ps_ReportStatus;
                removedPs.DeleteCue -= Ps_DeleteCue;
                removedPs.AddCueBefore -= Ps_AddCueBefore;
                FocusUntrackLowestControls(removedPs);
            }
            CueList.Controls.Remove(CueList.GetControlFromPosition(0, rowIndex));
            CueList.Controls.Remove(CueList.GetControlFromPosition(0, rowIndex + 1));
            foreach (Control ctl in CueList.Controls)
            {
                if (CueList.GetRow(ctl) >= rowIndex)
                {
                    CueList.SetRow(ctl, CueList.GetRow(ctl) - 2);
                    PlayStrip ps = ctl as PlayStrip;
                    if (ps != null)
                    {
                        ps.PlayStripIndex -= 1;
                    }
                }
            }
            CueList.RowCount -= 2;
            CueList.ResumeLayout();
        }

        private static int TableRowFromCueIndex(int rowIndex)
        {
            return 2 * (rowIndex) + 2;  //+2 is spacers at top
        }

        private void StopAll(object sender, EventArgs e)
        {
            foreach (PlayStrip Player in CueList.Controls.OfType<PlayStrip>())
            {
                Player.StopOthers(sender, e);
            }
        }

        /// <summary>
        /// Stop all currently playing sounds except the one that requested the stop.
        /// Uses the tracked _playingSounds set for efficiency.
        /// </summary>
        private void StopAllPlayingSounds(PlayStrip except)
        {
            // Snapshot the set to avoid modifying it while iterating
            var toStop = _playingSounds.Where(ps => ps != except && !ps.IsDisposed).ToList();
            foreach (var ps in toStop)
            {
                ps.Stop();
            }
        }

        private void StopPreviews(object sender, EventArgs e)
        {
            foreach (PlayStrip Player in CueList.Controls.OfType<PlayStrip>())
            {
                Player.StopPreviews(sender, e);
            }
        }

        private void PreloadAll()
        {
            Cursor prev = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            foreach (PlayStrip Player in CueList.Controls.OfType<PlayStrip>())
            {
                Player.PreloadFile();
            }
            Cursor.Current = prev;
        }

        /// <summary>
        /// Add invisible CueList controls to set the correct scroll limits
        /// </summary>
        private void PadCueList()
        {
            Debug.WriteLine("PadCueList");
            if (CueList.GetControlFromPosition(0, 0) == null) return;
            CueList.GetControlFromPosition(0, 0).Height = TOPGAP - SpacerControlHeight;
            CueList.GetControlFromPosition(0, CueList.RowCount - 1).Height = CueList.ClientSize.Height - TOPGAP;
            //BottomPlaceholders = CueList.Height / (cueListSpacing) - TOP_PLACEHOLDERS + 1;
            //while (paddedTop > TOP_PLACEHOLDERS) {
            //    RemovePlaystrip(TOPPLACEHOLDER);
            //    paddedTop--;
            //}
            //while (paddedTop < TOP_PLACEHOLDERS) {
            //    AddPlaystrip(null, TOPPLACEHOLDER);
            //    paddedTop++;
            //}
            //while (paddedBottom > BottomPlaceholders) {
            //    RemovePlaystrip(BOTTOMPLACEHOLDER);
            //    paddedBottom--;
            //}
            //while (paddedBottom < BottomPlaceholders) {
            //    AddPlaystrip(null, BOTTOMPLACEHOLDER);
            //    paddedBottom++;
            //}

            //CueList.VerticalScroll.Enabled = true;
            //CueList.VerticalScroll.Visible = true;
            //CueList.VerticalScroll.Minimum = 0;
            //CueList.VerticalScroll.Maximum = (CueList.Controls.Count - TOP_PLACEHOLDERS - BottomPlaceholders) * cueListSpacing - 1;
            //CueList.VerticalScroll.LargeChange = cueListSpacing;
            //CueList.VerticalScroll.SmallChange = cueListSpacing;
            //CueList.AutoScrollOffset = new Point(0, TOP_PLACEHOLDERS * cueListSpacing);
            foreach (Control ctl in CueList.Controls)
            {
                //Debug.WriteLine(ctl.ToString());
            }

            //UpdateDisplayedIndexes();
        }

        private void CueList_Scroll(object sender, ScrollEventArgs e)
        {
            Debug.WriteLine("Scroll");
            Debug.WriteLine("Type " + e.Type);
            Debug.WriteLine("Orientation " + e.ScrollOrientation);
            Debug.WriteLine("Old=" + e.OldValue);
            Debug.WriteLine("New=" + e.NewValue);
            Debug.WriteLine("Min " + CueList.VerticalScroll.Minimum);
            Debug.WriteLine("Max " + CueList.VerticalScroll.Maximum);
            Debug.WriteLine("Lg " + CueList.VerticalScroll.LargeChange);
            Debug.WriteLine("Sm " + CueList.VerticalScroll.SmallChange);
            //((System.Windows.Forms.FlowLayoutPanel..ScrollBar)sender).Value = e.NewValue;
            //ReportStatus(
            //    "Type " + e.Type +
            //    ", Orientation " + e.ScrollOrientation +
            //    ", Old=" + e.OldValue +
            //    ", New=" + e.NewValue +
            //    ", Min " + CueList.AutoScrollPosition.Minimum +
            //    ", Max " + CueList.AutoScrollPosition.Maximum +
            //    ", Lg " + CueList.AutoScrollPosition.LargeChange +
            //    ", Sm " + CueList.AutoScrollPosition.SmallChange
            //);
            if (e.NewValue > e.OldValue)
            {
                e.NewValue = ((e.NewValue /*+ cueListSpacing/2*/) / CueListSpacing) * CueListSpacing;
            }
            else
            {
                e.NewValue = ((e.NewValue /*+ cueListSpacing / 2*/) / CueListSpacing) * CueListSpacing;
            }
            CueList.VerticalScroll.Value = Math.Min(CueList.VerticalScroll.Maximum, e.NewValue);
            NextPlayCueChanged();
            //ReportStatus("Scrolled to " + e.NewValue.ToString("D"));
        }

        private void bnStopAll_Click(object sender, EventArgs e)
        {
            StopAll(sender, e);
            StopPreviews(sender, e);
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            // Process queued commands (from WebSocket or cross-thread callers) on the UI thread
            while (_commandQueue.TryDequeue(out Action action))
            {
                action();
            }

            PlayStrip playingStrip = null;
            foreach (Control ctl in CueList.Controls)
            {
                if (ctl.GetType() == typeof(PlayStrip))
                {
                    PlayStrip ps = (PlayStrip)ctl;
                    if (ps.IsPlaying)
                    {
                        ps.ProgressUpdate(sender, e);
                        playingStrip = ps;
                    }
                }
            }

            UpdateProgressDisplay(playingStrip);
        }

        private bool _wasPlaying = false;

        private void UpdateProgressDisplay(PlayStrip playingStrip)
        {
            if (playingStrip != null)
            {
                _wasPlaying = true;
                double duration = playingStrip.PlaybackLength.TotalSeconds;
                double position = playingStrip.PlaybackPosition.TotalSeconds;
                double remaining = Math.Max(0, duration - position);

                if (duration > 0)
                {
                    playbackProgressBar.Value = (int)(position / duration * 1000);
                }
                else
                {
                    playbackProgressBar.Value = 0;
                }

                playbackTimeLabel.Text = string.Format("{0} / -{1}",
                    FormatTime(position), FormatTime(remaining));

                UpdateTrackInfoLabel(playingStrip);
                UpdateWebAppProgress(position, duration);
            }
            else
            {
                // Reset form progress bar
                playbackProgressBar.Value = 0;
                double cueFileDuration = (NextPlayCue?.PlaybackLength ?? TimeSpan.Zero).TotalSeconds;
                playbackTimeLabel.Text = cueFileDuration > 0
                    ? string.Format("0:00 / {0}", FormatTime(cueFileDuration))
                    : "0:00 / 0:00";
                UpdateTrackInfoLabel(null);

                // Push a single web reset when transitioning from playing to stopped
                if (_wasPlaying)
                {
                    _wasPlaying = false;
                    UpdateWebApp();
                }
            }
        }

        private void UpdateTrackInfoLabel(PlayStrip activeStrip)
        {
            PlayStrip cue = activeStrip ?? NextPlayCue;
            if (cue == null || string.IsNullOrEmpty(cue.SFX.FileName))
            {
                trackInfoLabel.Text = "";
                trackInfoLabel.ToolTipText = "";
                return;
            }
            string filePath = cue.SFX.FileName;
            double dur = cue.PlaybackLength.TotalSeconds;
            string durStr = dur > 0 ? FormatTime(dur) : "?";
            string speedStr = Math.Abs(cue.SFX.Speed - 1.0f) > 0.01f ? $" @{cue.SFX.Speed:0.00}x" : "";
            trackInfoLabel.Text = string.Format("{0} | {1}{2}", Path.GetFileName(filePath), durStr, speedStr);
            trackInfoLabel.ToolTipText = filePath;
        }

        /// <summary>
        /// Builds the formatted track info string for the current (next) cue,
        /// matching the desktop trackInfoLabel format: "filename | duration @speedx"
        /// </summary>
        private static string BuildTrackInfoString(PlayStrip cue)
        {
            if (cue == null || string.IsNullOrEmpty(cue.SFX.FileName))
                return "";
            double dur = cue.PlaybackLength.TotalSeconds;
            string durStr = dur > 0 ? FormatTime(dur) : "?";
            string speedStr = Math.Abs(cue.SFX.Speed - 1.0f) > 0.01f ? $" @{cue.SFX.Speed:0.00}x" : "";
            return $"{Path.GetFileName(cue.SFX.FileName)} | {durStr}{speedStr}";
        }

        private static string FormatTime(double totalSeconds)
        {
            int mins = (int)(totalSeconds / 60);
            int secs = (int)(totalSeconds % 60);
            return string.Format("{0}:{1:D2}", mins, secs);
        }

        private void UpdateWebAppProgress(double positionSeconds, double durationSeconds)
        {
            PlayStrip next = NextPlayCue;
            PlayStrip prev = PrevPlayCue;
            DisplaySettings disp = new DisplaySettings()
            {
                Title = Text,
                PrevMainText = rtPrevMainText.Text,
                MainText = rtMainText.Text,
                TrackName = Path.GetFileName(next?.SFX.FileName),
                TrackInfo = BuildTrackInfoString(next),
                TrackPositionSeconds = positionSeconds,
                TrackDurationSeconds = durationSeconds,
                CurrentVolume = next?.SFX.Volume ?? 50,
                CurrentSpeed = next?.SFX.Speed ?? 1.0f,
                StopOthers = next?.SFX.StopOthers ?? false,
                CueNumber = next != null ? (next.PlayStripIndex + 1).ToString("D3") : "",
                CueDescription = next?.SFX.Description ?? "",
                CueFileName = Path.GetFileName(next?.SFX.FileName ?? ""),
                CueAutoRun = next?.SFX.AutoPlay ?? false,
                CuePauseSeconds = (next?.SFX.AutoPlayPauseMs ?? 0) / 1000.0,
                CueFadeInMs = next?.SFX.FadeInDurationMs ?? 0,
                CueFadeOutMs = next?.SFX.FadeOutDurationMs ?? 0,
                CueFadeCurve = (next?.SFX.FadeCurve ?? classes.FadeCurve.Linear) == classes.FadeCurve.Logarithmic ? "Logarithmic" : "Linear",
                PrevCueNumber = prev != null ? (prev.PlayStripIndex + 1).ToString("D3") : "",
                PrevCueDescription = prev?.SFX.Description ?? "",
                PrevCueFileName = Path.GetFileName(prev?.SFX.FileName ?? "")
            };
            OnDisplayChanged(disp);
        }

        private void bnPlayNext_Click(object sender, EventArgs e)
        {
            // Stop all currently playing sounds (not just the visual "previous" cue —
            // the cue order may have changed while sounds were playing)
            StopAllPlayingSounds(null);
            if (NextPlayCue != null)
            {
                NextPlayCue.Play();
                NextPlayCueIndex += 1;
            }
        }

        private void bnAddCue_Click(object sender, EventArgs e)
        {
            int newPosition = Math.Min(NextPlayCueIndex, CurrentShow.Cues.Count);
            SFX sfx = new SFX();
            InsertPlaystrip(sfx, newPosition);
            PadCueList();
            //Add to the show once the controls are in place so they can be updated
            CurrentShow.AddCue(sfx, newPosition);
            NextPlayCueChanged();
        }

        private void bnDeleteCue_Click(object sender, EventArgs e)
        {
            PlayStrip ps = NextPlayCue;
            if (ps == null) return;
            if (Settings.Default.ConfirmDeleteCue)
            {
                DialogResult Response = MessageBox.Show(string.Format("Delete Cue {0}?\r\n{1}", ps.PlayStripIndex + 1, ps.SFX.Description), "Cue List", MessageBoxButtons.YesNo);
                if (Response != DialogResult.Yes) return;
            }
            RemovePlaystrip(NextPlayCueIndex);
            CurrentShow.RemoveCue(ps.SFX);
            PadCueList();
            NextPlayCueChanged();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowFileHandler.CheckSave(CurrentShow) != DialogResult.OK) return;
            FileNew();
            //NextPlayCueChanged();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppLogger.Info($"SFXPlayer.Form1_FormClosing: reason={e.CloseReason}");
            if (e.CloseReason == CloseReason.TaskManagerClosing) return;    //allow close from task manager
            if (ShowFileHandler.CheckSave(CurrentShow) != DialogResult.OK)
            {
                e.Cancel = true;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFileHandler.Save(CurrentShow);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowFileHandler.SaveAs(CurrentShow);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ShowFileHandler.CheckSave(CurrentShow) != DialogResult.OK) return;
            FileOpen();
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            ScrollTimer.Enabled = false;
            CueList.VerticalScroll.Value = CueList.VerticalScroll.Value / (CueListSpacing) * (CueListSpacing);
            NextPlayCueChanged();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) return;
            Debug.WriteLine("CueList_Resize");
            //cuelistrows = (Height - cuelistFormSpacing) / cueListSpacing;
            //CueList.Height = cuelistrows * cueListSpacing - 8;
            //this.statusBar.Panels[0].Text = "NumberOfPlaceholders = " + (BottomPlaceholders + TOP_PLACEHOLDERS).ToString();
            PadCueList();

            rtMainText.Height = Math.Min(statusStrip.Top - rtMainText.Margin.Bottom - rtMainText.Top, rtPrevMainText.Height);
            pictureBox1.Top = bnPlayNext.Top - pictureBox1.Height;
            pictureBox2.Top = bnPlayNext.Bottom;
        }

        private void CueList_ClientSizeChanged(object sender, EventArgs e)
        {

            foreach (PlayStrip ctl in CueList.Controls.OfType<PlayStrip>())
            {
                ctl.Width = CueList.ClientSize.Width - 1;
            }
            foreach (Spacer ctl in CueList.Controls.OfType<Spacer>())
            {
                ctl.Width = CueList.ClientSize.Width - 1;
            }
            //Debug.WriteLine("Client size changed {0}x{1}", CueList.Width, CueList.Height);
        }

        private void rtMainText_TextChanged(object sender, EventArgs e)
        {
            NextPlayCue.SFX.MainText = rtMainText.Text;
        }

        private void rtPrevMainText_TextChanged(object sender, EventArgs e)
        {
            PrevPlayCue.SFX.MainText = rtPrevMainText.Text;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void exportShowFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string archiveFile = CurrentShow.CreateArchive(ShowFileHandler.CurrentFileName);
            SaveFileDialog sfdArch = new SaveFileDialog();
            sfdArch.FileName = Path.GetFileName(archiveFile);
            if (Directory.Exists(Settings.Default.ArchiveFolder))
            {
                sfdArch.InitialDirectory = Settings.Default.ArchiveFolder;
            }
            else
            {
                sfdArch.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            if (sfdArch.ShowDialog() == DialogResult.OK)
            {
                File.Move(archiveFile, sfdArch.FileName);
                Settings.Default.ArchiveFolder = Path.GetDirectoryName(sfdArch.FileName);
                Settings.Default.Save();
            }
            else
            {
                File.Delete(archiveFile);
            }
        }

        private void importShowFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //choose file
            if (ShowFileHandler.CheckSave(CurrentShow) != DialogResult.OK) return;
            OpenFileDialog ofdArch = new OpenFileDialog();
            if (Directory.Exists(Settings.Default.ArchiveFolder))
            {
                ofdArch.InitialDirectory = Settings.Default.ArchiveFolder;
            }
            else
            {
                ofdArch.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            ofdArch.Filter = "Show Archive|*.show|All files|*.*";
            ofdArch.FileName = "";
            ofdArch.Title = "Choose show archive to extract";
            if (ofdArch.ShowDialog() == DialogResult.OK)
            {
                //choose where to put it
                FolderBrowserDialog fbdArchive = new FolderBrowserDialog();
                fbdArchive.ShowNewFolderButton = true;
                if (!string.IsNullOrEmpty(Settings.Default.LastProjectFolder))
                {
                    fbdArchive.SelectedPath = new FileInfo(Path.GetDirectoryName(Settings.Default.LastProjectFolder)).Directory.FullName;
                }
                else
                {
                    fbdArchive.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }
                fbdArchive.Description = "Please choose a folder for the show";
                while (fbdArchive.ShowDialog() == DialogResult.OK)
                {
                    string ShowFolder = fbdArchive.SelectedPath;
                    if (new DirectoryInfo(ShowFolder).GetFiles().Count() != 0)
                    {
                        MessageBox.Show("The chosen folder is not empty, please choose another one", "Open show archive");
                        continue;
                        //"Files found = " + new DirectoryInfo(ShowFolder).GetFiles().Count() + ". Creating show folder");
                        //ShowFolder = Path.Combine(ShowFolder, Path.GetFileNameWithoutExtension(ofdArch.FileName));
                        //Directory.CreateDirectory(ShowFolder);
                    }
                    string ExtractedShow = global::SFXPlayer.classes.Show.ExtractArchive(ofdArch.FileName, ShowFolder);
                    if (!string.IsNullOrEmpty(ExtractedShow) && File.Exists(ExtractedShow))
                    {
                        ReportStatus("Show extracted to " + ExtractedShow);
                        FileOpen(ExtractedShow);
                    }
                    break;
                }
            }
        }
        private void createSampleProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if current show needs to be saved
                if (ShowFileHandler.CheckSave(CurrentShow) != DialogResult.OK)
                {
                    return;
                }

                // Create the SFXPlayer folder in Documents
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string sfxPlayerFolder = Path.Combine(documentsPath, "SFXPlayer");

                if (!Directory.Exists(sfxPlayerFolder))
                {
                    Directory.CreateDirectory(sfxPlayerFolder);
                }

                // Create the sample project
                string sampleFilePath = Path.Combine(sfxPlayerFolder, "Sample.sfx");

                // Check if file already exists
                if (File.Exists(sampleFilePath))
                {
                    var result = MessageBox.Show(
                        "A sample project already exists. Do you want to overwrite it?",
                        "Sample Project Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }

                ReportStatus("Creating sample project...");

                // Create a new show with default sounds
                var sampleShow = classes.Show.CreateDefaultShow();

                // Save the sample show
                XMLFileHandler<Show>.UntrackedSave(sampleShow, sampleFilePath);

                ReportStatus("Sample project created successfully");

                // Ask if user wants to open it
                var openResult = MessageBox.Show(
                    $"Sample project created successfully at:\n{sampleFilePath}\n\nWould you like to open it now?",
                    "Sample Project Created",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (openResult == DialogResult.Yes)
                {
                    // Load the sample show
                    Show newShow = ShowFileHandler.LoadFromFile(sampleFilePath);
                    if (newShow != null)
                    {
                        int tempNextPlayCueIndex = newShow.NextPlayCueIndex;
                        CurrentShow = newShow;
                        NextPlayCueIndex = tempNextPlayCueIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("SFXPlayer.createSampleProject: failed", ex);
                MessageBox.Show(
                    $"Error creating sample project:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                ReportStatus($"Error creating sample project: {ex.Message}");
                Debug.WriteLine($"Error creating sample project: {ex}");
            }
        }

        internal void PlayNextCue()
        {
            AppLogger.Info("SFXPlayer.PlayNextCue");
            _commandQueue.Enqueue(() => bnPlayNext_Click(null, null));
        }

        internal void StopAll()
        {
            AppLogger.Info("SFXPlayer.StopAll");
            _commandQueue.Enqueue(() => bnStopAll_Click(null, null));
        }

        internal void PreviousCue()
        {
            AppLogger.Info("SFXPlayer.PreviousCue");
            _commandQueue.Enqueue(() => bnPrev_Click(null, null));
        }

        internal void NextCue()
        {
            AppLogger.Info("SFXPlayer.NextCue");
            _commandQueue.Enqueue(() => bnNext_Click(null, null));
        }

        internal void SetNextCueVolume(int vol)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.Volume = Math.Max(0, Math.Min(100, vol));
                }
            });
        }

        internal void SetNextCueSpeed(float speed)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.Speed = Math.Max(0.1f, Math.Min(20.0f, speed));
                }
            });
        }

        internal void DeleteNextCue()
        {
            _commandQueue.Enqueue(() =>
            {
                PlayStrip ps = NextPlayCue;
                if (ps == null) return;
                SFX sfxToRemove = ps.SFX;
                RemovePlaystrip(ps.PlayStripIndex);
                CurrentShow.RemoveCue(sfxToRemove);
                PadCueList();
                NextPlayCueChanged();
            });
        }

        internal void SetNextCueAutoRun(bool enabled)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.AutoPlay = enabled;
                    UpdateWebApp();
                }
            });
        }

        internal void SetNextCuePauseSeconds(double seconds)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.AutoPlayPauseMs = (int)Math.Round(Math.Max(0, Math.Min(60, seconds)) * 1000);
                    UpdateWebApp();
                }
            });
        }

        internal void SetNextCueFadeIn(int ms)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.FadeInDurationMs = Math.Max(0, ms);
                    UpdateWebApp();
                }
            });
        }

        internal void SetNextCueFadeOut(int ms)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.FadeOutDurationMs = Math.Max(0, ms);
                    UpdateWebApp();
                }
            });
        }

        internal void SetNextCueFadeCurve(string curve)
        {
            _commandQueue.Enqueue(() =>
            {
                if (NextPlayCue != null)
                {
                    NextPlayCue.SFX.FadeCurve = string.Equals(curve, "log", StringComparison.OrdinalIgnoreCase)
                        ? classes.FadeCurve.Logarithmic
                        : classes.FadeCurve.Linear;
                    UpdateWebApp();
                }
            });
        }

        // Drag & drop helpers
        Control LastHovered;
        Color LastHoveredColor;
        bool ReplaceOK;
        bool AddOK;
        bool AddZone;
        bool ReplaceZone;

        private void HighlightControl(Control ctl)
        {
            if (LastHovered == ctl) return;
            UnHighlightControl();
            if (ctl == null) return;
            LastHovered = ctl;
            LastHoveredColor = LastHovered.BackColor;
            LastHovered.BackColor = SystemColors.Highlight;
        }

        private void UnHighlightControl()
        {
            if (LastHovered != null)
            {
                LastHovered.BackColor = LastHoveredColor;
                LastHovered = null;
            }
        }

        private Point ScreenToChild(Point pt, Control child)
        {
            return new Point(
                pt.X - RectangleToScreen(ClientRectangle).Left - child.Left,
                pt.Y - RectangleToScreen(ClientRectangle).Top - child.Top);
        }

        private bool CheckAllFilesAreAudio(string[] files)
        {
            foreach (string file in files)
            {
                bool fileOK = false;
                foreach (string filter in filters)
                {
                    if (Path.GetExtension(file).ToUpper() == filter)
                    {
                        fileOK = true;
                        break;
                    }
                }
                if (!fileOK) return false;
            }
            return true;
        }
    }
}
