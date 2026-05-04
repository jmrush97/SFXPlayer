using static SFXPlayer.classes.SVGResources;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Midi;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
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
using System.Reflection;
using System.Runtime.InteropServices;
using Svg.FilterEffects;
using SFXPlayer.classes;
using SFXPlayer.forms;

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

        /// <summary>
        /// The PlayStrip most recently started by the main transport (Go button / bnPlayNext_Click).
        /// Kept separately from _playingSounds so that Previous and Next can stop exactly this
        /// track when the user navigates, preventing two tracks from playing simultaneously.
        /// Cleared automatically via the PlayingStateChanged handler when the strip stops.
        /// </summary>
        private PlayStrip _currentActiveTrack = null;

        // Debounce timers for web-interface speed/volume slider commands.
        // The web slider fires 'oninput' on every pixel of movement, which would otherwise
        // enqueue many back-to-back SetSpeedLive calls that corrupt the _isSeeking guard
        // and trigger spurious PlaybackStopped → TriggerAutoPlay cue-cycling.
        private System.Windows.Forms.Timer _webSpeedDebounceTimer;
        private System.Windows.Forms.Timer _webVolumeDebounceTimer;
        private float _pendingWebSpeed = -1f;
        private int   _pendingWebVolume = -1;

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

        /// <summary>
        /// Returns the directory of the currently-open .sfx file, or the application directory if
        /// no file is open. Used when copying audio files from a different drive.
        /// </summary>
        public string ShowDirectory
        {
            get
            {
                string sfxFile = ShowFileHandler.CurrentFileName;
                if (!string.IsNullOrEmpty(sfxFile) && File.Exists(sfxFile))
                    return Path.GetDirectoryName(sfxFile);
                return Path.GetDirectoryName(Application.ExecutablePath);
            }
        }

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
            // Keep web-app green regardless of the legacy colour setting
            bnPlayNext.BackColor = System.Drawing.Color.FromArgb(39, 174, 96);
            PlayStrip.OFD = dlgOpenAudioFile;
            autoLoadLastsfxCuelistToolStripMenuItem.Checked = Settings.Default.AutoLoadLastSession;
            confirmDeleteCueToolStripMenuItem.Checked = Settings.Default.ConfirmDeleteCue;

            // Web-interface debounce timers: coalesce rapid slider commands so only
            // one player reload fires per 250ms burst of web speed/volume changes.
            _webSpeedDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _webSpeedDebounceTimer.Tick += WebSpeedDebounceTimer_Tick;
            _webVolumeDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _webVolumeDebounceTimer.Tick += WebVolumeDebounceTimer_Tick;
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
                Point pt = new Point(0, TOPGAP - CueListSpacing);
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
                Point pt = new Point(0, TOPGAP);
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
            PlayStrip playing = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && ps.IsPlaying);
            PlayStrip paused = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && ps.IsPaused);
            // When audio is active (playing or paused) the display shows that cue; otherwise the next cue
            PlayStrip active = playing ?? paused;
            PlayStrip displayCue = active ?? next;
            PlayStrip nextNext = GetNextNextCue(next);
            bool isLoading = CueList.Controls.OfType<PlayStrip>().Any(ps => ps.IsLoading);
            DisplaySettings disp = new DisplaySettings()
            {
                Title = Text,
                PrevMainText = rtPrevMainText.Text,
                MainText = displayCue?.SFX.MainText ?? rtMainText.Text,
                TrackName = Path.GetFileName(displayCue?.SFX.FileName),
                TrackInfo = BuildTrackInfoString(displayCue),
                TrackDurationSeconds = active?.PlaybackLength.TotalSeconds ?? (next?.PlaybackLength.TotalSeconds ?? 0.0),
                CurrentVolume = displayCue?.SFX.Volume ?? 50,
                CurrentSpeed = displayCue?.SFX.Speed ?? 1.0f,
                StopOthers = displayCue?.SFX.StopOthers ?? false,
                CueNumber = displayCue != null ? (displayCue.PlayStripIndex + 1).ToString("D3") : "",
                CueDescription = displayCue?.SFX.Description ?? "",
                CueFileName = Path.GetFileName(displayCue?.SFX.FileName ?? ""),
                CueAutoRun = next?.SFX.AutoPlay ?? false,
                CuePauseSeconds = (next?.SFX.AutoPlayPauseMs ?? 0) / 1000.0,
                CueFadeInMs = next?.SFX.FadeInDurationMs ?? 0,
                CueFadeOutMs = next?.SFX.FadeOutDurationMs ?? 0,
                CueFadeCurve = (next?.SFX.FadeCurve ?? classes.FadeCurve.Linear) == classes.FadeCurve.Logarithmic ? "Logarithmic" : "Linear",
                PrevCueNumber = prev != null ? (prev.PlayStripIndex + 1).ToString("D3") : "",
                PrevCueDescription = prev?.SFX.Description ?? "",
                PrevCueFileName = Path.GetFileName(prev?.SFX.FileName ?? ""),
                IsPlaying = playing != null,
                IsPaused = paused != null && playing == null,
                PlayingVolume = active?.SFX.Volume ?? (next?.SFX.Volume ?? 50),
                PlayingSpeed = active?.SFX.Speed ?? (next?.SFX.Speed ?? 1.0f),
                PlayingFadeGain = active?.CurrentFadeGain ?? 1.0f,
                AvailablePlaybackDevices = string.Join("|", CurrentAudioOutDevices),
                CurrentPlaybackDevice = Settings.Default.LastPlaybackDevice ?? "",
                AvailablePreviewDevices = string.Join("|", CurrentAudioOutDevices),
                CurrentPreviewDevice = Settings.Default.LastPreviewDevice ?? "",
                WaveformData = GetWaveformData(displayCue),
                CueListJson = GetCueListJson(),
                IsLoading = isLoading,
                GoTrackNum = next != null ? (next.PlayStripIndex + 1).ToString("D3") : "",
                GoTrackDesc = next?.SFX.Description ?? "",
                ActiveTrackNum = active != null ? (active.PlayStripIndex + 1).ToString("D3") : "",
                ActiveTrackDesc = active?.SFX.Description ?? "",
                NextNextTrackNum = nextNext != null ? (nextNext.PlayStripIndex + 1).ToString("D3") : "",
                NextNextTrackDesc = nextNext?.SFX.Description ?? "",
                ShowDescription = CurrentShow?.Description ?? "",
                LastSaveUser = CurrentShow?.History?.LastOrDefault()?.User ?? "",
                LastSaveTimestamp = CurrentShow?.History?.LastOrDefault()?.Timestamp.ToString("o") ?? "",
                LastSaveReason = CurrentShow?.History?.LastOrDefault()?.Reason ?? "",
                SaveHistoryJson = BuildSaveHistoryJson(CurrentShow),
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
            UpdateRecentAudioFilesMenu();

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
            InitialisingDevices = true;

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
            UpdateDeviceStatusDisplay();  // Add this line
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
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            if (ver != null)
                Title += $" v{ver.Major}.{ver.Minor}.{ver.Build}";
            try
            {
                var buildDate = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);
                Title += $" (Built {buildDate:yyyy-MM-dd})";
            }
            catch { }
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
                RestoreDevicesFromShow(newShow);
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
                RestoreDevicesFromShow(CurrentShow);
            }
            else
            {
                CurrentShow = oldShow;
            }
            CurrentShow.ShowFileBecameDirty += ShowFileHandler.SetDirty;
        }

        /// <summary>
        /// If the loaded show file stored preferred device names, restore them when
        /// those devices are still available on this machine.
        /// </summary>
        private void RestoreDevicesFromShow(Show show)
        {
            if (show == null) return;
            if (!string.IsNullOrEmpty(show.PlaybackDevice) && CurrentAudioOutDevices.Contains(show.PlaybackDevice))
            {
                Settings.Default.LastPlaybackDevice = show.PlaybackDevice;
                Settings.Default.Save();
            }
            if (!string.IsNullOrEmpty(show.PreviewDevice) && CurrentAudioOutDevices.Contains(show.PreviewDevice))
            {
                Settings.Default.LastPreviewDevice = show.PreviewDevice;
                Settings.Default.Save();
            }
            if (!string.IsNullOrEmpty(show.PlaybackDevice) || !string.IsNullOrEmpty(show.PreviewDevice))
                UpdateDevices();
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
                else
                {
                    _playingSounds.Remove(strip);
                    if (_currentActiveTrack == strip)
                        _currentActiveTrack = null;
                }
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
            ps.CueChanged += (s, e) => _commandQueue.Enqueue(() => { UpdateWebApp(); UpdateRecentAudioFilesMenu(); });
            ps.CueSelected += (s, e) => GotoCue((s as PlayStrip)?.PlayStripIndex ?? -1);
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
            // Stop all strips unconditionally (playing and paused). Using a direct loop
            // over all PlayStrips is more robust than relying on the _playingSounds set,
            // which may not reflect paused strips or strips mid-speed-change.
            foreach (PlayStrip ps in CueList.Controls.OfType<PlayStrip>())
            {
                if (!ps.IsDisposed)
                    ps.Stop();
            }
            StopPreviews(sender, e);
            bnPause.Text = "Pause";
        }

        private void bnPause_Click(object sender, EventArgs e)
        {
            TogglePause();
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            // Process queued commands (from WebSocket or cross-thread callers) on the UI thread
            while (_commandQueue.TryDequeue(out Action action))
            {
                action();
            }

            // Drain each PlayStrip's player-action queue so that all button/slider/file-load
            // operations run on the UI thread AFTER any pending NAudio PlaybackStopped
            // callbacks (posted via SynchronizationContext) have been processed.
            foreach (Control ctl in CueList.Controls)
            {
                if (ctl is PlayStrip ps)
                    ps.DrainPlayerQueue();
            }

            PlayStrip playingStrip = null;
            PlayStrip pausedStrip = null;
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
                    else if (ps.IsPaused && pausedStrip == null)
                    {
                        pausedStrip = ps;
                    }
                }
            }

            UpdateProgressDisplay(playingStrip, pausedStrip);
        }

        private bool _wasPlaying = false;
        private bool _wasPaused = false;

        private void UpdateProgressDisplay(PlayStrip playingStrip, PlayStrip pausedStrip = null)
        {
            PlayStrip activeStrip = playingStrip ?? pausedStrip;
            bool isPaused = (playingStrip == null && pausedStrip != null);

            if (activeStrip != null)
            {
                double duration = activeStrip.PlaybackLength.TotalSeconds;
                double position = activeStrip.PlaybackPosition.TotalSeconds;
                double remaining = Math.Max(0, duration - position);
                try
                {
                    if (duration > 0)
                    {
                        playbackProgressBar.Value = (int)(position / duration * 1000);
                    }
                    else
                    {
                        playbackProgressBar.Value = 0;
                    }
                } catch (ArgumentOutOfRangeException) {
                    // In case of any timing issues, just reset the progress bar to 0
                    playbackProgressBar.Value = 0;
                } catch (Exception ex) {
                    // Log any unexpected exceptions without crashing the app
                    AppLogger.Error($"Error updating progress bar position={position}, duration={duration}", ex);
                }

                playbackTimeLabel.Text = string.Format("{0} / -{1}",
                    FormatTime(position), FormatTime(remaining));

                UpdateTrackInfoLabel(activeStrip);

                if (isPaused)
                {
                    // When paused, send a single web update on transition into paused state
                    if (!_wasPaused)
                    {
                        _wasPaused = true;
                        _wasPlaying = false;
                        UpdateWebAppProgress(position, duration);
                    }
                }
                else
                {
                    _wasPlaying = true;
                    _wasPaused = false;
                    UpdateWebAppProgress(position, duration);
                }
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

                // Push a single web reset when transitioning from playing/paused to stopped
                if (_wasPlaying || _wasPaused)
                {
                    _wasPlaying = false;
                    _wasPaused = false;
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

        /// <summary>
        /// Returns the PlayStrip that is one position after <paramref name="next"/> in the cue list,
        /// or null if there is none.
        /// </summary>
        private PlayStrip GetNextNextCue(PlayStrip next)
        {
            if (next == null) return null;
            int targetIdx = next.PlayStripIndex + 1;
            return CueList.Controls.OfType<PlayStrip>()
                .FirstOrDefault(p => p.PlayStripIndex == targetIdx);
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
            PlayStrip playing = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && ps.IsPlaying);
            PlayStrip paused = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && ps.IsPaused);
            PlayStrip active = playing ?? paused;
            // Show the active cue prominently when audio is running; fall back to the next cue
            PlayStrip displayCue = active ?? next;
            PlayStrip nextNext = GetNextNextCue(next);
            bool isLoading = CueList.Controls.OfType<PlayStrip>().Any(ps => ps.IsLoading);
            DisplaySettings disp = new DisplaySettings()
            {
                Title = Text,
                PrevMainText = rtPrevMainText.Text,
                MainText = displayCue?.SFX.MainText ?? rtMainText.Text,
                TrackName = Path.GetFileName(displayCue?.SFX.FileName),
                TrackInfo = BuildTrackInfoString(displayCue),
                TrackPositionSeconds = positionSeconds,
                TrackDurationSeconds = durationSeconds,
                CurrentVolume = displayCue?.SFX.Volume ?? 50,
                CurrentSpeed = displayCue?.SFX.Speed ?? 1.0f,
                StopOthers = displayCue?.SFX.StopOthers ?? false,
                CueNumber = displayCue != null ? (displayCue.PlayStripIndex + 1).ToString("D3") : "",
                CueDescription = displayCue?.SFX.Description ?? "",
                CueFileName = Path.GetFileName(displayCue?.SFX.FileName ?? ""),
                CueAutoRun = next?.SFX.AutoPlay ?? false,
                CuePauseSeconds = (next?.SFX.AutoPlayPauseMs ?? 0) / 1000.0,
                CueFadeInMs = next?.SFX.FadeInDurationMs ?? 0,
                CueFadeOutMs = next?.SFX.FadeOutDurationMs ?? 0,
                CueFadeCurve = (next?.SFX.FadeCurve ?? classes.FadeCurve.Linear) == classes.FadeCurve.Logarithmic ? "Logarithmic" : "Linear",
                PrevCueNumber = prev != null ? (prev.PlayStripIndex + 1).ToString("D3") : "",
                PrevCueDescription = prev?.SFX.Description ?? "",
                PrevCueFileName = Path.GetFileName(prev?.SFX.FileName ?? ""),
                IsPlaying = playing != null,
                IsPaused = paused != null && playing == null,
                PlayingVolume = active?.SFX.Volume ?? (next?.SFX.Volume ?? 50),
                PlayingSpeed = active?.SFX.Speed ?? (next?.SFX.Speed ?? 1.0f),
                PlayingFadeGain = active?.CurrentFadeGain ?? 1.0f,
                AvailablePlaybackDevices = string.Join("|", CurrentAudioOutDevices),
                CurrentPlaybackDevice = Settings.Default.LastPlaybackDevice ?? "",
                AvailablePreviewDevices = string.Join("|", CurrentAudioOutDevices),
                CurrentPreviewDevice = Settings.Default.LastPreviewDevice ?? "",
                WaveformData = GetWaveformData(displayCue),
                CueListJson = GetCueListJson(),
                IsLoading = isLoading,
                GoTrackNum = next != null ? (next.PlayStripIndex + 1).ToString("D3") : "",
                GoTrackDesc = next?.SFX.Description ?? "",
                ActiveTrackNum = active != null ? (active.PlayStripIndex + 1).ToString("D3") : "",
                ActiveTrackDesc = active?.SFX.Description ?? "",
                NextNextTrackNum = nextNext != null ? (nextNext.PlayStripIndex + 1).ToString("D3") : "",
                NextNextTrackDesc = nextNext?.SFX.Description ?? "",
                ShowDescription = CurrentShow?.Description ?? "",
                LastSaveUser = CurrentShow?.History?.LastOrDefault()?.User ?? "",
                LastSaveTimestamp = CurrentShow?.History?.LastOrDefault()?.Timestamp.ToString("o") ?? "",
                LastSaveReason = CurrentShow?.History?.LastOrDefault()?.Reason ?? "",
                SaveHistoryJson = BuildSaveHistoryJson(CurrentShow),
            };
            OnDisplayChanged(disp);
        }

        private void bnPrev_Click(object sender, EventArgs e)
        {
            // Stop the active transport track before navigating to prevent two tracks
            // playing simultaneously when Go is pressed after navigation.
            if (_currentActiveTrack != null && !_currentActiveTrack.IsDisposed)
                _currentActiveTrack.Stop();
            bnPause.Text = "Pause";
            NextPlayCueIndex -= 1;
        }

        private void bnNext_Click(object sender, EventArgs e)
        {
            // Stop the active transport track before navigating to prevent two tracks
            // playing simultaneously when Go is pressed after navigation.
            if (_currentActiveTrack != null && !_currentActiveTrack.IsDisposed)
                _currentActiveTrack.Stop();
            bnPause.Text = "Pause";
            NextPlayCueIndex += 1;
        }

        private void bnPlayNext_Click(object sender, EventArgs e)
        {
            // Stop all currently playing sounds (not just the visual "previous" cue —
            // the cue order may have changed while sounds were playing)
            StopAllPlayingSounds(null);
            if (NextPlayCue != null)
            {
                var cue = NextPlayCue; // capture before index advances
                cue.Play();
                _currentActiveTrack = cue;
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
            StampSaveMetadata(PromptSaveReason());
            ShowFileHandler.Save(CurrentShow);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StampSaveMetadata(PromptSaveReason());
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
            PadCueList();
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

        private void audioMidiDevicesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowDeviceSelectionDialog();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string version = ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}" : "Unknown";
            string buildDate = "Unknown";
            try
            {
                buildDate = File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    .ToString("yyyy-MM-dd HH:mm");
            }
            catch { }
            string gitHub = "https://github.com/jmrush97/SFXPlayer";
            string message = $"SFX Player\n\nVersion: {version}\nBuild Date: {buildDate}\n\nGitHub: {gitHub}";
            MessageBox.Show(message, "About SFX Player", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Rebuilds the "Recent Audio Files" submenu from the stored settings list.
        /// Called after any file selection and on form load.
        /// </summary>
        private void UpdateRecentAudioFilesMenu()
        {
            var recent = Settings.Default.RecentAudioFiles;
            recentAudioFilesToolStripMenuItem.DropDownItems.Clear();
            if (recent == null || recent.Count == 0)
            {
                recentAudioFilesToolStripMenuItem.Visible = false;
                recentAudioFilesSeparator.Visible = false;
                return;
            }
            foreach (string filePath in recent)
            {
                if (string.IsNullOrWhiteSpace(filePath)) continue;
                var item = new ToolStripMenuItem(Path.GetFileName(filePath))
                {
                    ToolTipText = filePath,
                    Tag = filePath
                };
                item.Click += RecentAudioFileMenuItem_Click;
                recentAudioFilesToolStripMenuItem.DropDownItems.Add(item);
            }
            recentAudioFilesToolStripMenuItem.Visible = recentAudioFilesToolStripMenuItem.DropDownItems.Count > 0;
            recentAudioFilesSeparator.Visible = recentAudioFilesToolStripMenuItem.Visible;
        }

        private void RecentAudioFileMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is string filePath)
            {
                PlayStrip target = NextPlayCue;
                if (target == null) return;
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"File not found:\n{filePath}", "Recent Audio Files",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                target.SelectFile(filePath);
                UpdateRecentAudioFilesMenu();
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
            // Debounce: store the pending value and restart the timer so that rapid
            // web slider events collapse into a single player update after 250ms of
            // inactivity. The SFX model and display are updated immediately; only the
            // live _musicPlayer.Volume call is deferred.
            _commandQueue.Enqueue(() =>
            {
                int clamped = Math.Max(0, Math.Min(100, vol));
                var active = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && (ps.IsPlaying || ps.IsPaused));
                if (active != null)
                {
                    // A track is already playing/paused — adjust only that track.
                    // Do NOT touch NextPlayCue so its preset volume is not overwritten.
                    active.SFX.Volume = clamped;
                    active.RefreshVolumeDisplay();
                }
                else if (NextPlayCue != null)
                {
                    // Nothing is playing: adjust the next cue's preset volume.
                    NextPlayCue.SFX.Volume = clamped;
                    NextPlayCue.RefreshVolumeDisplay();
                }
                _pendingWebVolume = clamped;
                _webVolumeDebounceTimer.Stop();
                _webVolumeDebounceTimer.Start();
                UpdateWebApp();
            });
        }

        private void WebVolumeDebounceTimer_Tick(object sender, EventArgs e)
        {
            _webVolumeDebounceTimer.Stop();
            int vol = _pendingWebVolume;
            if (vol < 0) return;
            _pendingWebVolume = -1;
            var active = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && (ps.IsPlaying || ps.IsPaused));
            if (active != null)
                active.SetVolumeLive(vol);
        }

        internal void SetNextCueSpeed(float speed)
        {
            // Debounce: the web slider fires 'oninput' on every pixel, so many speed:X
            // commands arrive in a burst. Without debouncing each one would enqueue a full
            // Stop/Open/Seek/Play cycle; back-to-back cycles break the _isSeeking guard and
            // trigger spurious PlaybackStopped → TriggerAutoPlay → cue cycling. By storing
            // the latest pending value and restarting a 250ms timer, only one SetSpeedLive
            // is ever executed per drag gesture.
            _commandQueue.Enqueue(() =>
            {
                float clamped = Math.Max(0.1f, Math.Min(8.0f, speed));
                var active = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && (ps.IsPlaying || ps.IsPaused));
                if (active != null)
                {
                    // A track is already playing/paused — adjust only that track.
                    // Do NOT touch NextPlayCue so its preset speed is not overwritten.
                    active.SFX.Speed = clamped;
                }
                else if (NextPlayCue != null)
                {
                    // Nothing is playing: adjust the next cue's preset speed.
                    NextPlayCue.SFX.Speed = clamped;
                }
                _pendingWebSpeed = clamped;
                _webSpeedDebounceTimer.Stop();
                _webSpeedDebounceTimer.Start();
                UpdateWebApp();
            });
        }

        private void WebSpeedDebounceTimer_Tick(object sender, EventArgs e)
        {
            _webSpeedDebounceTimer.Stop();
            float speed = _pendingWebSpeed;
            if (speed < 0) return;
            _pendingWebSpeed = -1f;
            var active = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && (ps.IsPlaying || ps.IsPaused));
            if (active != null)
                active.SetSpeedLive(speed);
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
                    NextPlayCue.RefreshWaveform();
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
                    NextPlayCue.RefreshWaveform();
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

        internal void SetPlaybackDevice(string deviceName)
        {
            _commandQueue.Enqueue(() =>
            {
                if (!string.IsNullOrEmpty(deviceName) && CurrentAudioOutDevices.Contains(deviceName))
                {
                    Settings.Default.LastPlaybackDevice = deviceName;
                    Settings.Default.Save();
                    UpdateDevices();
                    UpdateWebApp();
                }
            });
        }

        internal void SetPreviewDevice(string deviceName)
        {
            _commandQueue.Enqueue(() =>
            {
                if (!string.IsNullOrEmpty(deviceName) && CurrentAudioOutDevices.Contains(deviceName))
                {
                    Settings.Default.LastPreviewDevice = deviceName;
                    Settings.Default.Save();
                    UpdateDevices();
                    UpdateWebApp();
                }
            });
        }

        /// <summary>
        /// Updates the show's description from the web or WinForms UI without stamping save history.
        /// </summary>
        internal void SetShowDescription(string text)
        {
            _commandQueue.Enqueue(() =>
            {
                if (CurrentShow == null) return;
                CurrentShow.Description = text ?? "";
                UpdateWebApp();
            });
        }

        /// <summary>
        /// Shows a dialog asking for an optional save reason.
        /// Returns the entered reason (may be empty string) when the user clicks Save,
        /// or <c>null</c> if the user cancels — callers should abort the save when null is returned.
        /// </summary>
        private string PromptSaveReason()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Save";
                dlg.Size = new System.Drawing.Size(420, 160);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl = new Label { Text = "Update reason (optional):", Left = 12, Top = 14, Width = 380, AutoSize = true };
                var txt = new TextBox { Left = 12, Top = 34, Width = 380, Anchor = AnchorStyles.Left | AnchorStyles.Right };
                var btnOk = new Button { Text = "Save", DialogResult = DialogResult.OK, Left = 230, Top = 70, Width = 80 };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 316, Top = 70, Width = 80 };

                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }

        /// <summary>
        /// Stamps device preferences and adds a SaveRecord to the show history.
        /// Call this before every save operation.
        /// </summary>
        private void StampSaveMetadata(string reason)
        {
            if (reason == null) return;    // user cancelled
            if (CurrentShow == null) return;
            CurrentShow.PlaybackDevice = Settings.Default.LastPlaybackDevice ?? "";
            CurrentShow.PreviewDevice = Settings.Default.LastPreviewDevice ?? "";
            CurrentShow.History.Add(new classes.SaveRecord
            {
                User = Environment.UserName,
                Timestamp = DateTime.UtcNow,
                Reason = reason
            });
        }

        /// <summary>
        /// Builds a JSON array of all save history records for the given show.
        /// </summary>
        private static string BuildSaveHistoryJson(classes.Show show)
        {
            if (show?.History == null || show.History.Count == 0) return "[]";
            var sb = new System.Text.StringBuilder("[");
            bool first = true;
            foreach (var r in show.History)
            {
                if (!first) sb.Append(',');
                first = false;
                string u = EscapeJsonString(r.User ?? "");
                string ts = EscapeJsonString(r.Timestamp == DateTime.MinValue ? "" : r.Timestamp.ToString("o"));
                string reason = EscapeJsonString(r.Reason ?? "");
                sb.Append($"{{\"user\":\"{u}\",\"ts\":\"{ts}\",\"reason\":\"{reason}\"}}");
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Opens a dialog that lets the user view and edit the cue list description.
        /// </summary>
        private void showDescriptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Cue List Description";
                dlg.Size = new System.Drawing.Size(520, 440);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.Sizable;
                dlg.MinimumSize = new System.Drawing.Size(360, 320);

                var lblDesc = new Label { Text = "Description:", Left = 12, Top = 10, Width = 200, AutoSize = true };
                var txtDesc = new TextBox
                {
                    Left = 12, Top = 28, Width = 478, Height = 80,
                    Multiline = true, ScrollBars = ScrollBars.Vertical,
                    Text = CurrentShow?.Description ?? "",
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };

                var lblHistory = new Label { Text = "Save History:", Left = 12, Top = 118, Width = 200, AutoSize = true };
                var lst = new ListView
                {
                    Left = 12, Top = 136, Width = 478, Height = 200,
                    View = View.Details, FullRowSelect = true, GridLines = true,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
                };
                lst.Columns.Add("Date / Time (UTC)", 140);
                lst.Columns.Add("User", 100);
                lst.Columns.Add("Reason", 220);
                if (CurrentShow?.History != null)
                {
                    foreach (var r in CurrentShow.History.AsEnumerable().Reverse())
                    {
                        string ts = r.Timestamp == DateTime.MinValue ? "" : r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                        lst.Items.Add(new ListViewItem(new[] { ts, r.User ?? "", r.Reason ?? "" }));
                    }
                }

                var btnOk = new Button
                {
                    Text = "OK", DialogResult = DialogResult.OK,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };
                var btnCancel = new Button
                {
                    Text = "Cancel", DialogResult = DialogResult.Cancel,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Right
                };
                dlg.Controls.AddRange(new Control[] { lblDesc, txtDesc, lblHistory, lst, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                dlg.Layout += (s, le) =>
                {
                    int right = dlg.ClientSize.Width - 12;
                    int bottom = dlg.ClientSize.Height - 12;
                    btnCancel.SetBounds(right - 80, bottom - 30, 80, 26);
                    btnOk.SetBounds(right - 168, bottom - 30, 80, 26);
                    lst.Width = right - 12;
                    lst.Height = bottom - 50 - lst.Top;
                    txtDesc.Width = right - 12;
                };

                if (dlg.ShowDialog(this) == DialogResult.OK && CurrentShow != null)
                {
                    CurrentShow.Description = txtDesc.Text;
                    ShowFileHandler.SetDirty();
                    UpdateWebApp();
                }
            }
        }

        internal void SeekPosition(double fraction)
        {
            _commandQueue.Enqueue(() =>
            {
                fraction = Math.Max(0.0, Math.Min(1.0, fraction));
                var active = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && (ps.IsPlaying || ps.IsPaused));
                if (active != null)
                {
                    active.SeekToFraction((float)fraction);
                    // If paused, push the new position to the web immediately
                    if (active.IsPaused)
                    {
                        double pos = active.PlaybackPosition.TotalSeconds;
                        double dur = active.PlaybackLength.TotalSeconds;
                        UpdateWebAppProgress(pos, dur);
                    }
                }
                else if (NextPlayCue != null && NextPlayCue.PlaybackLength.TotalSeconds > 0)
                {
                    NextPlayCue.SeekToFraction((float)fraction);
                }
            });
        }

        /// <summary>
        /// Toggles pause/resume for any currently playing audio. If playing, pauses; if paused, resumes.
        /// </summary>
        internal void TogglePause()
        {
            AppLogger.Info("SFXPlayer.TogglePause");
            _commandQueue.Enqueue(() =>
            {
                var playing = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && ps.IsPlaying);
                var paused = _playingSounds.FirstOrDefault(ps => !ps.IsDisposed && ps.IsPaused);
                PlayStrip active = playing ?? paused;
                if (playing != null)
                    playing.TogglePause();
                else if (paused != null)
                    paused.TogglePause();
                // Update the WinForms pause button label
                if (active?.IsPaused == true)
                    bnPause.Text = "Resume \u25B6";
                else
                    bnPause.Text = "Pause";
                // Reset the paused-transition flag so the timer sends a fresh correct update
                _wasPaused = false;
                // Send an immediate update with the correct playback position
                if (active != null)
                    UpdateWebAppProgress(active.PlaybackPosition.TotalSeconds, active.PlaybackLength.TotalSeconds);
                else
                    UpdateWebApp();
            });
        }

        /// <summary>
        /// Moves the "next cue" pointer to the specified 0-based index without stopping any playing audio.
        /// </summary>
        internal void GotoCue(int index)
        {
            _commandQueue.Enqueue(() =>
            {
                int count = CurrentShow?.Cues?.Count ?? 0;
                if (index < 0 || index >= count) return;
                NextPlayCueIndex = index;
                UpdateWebApp();
            });
        }

        private static string _lastWaveformFile = null;
        private static string _cachedWaveformData = null;

        private static string GetWaveformData(PlayStrip strip)
        {
            if (strip == null) return "";
            string filePath = strip.SFX?.FileName;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return "";
            if (filePath == _lastWaveformFile) return _cachedWaveformData ?? "";
            var peaks = GenerateWaveformPeaks(filePath, 200);
            _lastWaveformFile = filePath;
            _cachedWaveformData = peaks != null
                ? string.Join(",", peaks.Select(p => p.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)))
                : "";
            return _cachedWaveformData;
        }

        private string GetCueListJson()
        {
            var sb = new System.Text.StringBuilder("[");
            bool first = true;
            int nextIdx = NextPlayCue?.PlayStripIndex ?? -1;
            foreach (PlayStrip ps in CueList.Controls.OfType<PlayStrip>().OrderBy(p => p.PlayStripIndex))
            {
                if (!first) sb.Append(',');
                first = false;
                string desc = EscapeJsonString(ps.SFX.Description ?? "");
                string file = EscapeJsonString(Path.GetFileName(ps.SFX.FileName ?? ""));
                string spd = ps.SFX.Speed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string isCur = ps.PlayStripIndex == nextIdx ? "true" : "false";
                sb.Append($"{{\"i\":{ps.PlayStripIndex + 1},\"idx\":{ps.PlayStripIndex},\"d\":\"{desc}\",\"f\":\"{file}\",\"v\":{ps.SFX.Volume},\"s\":{spd},\"c\":{isCur}}}");
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string EscapeJsonString(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

        private static float[] GenerateWaveformPeaks(string fileName, int bucketCount)
        {
            try
            {
                using var reader = new NAudio.Wave.AudioFileReader(fileName);
                // Use the same sampling formula as WaveFormRenderer.Render():
                //   samplesPerPeak = (reader.Length / bytesPerSample) / width
                // where bytesPerSample = BitsPerSample/8 and the sample count includes all channels.
                int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;
                long totalSamples = reader.Length / bytesPerSample;
                int samplesPerPeak = (int)Math.Max(1, totalSamples / bucketCount);

                var peakProvider = new NAudio.WaveFormRenderer.MaxPeakProvider();
                peakProvider.Init(reader, samplesPerPeak);

                float[] peaks = new float[bucketCount];
                for (int i = 0; i < bucketCount; i++)
                    peaks[i] = peakProvider.GetNextPeak().Max;

                float maxPeak = 0;
                foreach (var p in peaks) if (p > maxPeak) maxPeak = p;
                if (maxPeak < 0.0001f) return null;
                for (int i = 0; i < bucketCount; i++)
                    peaks[i] = peaks[i] / maxPeak;

                return peaks;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateWaveformPeaks error: {ex.Message}");
                return null;
            }
        }


        Control LastHovered;
        Color LastHoveredColor;
        bool ReplaceOK;
        bool AddOK;
        bool AddZone;
        bool ReplaceZone;
        private Label lblDeviceStatus;

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

        private void UpdateDeviceStatusDisplay()
        {
            if (lblDeviceStatus == null) return;

            string playback = CurrentPlaybackDeviceIdx >= 0 
                ? $"🔊 {TruncateDeviceName(Settings.Default.LastPlaybackDevice, 25)}" 
                : "🔊 Not Connected";
            
            string preview = CurrentPreviewDeviceIdx >= 0 
                ? $"🎧 {TruncateDeviceName(Settings.Default.LastPreviewDevice, 25)}" 
                : "🎧 Not Connected";
            
            string midi = CurrentMIDIDeviceIdx >= 0 
                ? $"🎹 {TruncateDeviceName(Settings.Default.LastMidiDevice, 20)}" 
                : "🎹 Not Connected";

            lblDeviceStatus.Text = $"{playback}  |  {preview}  |  {midi}";
            
            // Color-code based on connection status
            bool allConnected = CurrentPlaybackDeviceIdx >= 0 && 
                               CurrentPreviewDeviceIdx >= 0 && 
                               CurrentMIDIDeviceIdx >= 0;
            lblDeviceStatus.ForeColor = allConnected 
                ? System.Drawing.Color.DarkGreen 
                : System.Drawing.Color.DarkRed;
            
            // Build detailed tooltip
            string tooltip = "Click to change devices\n\n";
            tooltip += $"Playback: {(CurrentPlaybackDeviceIdx >= 0 ? Settings.Default.LastPlaybackDevice : "Not Connected")}\n";
            tooltip += $"Preview: {(CurrentPreviewDeviceIdx >= 0 ? Settings.Default.LastPreviewDevice : "Not Connected")}\n";
            tooltip += $"MIDI: {(CurrentMIDIDeviceIdx >= 0 ? Settings.Default.LastMidiDevice : "Not Connected")}";
            
            toolTip1.SetToolTip(lblDeviceStatus, tooltip);
        }

        private string TruncateDeviceName(string deviceName, int maxLength = 20)
        {
            if (string.IsNullOrEmpty(deviceName)) return "None";
            if (deviceName.Length <= maxLength) return deviceName;
            return deviceName.Substring(0, maxLength - 3) + "...";
        }

        private void lblDeviceStatus_Click(object sender, EventArgs e)
        {
            ShowDeviceSelectionDialog();
        }

        private void ShowDeviceSelectionDialog()
        {
            using (DeviceSelectionDialog dialog = new DeviceSelectionDialog(
                CurrentAudioOutDevices.ToArray(),
                CurrentAudioOutDevices.ToArray(),
                CurrentMIDIOutDevices.ToArray(),
                Settings.Default.LastPlaybackDevice,
                Settings.Default.LastPreviewDevice,
                Settings.Default.LastMidiDevice))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    bool devicesChanged = false;

                    // Update playback device if changed
                    if (dialog.SelectedPlaybackDevice != Settings.Default.LastPlaybackDevice)
                    {
                        Settings.Default.LastPlaybackDevice = dialog.SelectedPlaybackDevice;
                        devicesChanged = true;
                    }

                    // Update preview device if changed
                    if (dialog.SelectedPreviewDevice != Settings.Default.LastPreviewDevice)
                    {
                        Settings.Default.LastPreviewDevice = dialog.SelectedPreviewDevice;
                        devicesChanged = true;
                    }

                    // Update MIDI device if changed
                    if (dialog.SelectedMidiDevice != Settings.Default.LastMidiDevice)
                    {
                        Settings.Default.LastMidiDevice = dialog.SelectedMidiDevice;
                        devicesChanged = true;
                    }

                    if (devicesChanged)
                    {
                        Settings.Default.Save();
                        UpdateDevices();
                    }
                }
            }
        }
    }
}
