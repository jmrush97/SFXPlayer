using static SFXPlayer.classes.SVGResources;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using SFXPlayer.Properties;
using System.Threading;
using System.IO;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using NAudio.WaveFormRenderer;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using SFXPlayer.classes;

namespace SFXPlayer
{
    enum PlayerState
    {
        error = -1,
        uninitialised,// = PlaybackState.Stopped,
        play,// = PlaybackState.Playing,
        paused,// = PlaybackState.Paused,
        loading = 3,
        loaded = 4,

    }

    /// <summary>
    /// A TextBox that paints a waveform bitmap as its background by intercepting
    /// WM_ERASEBKGND, which the native Win32 EDIT control handles itself (ignoring
    /// the inherited BackgroundImage property). Dark background + white text.
    /// The waveform is stored as a full-resolution source <see cref="Bitmap"/> produced by
    /// <see cref="NAudio.WaveFormRenderer.WaveFormRenderer"/>; zoom/pan is implemented by
    /// cropping the source bitmap with <see cref="Graphics.DrawImage"/> into a scaled cache
    /// bitmap that matches the control's pixel size.
    /// Position and volume overlay lines are drawn in GDI+ on top.
    /// </summary>
    internal sealed class WaveformTextBox : TextBox
    {
        private static readonly Color WaveformBg = Color.FromArgb(26, 26, 46);
        private Bitmap _waveformBitmap;  // full-resolution source bitmap (owned here)
        private Bitmap _renderCache;     // control-size cropped/scaled cache
        private int _cacheW = -1, _cacheH = -1;
        private float _cacheZoom = -1f, _cacheZoomCenter = -1f;
        private const int WM_ERASEBKGND = 0x0014;

        // Overlay state
        private float _positionFraction = -1f;  // -1 = no line
        private int _volumePct = 100;            // 0–100
        private float _zoom = 1f;               // 1 = full view; >1 = zoomed in
        private float _zoomCenter = 0.5f;       // fractional center of the zoomed view

        public float Zoom
        {
            get => _zoom;
            set { _zoom = Math.Max(1f, Math.Min(8f, value)); Invalidate(); }
        }

        public WaveformTextBox()
        {
            BackColor = WaveformBg;
            ForeColor = Color.White;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _waveformBitmap?.Dispose();
                _waveformBitmap = null;
                _renderCache?.Dispose();
                _renderCache = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>Sets the waveform source bitmap. Pass null to clear. Takes ownership of the bitmap.</summary>
        public void SetWaveform(Bitmap bmp)
        {
            _waveformBitmap?.Dispose();
            _waveformBitmap = bmp;
            InvalidateCache();
            Invalidate();
        }

        /// <summary>Sets the playback position fraction (0–1) to draw as an overlay line. Use -1 to hide.</summary>
        public void SetPosition(float fraction, float zoomCenter = -1f)
        {
            _positionFraction = fraction;
            if (zoomCenter >= 0f) _zoomCenter = Math.Max(0f, Math.Min(1f, zoomCenter));
            Invalidate();
        }

        /// <summary>Sets the current volume (0–100) for the horizontal volume-level overlay line.</summary>
        public void SetVolume(int volume)
        {
            _volumePct = Math.Max(0, Math.Min(100, volume));
            Invalidate();
        }

        /// <summary>Adjusts zoom by a multiplicative factor (e.g. 1.25 or 0.8) and re-centers on the given fraction.</summary>
        public void AdjustZoom(float factor, float centerFraction)
        {
            _zoom = Math.Max(1f, Math.Min(8f, _zoom * factor));
            _zoomCenter = Math.Max(0f, Math.Min(1f, centerFraction));
            Invalidate();
        }

        /// <summary>
        /// Maps a canvas X coordinate (pixels) to the corresponding track fraction, taking zoom into account.
        /// </summary>
        public float CanvasXToFraction(int canvasX)
        {
            float halfWindow = 0.5f / _zoom;
            float startFrac = Math.Max(0f, _zoomCenter - halfWindow);
            float endFrac = Math.Min(1f, startFrac + 1f / _zoom);
            startFrac = endFrac - 1f / _zoom;
            if (Width <= 0) return 0f;
            float clickRatio = Math.Max(0f, Math.Min(1f, (float)canvasX / Width));
            return Math.Max(0f, Math.Min(1f, startFrac + clickRatio * (endFrac - startFrac)));
        }

        private void InvalidateCache()
        {
            _renderCache?.Dispose();
            _renderCache = null;
            _cacheW = -1; _cacheH = -1;
            _cacheZoom = -1f; _cacheZoomCenter = -1f;
        }

        /// <summary>
        /// Ensures <see cref="_renderCache"/> is a fresh bitmap cropped and scaled from the
        /// source waveform bitmap to the current control size and zoom window.
        /// Re-renders only when size or zoom has changed.
        /// </summary>
        private void EnsureRenderedCache()
        {
            if (_waveformBitmap == null)
            {
                _renderCache?.Dispose();
                _renderCache = null;
                return;
            }

            bool stale = _renderCache == null
                || _cacheW != Width || _cacheH != Height
                || Math.Abs(_cacheZoom - _zoom) > 0.001f
                || Math.Abs(_cacheZoomCenter - _zoomCenter) > 0.001f;

            if (!stale) return;

            // Compute the visible fraction window for zoom/pan
            float halfWindow = 0.5f / _zoom;
            float startFrac = Math.Max(0f, _zoomCenter - halfWindow);
            float endFrac = Math.Min(1f, startFrac + 1f / _zoom);
            startFrac = endFrac - 1f / _zoom; // re-clamp after end-clamp

            // Map fractional window to source pixel columns
            int srcX = (int)(startFrac * _waveformBitmap.Width);
            int srcW = Math.Max(1, (int)((endFrac - startFrac) * _waveformBitmap.Width));

            _renderCache?.Dispose();
            _renderCache = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            using var g = Graphics.FromImage(_renderCache);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(_waveformBitmap,
                new Rectangle(0, 0, Width, Height),
                new Rectangle(srcX, 0, srcW, _waveformBitmap.Height),
                GraphicsUnit.Pixel);

            _cacheW = Width; _cacheH = Height;
            _cacheZoom = _zoom; _cacheZoomCenter = _zoomCenter;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND && m.WParam != IntPtr.Zero)
            {
                using var g = Graphics.FromHdc(m.WParam);
                EnsureRenderedCache();
                if (_renderCache != null)
                    g.DrawImage(_renderCache, 0, 0, Width, Height);
                else
                    g.Clear(WaveformBg);
                DrawOverlays(g);
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        private void DrawOverlays(Graphics g)
        {
            float mid = Height / 2f;

            // Volume level line (dashed horizontal pair at ±volumePct amplitude)
            if (_volumePct > 0 && _volumePct < 100)
            {
                float volH = (_volumePct / 100f) * mid * 0.9f;
                using var volPen = new Pen(Color.FromArgb(140, 100, 180, 255), 1f);
                volPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                g.DrawLine(volPen, 0, mid - volH, Width, mid - volH);
                g.DrawLine(volPen, 0, mid + volH, Width, mid + volH);
            }

            // Playback position line (white vertical)
            if (_positionFraction >= 0f && _positionFraction <= 1f)
            {
                float halfWindow = 0.5f / _zoom;
                float startFrac = Math.Max(0f, _zoomCenter - halfWindow);
                float endFrac = Math.Min(1f, startFrac + 1f / _zoom);
                startFrac = endFrac - 1f / _zoom;
                float visibleFrac = (endFrac > startFrac)
                    ? (_positionFraction - startFrac) / (endFrac - startFrac)
                    : 0.5f;
                int x = Math.Max(0, Math.Min(Width - 1, (int)(visibleFrac * Width)));
                using var pen = new Pen(Color.FromArgb(220, 255, 255, 255), 2f);
                g.DrawLine(pen, x, 0, x, Height);
            }
        }
    }

    public partial class PlayStrip : UserControl
    {
        /// <summary>Threshold below which speed is considered equal to 1.0x for display purposes.</summary>
        private const float SpeedDisplayThreshold = 0.01f;

        private static readonly Font _volumeLabelFont = new Font("Arial", 6.5f, FontStyle.Regular);

        private Bitmap graph;
        private readonly MusicPlayer _musicPlayer = new MusicPlayer();
        private readonly MusicPlayer _PreviewPlayer = new MusicPlayer();
        ucVolume volume = new ucVolume();
        ucSpeed speedControl = new ucSpeed();

        /// <summary>
        /// Thread-safe queue of player actions.  All operations that touch _musicPlayer or
        /// _PreviewPlayer are posted here from UI-event handlers instead of being executed
        /// directly.  The queue is drained by SFXPlayer.ProgressTimer_Tick on the UI thread,
        /// which guarantees that any pending NAudio PlaybackStopped callbacks delivered via
        /// SynchronizationContext.Post() have already been processed before the next action
        /// runs, preventing state-machine races between player events and UI events.
        /// </summary>
        private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _playerQueue =
            new System.Collections.Concurrent.ConcurrentQueue<Action>();

        /// <summary>
        /// Executes all queued player actions in order on the calling thread (must be the UI thread).
        /// Called from SFXPlayer.ProgressTimer_Tick for every PlayStrip regardless of play state.
        /// </summary>
        public void DrainPlayerQueue()
        {
            while (_playerQueue.TryDequeue(out Action action))
                action();
        }

        public event EventHandler StopAll;
        public event EventHandler<StatusEventArgs> ReportStatus;
        public event EventHandler<int> AutoPlayNext;
        public event EventHandler DeleteCue;
        public event EventHandler AddCueBefore;
        /// <summary>Fired when this strip starts or stops playing. Arg = true if now playing.</summary>
        public event EventHandler<bool> PlayingStateChanged;
        /// <summary>Fired when this cue's file or description changes so the web app can be refreshed.</summary>
        public event EventHandler CueChanged;
        /// <summary>Fired when the user clicks this strip to select it as the next cue.</summary>
        public event EventHandler CueSelected;
        int prevPct = -1;

        #region Initialisation

        public PlayStrip()
        {
            InitializeComponent();
            InitialiseButtons();
            InitializeSound();
            SFX = new SFX();
        }

        public PlayStrip(SFX SFX) : this()
        {
            this.SFX = SFX;
            UpdateButtons();
        }
        // Sets up the SoundPlayer object.
        private void InitializeSound()
        {
            // Create an instance of the SoundPlayer class.
            //player = new SoundPlayer();

            // Listen for the LoadCompleted event.
            //player.LoadCompleted += new AsyncCompletedEventHandler(player_LoadCompleted);

            // Listen for the SoundLocationChanged event.
            //player.SoundLocationChanged += new EventHandler(player_LocationChanged);

            components.Add(_musicPlayer);
            _musicPlayer.PlaybackStopped += _musicPlayer_PlaybackStopped;
            components.Add(_PreviewPlayer);
            _PreviewPlayer.PlaybackStopped += _PreviewPlayer_PlaybackStopped;
            volume.VolumeChanged += Volume_VolumeChanged;
            volume.Done += Volume_Done;
            bnVolume.Paint += BnVolume_Paint;
            speedControl.SpeedChanged += SpeedControl_SpeedChanged;
            speedControl.Done += SpeedControl_Done;


        }

        private void _PreviewPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            timer1.Stop();
            UpdatePlayerState(PlayerState);
        }

        private void PlayStrip_Load(object sender, EventArgs e)
        {
            AddDnDEventHandlers(this);
            pnlWaveform.MouseDown += PnlWaveform_MouseDown;
            pnlWaveform.MouseWheel += PnlWaveform_MouseWheel;
            // Wire a click on the strip itself (any non-interactive child) to select this cue
            MouseDown += PlayStrip_MouseDown_SelectCue;
            foreach (Control c in Controls)
            {
                if (c != pnlWaveform)
                    c.MouseDown += PlayStrip_MouseDown_SelectCue;
            }
            UpdateWaveformBackground();
        }

        private void PlayStrip_MouseDown_SelectCue(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                CueSelected?.Invoke(this, EventArgs.Empty);
        }

        private void PnlWaveform_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && pnlWaveform.Width > 0 && _musicPlayer.Length.TotalSeconds > 0)
            {
                float fraction = pnlWaveform.CanvasXToFraction(e.X);
                _playerQueue.Enqueue(() => SeekToFraction(fraction));
            }
        }

        private void PnlWaveform_MouseWheel(object sender, MouseEventArgs e)
        {
            float factor = e.Delta > 0 ? 1.25f : 0.8f;
            float center = pnlWaveform.CanvasXToFraction(e.X);
            pnlWaveform.AdjustZoom(factor, center);
        }

        #endregion

        #region SFX_Object

        private SFX _SFX;
        public SFX SFX
        {
            get
            {
                return _SFX;
            }
            set
            {
                _SFX = value;
                tbDescription.Text = SFX.Description;
                pnlWaveform.SetVolume(SFX.Volume);
                bnStopAll.Checked = SFX.StopOthers;
                UpdatePlayerState(PlayerState);
                UpdateAutoPlayLabel();
                UpdateSpeedTooltip();
                UpdateVolumeTooltip();
                UpdateFadeTooltip();
                UpdateWaveformBackground();
            }
        }

        private void tbDescription_TextChanged(object sender, EventArgs e)
        {
            SFX.Description = tbDescription.Text;
            CueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void bnStopAll_CheckedChanged(object sender, EventArgs e)
        {
            SFX.StopOthers = bnStopAll.Checked;
        }

        private void Delete_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            autoRunToolStripMenuItem.Checked = SFX.AutoPlay;
            setPauseToolStripMenuItem.Text = string.Format("Set Auto-run Pause ({0:0.0}s)...",
                SFX.AutoPlayPauseMs / 1000.0);

            // Update cue-state radio indicators using shared label constants
            bool hasFile = !string.IsNullOrEmpty(SFX.FileName);
            const string labelNormal  = "Normal (Yellow)";
            const string labelAutoRun = "Auto-run (Green)";
            const string labelSkip    = "Skip / Not Run (White)";
            cueStateNormalMenuItem.Text  = (hasFile && !SFX.Skipped && !SFX.AutoPlay) ? $"✔ {labelNormal}"  : $"○ {labelNormal}";
            cueStateAutoRunMenuItem.Text = (hasFile && !SFX.Skipped &&  SFX.AutoPlay) ? $"✔ {labelAutoRun}" : $"○ {labelAutoRun}";
            cueStateSkipMenuItem.Text    = (SFX.Skipped || !hasFile)                  ? $"✔ {labelSkip}"    : $"○ {labelSkip}";
        }

        private void addCueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddCueBefore?.Invoke(this, EventArgs.Empty);
        }

        private void cueStateNormalMenuItem_Click(object sender, EventArgs e)
        {
            SFX.Skipped = false;
            SFX.AutoPlay = false;
            UpdateStripBackColor();
            UpdateAutoPlayLabel();
        }

        private void cueStateAutoRunMenuItem_Click(object sender, EventArgs e)
        {
            SFX.Skipped = false;
            SFX.AutoPlay = true;
            UpdateStripBackColor();
            UpdateAutoPlayLabel();
        }

        private void cueStateSkipMenuItem_Click(object sender, EventArgs e)
        {
            SFX.Skipped = true;
            UpdateStripBackColor();
            UpdateAutoPlayLabel();
        }

        private void autoRunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SFX.AutoPlay = autoRunToolStripMenuItem.Checked;
            if (SFX.AutoPlay) SFX.Skipped = false;
            UpdateStripBackColor();
            UpdateAutoPlayLabel();
        }

        private void setPauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double currentSecs = SFX.AutoPlayPauseMs / 1000.0;
            string input = ShowInputDialog(
                "Enter pause before next cue (seconds, e.g. 1.5):",
                "Auto-run Pause",
                currentSecs.ToString("0.0"));
            if (string.IsNullOrEmpty(input)) return;
            if (double.TryParse(input,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture,
                out double secs))
            {
                SFX.AutoPlayPauseMs = (int)Math.Round(Math.Max(0, Math.Min(60, secs)) * 1000);
                UpdateAutoPlayLabel();
            }
        }

        private void UpdateAutoPlayLabel()
        {
            if (SFX == null || !SFX.AutoPlay || SFX.Skipped)
            {
                lbAutoPlay.Visible = false;
                return;
            }
            double pauseSecs = SFX.AutoPlayPauseMs / 1000.0;
            if (pauseSecs > 0)
                lbAutoPlay.Text = string.Format("⏩ Auto-run  +{0:0.0}s pause", pauseSecs);
            else
                lbAutoPlay.Text = "⏩ Auto-run (no pause)";
            lbAutoPlay.Visible = true;
        }

        /// <summary>
        /// Returns the loaded-state background color based on cue state (dark theme).
        /// Dark navy = no file or skipped, dark amber = normal, dark green = auto-run
        /// </summary>
        private Color GetCueStateColor()
        {
            if (SFX == null || string.IsNullOrEmpty(SFX.FileName) || SFX.Skipped)
                return SystemColors.Control;                    // default: system background
            return SFX.AutoPlay
                ? Color.FromArgb(160, 230, 160)                // medium green  (auto-run)
                : Color.FromArgb(240, 220, 130);               // medium amber (normal)
        }

        /// <summary>
        /// Apply the cue-state colour to the strip background when in a
        /// non-transient state (loaded / uninitialised).
        /// </summary>
        private void UpdateStripBackColor()
        {
            if (PlayerState == PlayerState.loaded || PlayerState == PlayerState.uninitialised)
            {
                BackColor = GetCueStateColor();
            }
        }

        private static string ShowInputDialog(string prompt, string title, string defaultValue)
        {
            using Form dialog = new Form();
            dialog.Text = title;
            dialog.ClientSize = new System.Drawing.Size(300, 220);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            Label lbl = new Label { Text = prompt, Location = new System.Drawing.Point(10, 8), AutoSize = false, Width = 280, Height = 40 };
            TextBox tb = new TextBox { Text = defaultValue, Location = new System.Drawing.Point(10, 52), Width = 280 };
            Button btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(130, 78), Width = 75 };
            Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(215, 78), Width = 75 };
            dialog.Controls.AddRange(new Control[] { lbl, tb, btnOK, btnCancel });
            dialog.AcceptButton = btnOK;
            dialog.CancelButton = btnCancel;
            tb.Select(0, defaultValue.Length);
            return dialog.ShowDialog() == DialogResult.OK ? tb.Text : null;
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteCue?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region UI

        private void PlayStrip_Resize(object sender, EventArgs e)
        {
            ResizeProgressBar();
            UpdateWaveformBackground();
        }

        private void UpdatePlayerState(PlayerState newstate)
        {
            switch (newstate)
            {
                case PlayerState.uninitialised:
                    BackColor = GetCueStateColor();
                    break;
                case PlayerState.loading:
                    BackColor = Color.FromArgb(255, 200, 200);         // light coral (loading)
                    break;
                case PlayerState.loaded:
                    BackColor = GetCueStateColor();
                    break;
                case PlayerState.play:
                    BackColor = Color.Transparent;
                    break;
                case PlayerState.paused:
                    BackColor = Color.FromArgb(200, 255, 225);         // light green-teal (paused)
                    break;
                case PlayerState.error:
                    BackColor = Color.FromArgb(255, 160, 160);          // light red (error)
                    break;
                default:
                    BackColor = GetCueStateColor();
                    break;
            }
            prevPct = -1;
        }

        public int PlayStripIndex
        {
            get
            {
                return int.Parse(lbIndex.Text) - 1;
            }
            set
            {
                lbIndex.Text = (value + 1).ToString("D3");
            }

        }

        #endregion

        #region buttonupdates
        private void InitialiseButtons()
        {
            UpdateButtonImage(bnVolume, "volume-up-fill.svg");
            UpdateButtonImage(bnSpeed, "speed-gauge.svg");
            UpdateButtonImage(bnFade, "music-note-beamed.svg");
            UpdateButtonImage(bnPreview, "headphones.svg");
            UpdateButtonImage(bnPlay, "play-fill.svg");
            UpdateButtonImage(bnEdit, "blank.svg");
        }

        private void UpdateButtonImage(PictureBox Button, string ButtonImageName)
        {
            if ((string)Button.Tag != ButtonImageName)
            {
                Button.Tag = ButtonImageName;
                Button.Image = FromSvgResource(ButtonImageName);
            }
        }

        private void UpdateButtons()
        {
            UpdateFileButton();
            UpdateEditButton();
            UpdatePlayButton();
            UpdatePreviewButton();
        }

        private void UpdateFileButton()
        {
            if (string.IsNullOrEmpty(SFX.FileName))
            {
                UpdateButtonImage(bnFile, "file-music.svg");
                toolTip1.SetToolTip(bnFile, "Select Sound File");
            }
            else if (!File.Exists(SFX.FileName))
            {
                UpdateButtonImage(bnFile, "file-earmark-music.svg");
                toolTip1.SetToolTip(bnFile, "File not found: \"" + SFX.ShortFileName + "\"");
            }
            else
            {
                UpdateButtonImage(bnFile, "file-earmark-music-fill.svg");
                toolTip1.SetToolTip(bnFile, "Remove \"" + SFX.ShortFileName + "\"");
            }
        }

        private void UpdateEditButton()
        {
            if (SFX.Triggers.Any())
            {
                UpdateButtonImage(bnEdit, "stopwatch-fill.svg");
                toolTip1.SetToolTip(bnEdit, $"{SFX.Triggers.Count} event triggers");
            }
            else
            {
                UpdateButtonImage(bnEdit, "stopwatch.svg");
                toolTip1.SetToolTip(bnEdit, "Add event triggers");
            }
        }

        private void UpdatePlayButton()
        {
            if (PlayerState == PlayerState.play)
            {
                UpdateButtonImage(bnPlay, "stop-fill.svg");
                toolTip1.SetToolTip(bnPlay, "Stop");
            }
            else
            {
                if (!File.Exists(SFX.FileName))
                {
                    UpdateButtonImage(bnPlay, "blank.svg");
                    toolTip1.SetToolTip(bnPlay, "");
                }
                else
                {
                    UpdateButtonImage(bnPlay, "play-fill.svg");
                    toolTip1.SetToolTip(bnPlay, "Play");
                }
            }
        }

        private void UpdatePreviewButton()
        {
            if (_PreviewPlayer.PlaybackState == PlaybackState.Playing)
            {
                //bnPreview.Image = Resources.Stop2_18;
                UpdateButtonImage(bnPreview, "stop-fill.svg");
                toolTip1.SetToolTip(bnPreview, "Stop Preview");
            }
            else
            {
                if (!File.Exists(SFX.FileName))
                {
                    UpdateButtonImage(bnPreview, "blank.svg");
                    toolTip1.SetToolTip(bnPreview, "");
                }
                else
                {
                    //bnPreview.Image = Resources.Headphones2_18;
                    UpdateButtonImage(bnPreview, "headphones.svg");
                    toolTip1.SetToolTip(bnPreview, "Preview");
                }
            }
        }
        #endregion  //buttonupdates

        #region AudioFile

        private void bnFile_Click(object sender, EventArgs e)
        {
            if (PlayerState == PlayerState.play) return;
            if (string.IsNullOrEmpty(SFX.FileName))
            {
                ChooseFile();
            }
            else
            {
                if (tbDescription.Text == SFX.ShortFileNameOnly) tbDescription.Text = "";
                SFX.FileName = "";
                PlayerState = PlayerState.uninitialised;
                UpdateWaveformBackground();
                CueChanged?.Invoke(this, EventArgs.Empty);
            }
            UpdateButtons();
        }

        private void tableLayoutPanel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ChooseFile();
        }

        private void lbIndex_DoubleClick(object sender, EventArgs e)
        {
            ChooseFile();
        }

        private void tbDescription_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ChooseFile();
        }

        public static OpenFileDialog OFD;

        private void ChooseFile()
        {
            if (PlayerState == PlayerState.play) return;
            if (PlayerState == PlayerState.paused) Stop();
            //OFD.Filter = CSCore.Codecs.CodecFactory.SupportedFilesFilterEn + "|All files|*.*";
            if (Directory.Exists(Settings.Default.LastAudioFolder))
            {
                OFD.InitialDirectory = Settings.Default.LastAudioFolder;
            }
            OFD.Title = "Choose audio file";
            OFD.FileName = "";
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                SelectFile(OFD.FileName);
            }
        }

        public void SelectFile(string FileName)
        {
            AppLogger.Info($"PlayStrip.SelectFile: \"{FileName}\" | description: \"{SFX.Description}\"");
            // If the audio file is on a different drive than the .sfx file, copy it alongside the .sfx file.
            string sfxDir = Program.mainForm?.ShowDirectory;
            if (!string.IsNullOrEmpty(sfxDir) && Directory.Exists(sfxDir))
            {
                string fileDrive = Path.GetPathRoot(FileName);
                string sfxDrive  = Path.GetPathRoot(sfxDir);
                if (!string.IsNullOrEmpty(fileDrive) && !string.IsNullOrEmpty(sfxDrive) &&
                    !string.Equals(fileDrive, sfxDrive, StringComparison.OrdinalIgnoreCase))
                {
                    string destPath = Path.Combine(sfxDir, Path.GetFileName(FileName));
                    if (!File.Exists(destPath))
                    {
                        try
                        {
                            File.Copy(FileName, destPath);
                            AppLogger.Info($"PlayStrip.SelectFile: copied \"{FileName}\" -> \"{destPath}\"");
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error($"PlayStrip.SelectFile: failed to copy \"{FileName}\" to \"{destPath}\"", ex);
                            MessageBox.Show($"Could not copy file to show folder:\n{ex.Message}", "Copy Failed",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    if (File.Exists(destPath))
                        FileName = destPath;
                }
            }
            Settings.Default.LastAudioFolder = Path.GetDirectoryName(FileName); Settings.Default.Save();
            AddToRecentAudioFiles(FileName);
            if (tbDescription.Text == SFX.ShortFileNameOnly) tbDescription.Text = "";
            SFX.FileName = FileName;
            if (tbDescription.Text == "")
            {
                // Try to read media metadata tags (mp3, wma, mp4, etc.)
                string metaDescription = TryReadMediaDescription(FileName);
                tbDescription.Text = !string.IsNullOrWhiteSpace(metaDescription)
                    ? metaDescription
                    : SFX.ShortFileNameOnly;
            }
            PlayerState = PlayerState.uninitialised;
            UpdateButtons();
            PreloadFile();
            UpdateWaveformBackground();
            CueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Attempts to read title/artist from media file tags.
        /// Returns "<title> - <artist>" or just title, or null if no tag information is available.
        /// </summary>
        private static string TryReadMediaDescription(string fileName)
        {
            try
            {
                using var tagFile = TagLib.File.Create(fileName);
                string title  = tagFile.Tag?.Title?.Trim() ?? "";
                string artist = (tagFile.Tag?.FirstPerformer ?? tagFile.Tag?.AlbumArtists?.FirstOrDefault() ?? "").Trim();
                if (string.IsNullOrEmpty(title)) return null;
                return string.IsNullOrEmpty(artist) ? title : $"{title} - {artist}";
            }
            catch
            {
                return null;
            }
        }

        internal void PreloadFile()
        {
            //if (PlayerState == PlayerState.uninitialised)
            //{
            LoadFile();
            //}
        }

        private void LoadFile()
        {
            if (string.IsNullOrEmpty(SFX.FileName)) return;
            if (!File.Exists(SFX.FileName))
            {
                PlayerState = PlayerState.error;
            }
            if (!File.Exists(SFX.FileName))
            {
                AppLogger.Warning($"PlayStrip.LoadFile: file not found \"{SFX.FileName}\" (description: \"{SFX.Description}\")");
                Program.mainForm.ReportStatus("File not found: " + SFX.FileName);
                Debug.WriteLine("File not found: " + Path.GetFullPath(SFX.FileName));
                return;
            }
            AppLogger.Info($"PlayStrip.LoadFile: loading \"{SFX.FileName}\" | description: \"{SFX.Description}\"");
            PlayerState = PlayerState.loading;
            UpdatePlayerState(PlayerState);
            _musicPlayer.Open(SFX.FileName, SFXPlayer.CurrentPlaybackDeviceIdx, SFX.Speed,
                SFX.FadeInDurationMs, SFX.FadeOutDurationMs, SFX.FadeCurve);
            _musicPlayer.Volume = SFX.Volume;
            PlayerState = PlayerState.loaded;
            AppLogger.Info($"PlayStrip.LoadFile: loaded \"{SFX.FileName}\" | duration: {_musicPlayer.Length}");
        }

        /// <summary>
        /// Generates a high-resolution waveform bitmap and displays it as the background of
        /// the waveform panel so the user can see a representation of the audio, including
        /// fade-in and fade-out envelope overlays. The bitmap is produced by
        /// <see cref="NAudio.WaveFormRenderer.WaveFormRenderer"/> and zoom/pan is handled
        /// by cropping the source bitmap at render time.
        /// </summary>
        private void UpdateWaveformBackground()
        {
            // Clear first
            pnlWaveform.SetWaveform((Bitmap)null);
            if (SFX == null)
            {
                Debug.WriteLine("PlayStrip.UpdateWaveformBackground: SFX is null");
                return;
            }

            if (string.IsNullOrEmpty(SFX.FileName) || !File.Exists(SFX.FileName)) return;
            try
            {
                var bmp = GenerateWaveformBitmap(SFX.FileName, SFX.FadeInDurationMs, SFX.FadeOutDurationMs);
                pnlWaveform.SetWaveform(bmp); // null is safe — SetWaveform clears if null
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlayStrip.UpdateWaveformBackground: {ex.Message}");
            }
        }

        /// <summary>Refresh the waveform background (e.g. after fade settings change).</summary>
        public void RefreshWaveform() => UpdateWaveformBackground();

        /// <summary>
        /// Seeks the music player to a fractional position (0.0 = start, 1.0 = end).
        /// Works whether the player is loaded or playing.
        /// </summary>
        public void SeekToFraction(float fraction)
        {
            var totalSeconds = _musicPlayer.Length.TotalSeconds;
            if (totalSeconds <= 0) return;
            fraction = Math.Max(0f, Math.Min(1f, fraction));
            _musicPlayer.Seek(fraction);
            // If the cue is in "loaded" state (not yet actively playing), keep it that way —
            // Seek handles resume internally if it was already playing.
        }

        /// <summary>
        /// Computes the half-sine fade envelope gain (0–1) for a waveform sample point.
        /// Fade-in: gain rises from 0 to 1 over the first fadeInBuckets buckets.
        /// Fade-out: gain falls from 1 to 0 over the last fadeOutBuckets buckets.
        /// </summary>
        private static float ComputeSineFadeGain(int bucket, int sampleCount, int fadeInBuckets, int fadeOutBuckets)
        {
            if (fadeInBuckets > 0 && bucket < fadeInBuckets)
                return (float)Math.Sin(Math.PI / 2.0 * bucket / Math.Max(1, fadeInBuckets - 1));
            if (fadeOutBuckets > 0 && bucket >= sampleCount - fadeOutBuckets)
            {
                int bucketFromEnd = sampleCount - 1 - bucket;
                return (float)Math.Sin(Math.PI / 2.0 * bucketFromEnd / Math.Max(1, fadeOutBuckets - 1));
            }
            return 1.0f;
        }

        // Source bitmap dimensions — high resolution so zoom crops look sharp.
        private const int WaveformSourceWidth = 2000;
        private const int WaveformSourceHalfHeight = 30; // total height = 60px

        /// <summary>
        /// Generates a high-resolution waveform bitmap for the given audio file using
        /// <see cref="NAudio.WaveFormRenderer.WaveFormRenderer"/> with <see cref="NAudio.WaveFormRenderer.MaxPeakProvider"/>
        /// for accurate peak representation. Fade-in and fade-out envelope curves are drawn
        /// on top in yellow using GDI+.
        /// Returns null if the file cannot be read or the audio is silent.
        /// </summary>
        private static Bitmap GenerateWaveformBitmap(string fileName, int fadeInMs = 0, int fadeOutMs = 0)
        {
            try
            {
                using var reader = new NAudio.Wave.AudioFileReader(fileName);
                double totalDurationSeconds = reader.TotalTime.TotalSeconds;
                if (totalDurationSeconds <= 0) return null;

                var topPen = new Pen(Color.FromArgb(160, 100, 200, 100));
                var bottomPen = new Pen(Color.FromArgb(160, 100, 200, 100));
                var settings = new NAudio.WaveFormRenderer.StandardWaveFormRendererSettings
                {
                    Width = WaveformSourceWidth,
                    TopHeight = WaveformSourceHalfHeight,
                    BottomHeight = WaveformSourceHalfHeight,
                    BackgroundColor = Color.FromArgb(26, 26, 46),
                    TopPeakPen = topPen,
                    BottomPeakPen = bottomPen,
                };

                var bmp = new NAudio.WaveFormRenderer.WaveFormRenderer()
                    .Render(reader, new NAudio.WaveFormRenderer.MaxPeakProvider(), settings) as Bitmap;

                topPen.Dispose();
                bottomPen.Dispose();

                if (bmp == null) return null;

                if ((fadeInMs > 0 || fadeOutMs > 0) && totalDurationSeconds > 0)
                    DrawFadeEnvelopeOnBitmap(bmp, totalDurationSeconds, fadeInMs, fadeOutMs);

                return bmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateWaveformBitmap error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Draws half-sine fade-in and fade-out envelope curves onto an existing waveform bitmap using GDI+.
        /// </summary>
        private static void DrawFadeEnvelopeOnBitmap(Bitmap bmp, double totalDurationSeconds, int fadeInMs, int fadeOutMs)
        {
            const int sampleCount = 200;
            float mid = bmp.Height / 2f;

            int fadeInBuckets = (int)Math.Round(Math.Min(1.0, fadeInMs / 1000.0 / totalDurationSeconds) * sampleCount);
            int fadeOutBuckets = (int)Math.Round(Math.Min(1.0, fadeOutMs / 1000.0 / totalDurationSeconds) * sampleCount);
            if (fadeInBuckets + fadeOutBuckets > sampleCount)
            {
                int total = fadeInBuckets + fadeOutBuckets;
                fadeInBuckets = (int)Math.Round((double)fadeInBuckets / total * sampleCount);
                fadeOutBuckets = sampleCount - fadeInBuckets;
            }

            float[] gains = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
                gains[i] = ComputeSineFadeGain(i, sampleCount, fadeInBuckets, fadeOutBuckets);

            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float penWidth = Math.Max(1f, (float)bmp.Width / 500f);
            using var fadePen = new Pen(Color.FromArgb(178, 255, 220, 0), penWidth);

            if (fadeInBuckets > 1)
            {
                var upper = new PointF[fadeInBuckets];
                var lower = new PointF[fadeInBuckets];
                for (int i = 0; i < fadeInBuckets; i++)
                {
                    float x = (i + 0.5f) / sampleCount * bmp.Width;
                    float halfH = gains[i] * mid * 0.9f;
                    upper[i] = new PointF(x, mid - halfH);
                    lower[i] = new PointF(x, mid + halfH);
                }
                g.DrawLines(fadePen, upper);
                g.DrawLines(fadePen, lower);
            }

            if (fadeOutBuckets > 1)
            {
                int startBucket = sampleCount - fadeOutBuckets;
                var upper = new PointF[fadeOutBuckets];
                var lower = new PointF[fadeOutBuckets];
                for (int i = 0; i < fadeOutBuckets; i++)
                {
                    float x = (startBucket + i + 0.5f) / sampleCount * bmp.Width;
                    float halfH = gains[startBucket + i] * mid * 0.9f;
                    upper[i] = new PointF(x, mid - halfH);
                    lower[i] = new PointF(x, mid + halfH);
                }
                g.DrawLines(fadePen, upper);
                g.DrawLines(fadePen, lower);
            }
        }

        /// <summary>
        /// Adds a file path to the top of the recent audio files list (max 10 entries).
        /// </summary>
        internal static void AddToRecentAudioFiles(string filePath)
        {
            var recent = Settings.Default.RecentAudioFiles
                         ?? new System.Collections.Specialized.StringCollection();
            // Remove existing entry for this file (case-insensitive) so it moves to top
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                if (string.Equals(recent[i], filePath, StringComparison.OrdinalIgnoreCase))
                    recent.RemoveAt(i);
            }
            recent.Insert(0, filePath);
            while (recent.Count > 10)
                recent.RemoveAt(recent.Count - 1);
            Settings.Default.RecentAudioFiles = recent;
            Settings.Default.Save();
        }

        #endregion

        #region Transport

        private PlayerState _playerstate;

        private PlayerState PlayerState
        {
            get
            {
                return _playerstate;
            }
            set
            {
                _playerstate = value;
                UpdatePlayerState(_playerstate);
            }
        }

        public bool IsPlaying => (PlayerState == PlayerState.play);
        public bool IsPaused => (PlayerState == PlayerState.paused);

        public void TogglePause()
        {
            if (PlayerState == PlayerState.play)
                Pause();
            else if (PlayerState == PlayerState.paused)
                UnPause();
        }

        public TimeSpan PlaybackPosition => _musicPlayer.Position;
        public TimeSpan PlaybackLength => _musicPlayer.Length;
        public float CurrentFadeGain => _musicPlayer.CurrentFadeGain;

        private void bnPlay_Click(object sender, EventArgs e)
        {
            _playerQueue.Enqueue(() =>
            {
                try
                {
                    if (PlayerState == PlayerState.paused)
                        UnPause();
                    else if (PlayerState == PlayerState.play)
                        Stop();
                    else if (PlayerState != PlayerState.play)
                        Play();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("PlayStrip.bnPlay_Click: unexpected exception", ex);
                }
            });
        }

        private void bnPreview_Click(object sender, EventArgs e)
        {
            _playerQueue.Enqueue(() =>
            {
                if (_PreviewPlayer.PlaybackState == PlaybackState.Playing)
                {
                    _PreviewPlayer.Stop();
                }
                else
                {
                    if (!File.Exists(SFX.FileName)) return;
                    _PreviewPlayer.Open(SFX.FileName, SFXPlayer.CurrentPreviewDeviceIdx);
                    _PreviewPlayer.Volume = SFX.Volume; _PreviewPlayer.Position = TimeSpan.Zero;
                    _PreviewPlayer.Volume = SFX.Volume;
                    _PreviewPlayer.Play();
                    BackColor = Color.FromArgb(255, 205, 130);
                }
                UpdatePreviewButton();
            });
        }

        private void bnStop_Click(object sender, EventArgs e)
        {
            _playerQueue.Enqueue(() => Stop());
        }


        public void Play()
        {
            AppLogger.Info($"PlayStrip.Play: \"{SFX.FileName}\" | description: \"{SFX.Description}\"");
            if (SFX.Skipped)
            {
                AppLogger.Info($"PlayStrip.Play: cue is skipped, not playing");
                return;
            }
            if (PlayerState == PlayerState.uninitialised)
            {
                LoadFile();
            }
            if (bnStopAll.Checked)
            {
                StopAll?.Invoke(this, new EventArgs());
            }
            if (PlayerState == PlayerState.loaded)
            {
                PlayFromStart();
                ReportStatus?.Invoke(this, new StatusEventArgs("Playing " + SFX.ShortFileNameOnly));
            }
            else if (string.IsNullOrEmpty(SFX.FileName) && SFX.AutoPlay)
            {
                // Blank cue (no audio file): completes instantly; trigger auto-follow immediately.
                TriggerAutoPlay();
            }
        }

        internal void StopOthers(object sender, EventArgs e)
        {
            if (sender != this)
            {
                //don't stop the paused ones
                if (_musicPlayer.PlaybackState == PlaybackState.Playing)
                {
                    Stop();
                }
            }
        }

        internal void StopPreviews(object sender, EventArgs e)
        {
            if (_PreviewPlayer.PlaybackState == PlaybackState.Playing)
            {
                _PreviewPlayer.Stop();
            }
        }

        private void PlayFromStart()
        {
            _musicPlayer.Position = TimeSpan.Zero;
            _musicPlayer.Volume = SFX.Volume;
            if (SFX.DebounceStartMs > 0)
            {
                PlayerState = PlayerState.play;
                PlayingStateChanged?.Invoke(this, true);
                UpdatePlayButton();
                System.Threading.Tasks.Task.Delay(SFX.DebounceStartMs).ContinueWith(_ =>
                {
                    try
                    {
                        if (!IsDisposed && IsHandleCreated)
                            BeginInvoke(new Action(() =>
                            {
                                if (!IsDisposed && PlayerState == PlayerState.play)
                                    _musicPlayer.Play();
                            }));
                    }
                    catch (Exception) { }
                });
            }
            else
            {
                _musicPlayer.Play();
                PlayerState = PlayerState.play;
                PlayingStateChanged?.Invoke(this, true);
                UpdatePlayButton();
            }
            if (SFX.Triggers.Any()) { timer1.Start(); LastTrigger = 0; }
        }

        private void Pause()
        {
            AppLogger.Info($"PlayStrip.Pause: \"{SFX.FileName}\"");
            _musicPlayer.Pause();
            PlayerState = PlayerState.paused;
            ReportStatus?.Invoke(this, new StatusEventArgs("Playing " + SFX.ShortFileNameOnly, true));
            UpdatePlayButton();
        }

        private void UnPause()
        {
            AppLogger.Info($"PlayStrip.UnPause: \"{SFX.FileName}\"");
            _musicPlayer.Resume();
            PlayerState = PlayerState.play;
            UpdatePlayButton();
        }

        public void Stop()
        {
            AppLogger.Info($"PlayStrip.Stop: \"{SFX.FileName}\"");
            if (PlayerState == PlayerState.paused || PlayerState == PlayerState.play)
            {
                //_musicPlayer.Volume = 0;    //makes the stop less "clicky"
                //Thread.Sleep(10);
                _musicPlayer.Stop();
                //_musicPlayer.Volume = SFX.Volume;
                PlayerState = PlayerState.loaded;
                PlayingStateChanged?.Invoke(this, false);
                UpdatePlayButton();
            }
        }

        private void _musicPlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            PlayerState = PlayerState.loaded;
            PlayingStateChanged?.Invoke(this, false);
            UpdatePlayButton();
            try
            {
                ReportStatus?.Invoke(this, new StatusEventArgs("Playing " + SFX.ShortFileNameOnly, true));
            }
            catch { }

            if (SFX.AutoPlay)
            {
                if (SFX.DebounceEndMs > 0)
                    System.Threading.Tasks.Task.Delay(SFX.DebounceEndMs).ContinueWith(_ =>
                    {
                        try { if (!IsDisposed) TriggerAutoPlay(); }
                        catch (Exception) { }
                    });
                else
                    TriggerAutoPlay();
            }
        }

        private void TriggerAutoPlay()
        {
            AutoPlayNext?.Invoke(this, SFX.AutoPlayPauseMs);
        }

        #endregion

        #region ProgressBar

        private void ResizeProgressBar()
        {
            if (Program.mainForm?.WindowState == FormWindowState.Minimized) return;
            graph = new Bitmap(Width, Height);
            BackgroundImage = graph;
            //BackgroundImageLayout = ImageLayout.Stretch;
            prevPct = -1;
            DrawGraph(Progress);
        }

        internal void ProgressUpdate(object sender, EventArgs e)
        {
            if (IsPlaying)
            {
                double length = _musicPlayer.Length.TotalSeconds;
                double pos = _musicPlayer.Position.TotalSeconds;
                if (length > 0)
                {
                    float frac = (float)(pos / length);
                    pnlWaveform.SetPosition(frac, frac); // keep zoom centered on playhead
                }
            }
            else if (PlayerState == PlayerState.loaded)
            {
                // Reset position line when stopped
                pnlWaveform.SetPosition(-1f);
            }
        }

        private int Progress = 0;

        private void UpdatePosition(TimeSpan position)
        {
            double end = _musicPlayer.Length.TotalSeconds;
            double now = position.TotalSeconds;
            int pct = (int)(now / end * Width);
            if (pct == Progress) return;
            Progress = pct;
            SuspendLayout();
            Rectangle Changed = DrawGraph(pct);
            ResumeLayout();
            Invalidate(Changed);
            Refresh();
        }

        private Rectangle DrawGraph(int pct)
        {
            pct = Math.Max(0, Math.Min(Width, pct));
            Graphics g = Graphics.FromImage(graph);

            // Main progress fill
            using (SolidBrush brush = new SolidBrush(Settings.Default.ColourPlayerPlay))
                if (pct > 0)
                    g.FillRectangle(brush, 0, 0, pct, Height);

            using (SolidBrush brush = new SolidBrush(Settings.Default.ColourPlayerLoaded))
                if (pct < Width)
                    g.FillRectangle(brush, pct, 0, Width - pct, Height);

            // Volume level indicator — a thin bar at the bottom showing effective volume
            // Height of bar is proportional to current volume × fade gain
            float fadeGain = PlayerState == PlayerState.play ? _musicPlayer.CurrentFadeGain : 1.0f;
            int volPct = SFX?.Volume ?? 50;  // 0–100
            float effectiveVol = (volPct / 100f) * fadeGain;  // 0–1
            const float VolBarHeightFraction = 0.18f;   // fraction of strip height used for the volume bar
            const float VolMidThreshold     = 0.5f;     // above this, interpolate green → yellow; below, yellow → red
            int barH = Math.Max(2, (int)(Height * VolBarHeightFraction));
            int barW = (int)(Width * effectiveVol);
            if (barW > 0)
            {
                // Color: green at full volume, yellow at mid, red at silence
                int r = effectiveVol >= VolMidThreshold
                    ? (int)(255 * (1f - effectiveVol) * 2f)   // full→mid: 0→255 red
                    : 255;                                      // mid→silence: always 255 red
                int grn = effectiveVol >= VolMidThreshold
                    ? 200                                       // full→mid: fixed green
                    : (int)(255 * effectiveVol * 2f);          // mid→silence: 255→0 green
                Color volColor = Color.FromArgb(180, Math.Max(0, Math.Min(255, r)), Math.Max(0, Math.Min(255, grn)), 0);
                using (SolidBrush brush = new SolidBrush(volColor))
                    g.FillRectangle(brush, 0, Height - barH, barW, barH);
            }

            if (prevPct == -1)
            {
                prevPct = pct;
                return new Rectangle(0, 0, Width, Height);
            }
            else
            {
                Rectangle result = new Rectangle(prevPct, 0, pct, Height);
                prevPct = pct;
                return result;
            }
        }

        #endregion

        #region Volume

        bool justHidden = false;

        private void bnVolume_Enter(object sender, EventArgs e)
        {
            if (SFXPlayer.lastFocused == volume.Controls[0])
            {
                justHidden = true;
            }
        }

        private void bnVolume_Click(object sender, EventArgs e)
        {
            if (justHidden)
            {
                justHidden = false;
            }
            else
            {
                Point Loc = Parent.Location;
                Loc.X += Location.X + Width - volume.Width;
                Loc.Y += Location.Y + Height;
                if (Loc.Y + volume.Height > Parent.Parent.ClientSize.Height)
                {
                    Debug.WriteLine("Won't fit");
                    //return;
                    Loc.Y = Parent.Parent.ClientSize.Height - volume.Height;
                }
                Parent.Parent.Controls.Add(volume);
                Parent.Parent.Controls.SetChildIndex(volume, 0);
                volume.Location = Loc;
                volume.Volume = SFX.Volume;
                volume.Focus();
                Debug.WriteLine("This = " + this.ToString());
                Debug.WriteLine("Parent = " + Parent.ToString());
                Debug.WriteLine("Parent.Parent = " + Parent.Parent.ToString());
                BackColor = SystemColors.Highlight;            // system highlight colour
            }
        }

        private void Volume_VolumeChanged(object sender, EventArgs e)
        {
            int vol = volume.Volume;
            SFX.Volume = vol;
            UpdateVolumeTooltip();
            bnVolume.Invalidate();
            _playerQueue.Enqueue(() => _musicPlayer.Volume = SFX.Volume);
        }

        private void UpdateVolumeTooltip()
        {
            if (SFX == null) return;
            toolTip1.SetToolTip(bnVolume, $"Vol={SFX.Volume}");
        }

        internal void RefreshVolumeDisplay()
        {
            UpdateVolumeTooltip();
            bnVolume.Invalidate();
        }

        /// <summary>Applies a new volume directly to the live player without reloading the file.</summary>
        public void SetVolumeLive(int vol)
        {
            SFX.Volume = Math.Max(0, Math.Min(100, vol));
            _musicPlayer.Volume = SFX.Volume;
            pnlWaveform.SetVolume(SFX.Volume);
            RefreshVolumeDisplay();
        }

        /// <summary>Changes playback speed on the live player, preserving position if playing or paused.</summary>
        public void SetSpeedLive(float speed)
        {
            SFX.Speed = Math.Max(0.1f, Math.Min(8.0f, speed));
            UpdateSpeedTooltip();
            if (PlayerState == PlayerState.play || PlayerState == PlayerState.paused || PlayerState == PlayerState.loaded)
            {
                bool wasPlaying = PlayerState == PlayerState.play;
                bool wasPaused  = PlayerState == PlayerState.paused;
                double savedFraction = 0.0;
                if ((wasPlaying || wasPaused) && _musicPlayer.Length.TotalSeconds > 0)
                    savedFraction = _musicPlayer.Position.TotalSeconds / _musicPlayer.Length.TotalSeconds;
                // Stop before reopening so the device is in a clean state for Open()
                if (wasPlaying || wasPaused) _musicPlayer.Stop();
                if (!string.IsNullOrEmpty(SFX.FileName) && File.Exists(SFX.FileName))
                {
                    _musicPlayer.Open(SFX.FileName, SFXPlayer.CurrentPlaybackDeviceIdx, SFX.Speed,
                        SFX.FadeInDurationMs, SFX.FadeOutDurationMs, SFX.FadeCurve);
                    _musicPlayer.Volume = SFX.Volume;
                    // Seek() uses _isSeeking to suppress the spurious PlaybackStopped that
                    // was queued by the Stop() call above, ensuring our PlayerState assignment
                    // below is not overwritten by the deferred callback.
                    if (wasPlaying || wasPaused)
                        _musicPlayer.Seek(savedFraction);
                    PlayerState = PlayerState.loaded;
                    if (wasPlaying)
                    {
                        _musicPlayer.Play();
                        PlayerState = PlayerState.play;
                        PlayingStateChanged?.Invoke(this, true);
                    }
                    // Note: if wasPaused we intentionally leave the strip in 'loaded' state
                    // at the correct position because WaveOutEvent cannot start in a paused state.
                }
            }
            UpdatePlayButton();
        }

        private void BnVolume_Paint(object sender, PaintEventArgs e)
        {
            if (SFX == null) return;
            string volText = SFX.Volume.ToString();
            SizeF textSize = e.Graphics.MeasureString(volText, _volumeLabelFont);
            float x = (bnVolume.Width - textSize.Width) / 2f;
            float y = bnVolume.Height - textSize.Height - 1;
            e.Graphics.DrawString(volText, _volumeLabelFont, Brushes.Black, x, y);
        }

        private void Volume_Done(object sender, EventArgs e)
        {
            //focus left the volume fader, so disconnect it
            //Debug.WriteLine("Volume_Done - disconnecting fader control");
            if (Parent == null) return;
            if (Parent.Parent == null) return;
            if (Parent.Parent.Controls.Contains(volume))
            {
                Parent.Parent.Controls.Remove(volume);
            }
            UpdatePlayerState(PlayerState);
        }
        #endregion

        #region Speed

        bool justHiddenSpeed = false;

        private void bnSpeed_Enter(object sender, EventArgs e)
        {
            if (SFXPlayer.lastFocused == speedControl.Controls[0])
            {
                justHiddenSpeed = true;
            }
        }

        private void bnSpeed_Click(object sender, EventArgs e)
        {
            if (justHiddenSpeed)
            {
                justHiddenSpeed = false;
            }
            else
            {
                Point Loc = Parent.Location;
                Loc.X += Location.X + Width - speedControl.Width - volume.Width;
                Loc.Y += Location.Y + Height;
                if (Loc.Y + speedControl.Height > Parent.Parent.ClientSize.Height)
                {
                    Loc.Y = Parent.Parent.ClientSize.Height - speedControl.Height;
                }
                Parent.Parent.Controls.Add(speedControl);
                Parent.Parent.Controls.SetChildIndex(speedControl, 0);
                speedControl.Location = Loc;
                speedControl.Speed = SFX.Speed;
                speedControl.Focus();
                BackColor = SystemColors.Highlight;            // system highlight colour
            }
        }

        private void UpdateSpeedTooltip()
        {
            if (SFX == null) return;
            float spd = SFX.Speed;
            string tip = Math.Abs(spd - 1.0f) > SpeedDisplayThreshold
                ? $"Speed={spd:0.00}x"
                : "Speed (1.00x)";
            toolTip1.SetToolTip(bnSpeed, tip);
        }

        private void SpeedControl_SpeedChanged(object sender, EventArgs e)
        {
            float newSpeed = speedControl.Speed;
            // Update the model and tooltip immediately on the UI thread (no player conflict)
            SFX.Speed = newSpeed;
            toolTip1.SetToolTip(bnSpeed, $"Speed={newSpeed:0.00}x");
            // Defer the actual player reload to the queue so that any pending
            // PlaybackStopped callback from NAudio has been processed before we
            // Stop/Open/Play.  SetSpeedLive also uses MusicPlayer.Seek()'s _isSeeking
            // guard to suppress the spurious PlaybackStopped generated by the Stop()
            // call inside the reopen sequence.
            _playerQueue.Enqueue(() => SetSpeedLive(newSpeed));
        }

        private void SpeedControl_Done(object sender, EventArgs e)
        {
            if (Parent == null) return;
            if (Parent.Parent == null) return;
            if (Parent.Parent.Controls.Contains(speedControl))
            {
                Parent.Parent.Controls.Remove(speedControl);
            }
            UpdatePlayerState(PlayerState);
        }

        #endregion

        #region Fade

        bool justHiddenFade = false;

        private void bnFade_Enter(object sender, EventArgs e)
        {
            justHiddenFade = false;
        }

        private void bnFade_Click(object sender, EventArgs e)
        {
            if (justHiddenFade)
            {
                justHiddenFade = false;
                return;
            }
            ShowFadeDialog();
        }

        private void ShowFadeDialog()
        {
            using Form dialog = new Form();
            dialog.Text = "Fade Settings";
            dialog.ClientSize = new System.Drawing.Size(300, 175);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;

            Label lblIn = new Label { Text = "Fade In (ms):", Location = new System.Drawing.Point(10, 12), AutoSize = true };
            NumericUpDown nudIn = new NumericUpDown
            {
                Location = new System.Drawing.Point(130, 10),
                Width = 100,
                Minimum = 0,
                Maximum = 300000,
                Value = Math.Max(0, Math.Min(300000, SFX.FadeInDurationMs)),
                DecimalPlaces = 0
            };

            Label lblOut = new Label { Text = "Fade Out (ms):", Location = new System.Drawing.Point(10, 52), AutoSize = true };
            NumericUpDown nudOut = new NumericUpDown
            {
                Location = new System.Drawing.Point(130, 50),
                Width = 100,
                Minimum = 0,
                Maximum = 300000,
                Value = Math.Max(0, Math.Min(300000, SFX.FadeOutDurationMs)),
                DecimalPlaces = 0
            };

            Label lblCurve = new Label { Text = "Curve:", Location = new System.Drawing.Point(10, 92), AutoSize = true };
            ComboBox cbCurve = new ComboBox
            {
                Location = new System.Drawing.Point(130, 90),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbCurve.Items.AddRange(new object[] { "Linear", "Logarithmic" });
            cbCurve.SelectedIndex = (SFX.FadeCurve == classes.FadeCurve.Logarithmic) ? 1 : 0;

            Button btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new System.Drawing.Point(120, 135), Width = 75 };
            Button btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new System.Drawing.Point(205, 135), Width = 75 };
            dialog.Controls.AddRange(new Control[] { lblIn, nudIn, lblOut, nudOut, lblCurve, cbCurve, btnOK, btnCancel });
            dialog.AcceptButton = btnOK;
            dialog.CancelButton = btnCancel;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SFX.FadeInDurationMs = (int)nudIn.Value;
                SFX.FadeOutDurationMs = (int)nudOut.Value;
                SFX.FadeCurve = cbCurve.SelectedIndex == 1 ? classes.FadeCurve.Logarithmic : classes.FadeCurve.Linear;
                UpdateFadeTooltip();
                UpdateWaveformBackground();

                // Reload so the new fade chain takes effect immediately, via the player queue
                // so any pending PlaybackStopped callback has been processed first.
                if (PlayerState == PlayerState.loaded || PlayerState == PlayerState.play || PlayerState == PlayerState.paused)
                {
                    string fn = SFX.FileName;
                    int dev = SFXPlayer.CurrentPlaybackDeviceIdx;
                    float spd = SFX.Speed;
                    int fi = SFX.FadeInDurationMs;
                    int fo = SFX.FadeOutDurationMs;
                    var fc = SFX.FadeCurve;
                    int vol = SFX.Volume;
                    _playerQueue.Enqueue(() =>
                    {
                        bool wasPlaying2 = PlayerState == PlayerState.play;
                        bool wasPaused2  = PlayerState == PlayerState.paused;
                        double savedFrac2 = 0.0;
                        if ((wasPlaying2 || wasPaused2) && _musicPlayer.Length.TotalSeconds > 0)
                            savedFrac2 = _musicPlayer.Position.TotalSeconds / _musicPlayer.Length.TotalSeconds;
                        if (wasPlaying2 || wasPaused2) _musicPlayer.Stop();
                        if (!string.IsNullOrEmpty(fn) && File.Exists(fn))
                        {
                            _musicPlayer.Open(fn, dev, spd, fi, fo, fc);
                            _musicPlayer.Volume = vol;
                            if (wasPlaying2 || wasPaused2) _musicPlayer.Seek(savedFrac2);
                            PlayerState = PlayerState.loaded;
                            if (wasPlaying2)
                            {
                                _musicPlayer.Play();
                                PlayerState = PlayerState.play;
                                PlayingStateChanged?.Invoke(this, true);
                            }
                        }
                    });
                }
            }
        }

        private void UpdateFadeTooltip()
        {
            if (SFX == null) return;
            string tip;
            if (SFX.FadeInDurationMs == 0 && SFX.FadeOutDurationMs == 0)
                tip = "Fade (none)";
            else
            {
                string curveLabel = SFX.FadeCurve == classes.FadeCurve.Logarithmic ? "Log" : "Lin";
                tip = $"Fade In={SFX.FadeInDurationMs}ms  Out={SFX.FadeOutDurationMs}ms  [{curveLabel}]";
            }
            toolTip1.SetToolTip(bnFade, tip);
        }

        #endregion

        #region DragNDrop

        int AddDnDEventHandlers(Control ctl)
        {
            int count = 0;
            ctl.MouseDown += MouseDownHandler;
            ctl.MouseMove += MouseMoveHandler;
            ctl.MouseUp += MouseUpHandler;
            count++;
            foreach (Control subCtl in ctl.Controls)
            {
                count += AddDnDEventHandlers(subCtl);
            }
            return count;
        }

        private bool CheckingForDrag = false;
        private Rectangle DragBounds = new Rectangle();

        private void MouseDownHandler(object sender, MouseEventArgs e)
        {
            CheckingForDrag = true;
            DragBounds = new Rectangle(e.X - 5, e.Y - 5, 10, 10);
        }

        private void MouseUpHandler(object sender, MouseEventArgs e)
        {
            CheckingForDrag = false;
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (CheckingForDrag)
            {
                if (DragBounds.Contains(e.Location)) return;
                DoDragDrop(this, DragDropEffects.Move | DragDropEffects.Scroll);
                CheckingForDrag = false;
            }
        }
        #endregion

        private void bnEdit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(SFX.FileName) || !File.Exists(SFX.FileName))
            {
                MessageBox.Show("Please select an audio file before adding event triggers.",
                    "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Cursor cursor = Cursor.Current;
            Cursor = Cursors.WaitCursor;
            TimeStamper timeStamper = new TimeStamper();
            timeStamper.Edit(SFX);
            UpdateButtons();
            Cursor = cursor;
        }

        int _LastTrigger = 0;
        int LastTrigger
        {
            get
            {
                return _LastTrigger;
            }
            set
            {
                if (_LastTrigger != value)
                {
                    _LastTrigger = value;
                    //Debug.WriteLine($"new trigger value = {value}");
                }
            }
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            var position = _musicPlayer.Position.Ticks;
            while (LastTrigger < SFX.Triggers.Count())
            {
                if (SFX.Triggers[LastTrigger].TimeTicks <= position)
                {
                    Debug.Write(new TimeSpan(SFX.Triggers[LastTrigger].TimeTicks).ToString());        //trigger this event now
                    SFX.Triggers[LastTrigger].showEvent.Execute();
                    //Debug.WriteLine(SFX.Triggers[LastTrigger].showEvent.ToString());        //trigger this event now
                }
                else
                {
                    break;
                }
                LastTrigger++;
            }
            if (LastTrigger >= SFX.Triggers.Count()) timer1.Stop();
        }
    }

    public class StatusEventArgs : EventArgs
    {
        public string Status;
        public bool Clear = false;
        public StatusEventArgs(string status)
        {
            Status = status;
        }
        public StatusEventArgs(string status, bool clear)
        {
            Status = status;
            Clear = clear;
        }
    }
}
