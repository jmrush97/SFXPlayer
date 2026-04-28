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
using Svg;
using Svg.FilterEffects;
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

    public partial class PlayStrip : UserControl
    {
        /// <summary>Threshold below which speed is considered equal to 1.0x for display purposes.</summary>
        private const float SpeedDisplayThreshold = 0.01f;

        private Bitmap graph;
        private readonly MusicPlayer _musicPlayer = new MusicPlayer();
        private readonly MusicPlayer _PreviewPlayer = new MusicPlayer();
        ucVolume volume = new ucVolume();
        ucSpeed speedControl = new ucSpeed();
        public event EventHandler StopAll;
        public event EventHandler<StatusEventArgs> ReportStatus;
        public event EventHandler<int> AutoPlayNext;
        public event EventHandler DeleteCue;
        public event EventHandler AddCueBefore;
        /// <summary>Fired when this strip starts or stops playing. Arg = true if now playing.</summary>
        public event EventHandler<bool> PlayingStateChanged;
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
                bnStopAll.Checked = SFX.StopOthers;
                UpdatePlayerState(PlayerState);
                UpdateAutoPlayLabel();
                UpdateSpeedTooltip();
                UpdateFadeTooltip();
                UpdateWaveformBackground();
            }
        }

        private void tbDescription_TextChanged(object sender, EventArgs e)
        {
            SFX.Description = tbDescription.Text;
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
        /// Returns the loaded-state background color based on cue state:
        /// White = no file or skipped, Yellow = normal, Green = auto-run
        /// </summary>
        private Color GetCueStateColor()
        {
            if (SFX == null || string.IsNullOrEmpty(SFX.FileName) || SFX.Skipped)
                return Color.White;
            return SFX.AutoPlay ? Color.LightGreen : Color.LightYellow;
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
            dialog.ClientSize = new System.Drawing.Size(300, 110);
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
        }

        private void UpdatePlayerState(PlayerState newstate)
        {
            switch (newstate)
            {
                case PlayerState.uninitialised:
                    BackColor = GetCueStateColor();
                    break;
                case PlayerState.loading:
                    BackColor = Settings.Default.ColourPlayerLoading;
                    break;
                case PlayerState.loaded:
                    BackColor = GetCueStateColor();
                    break;
                case PlayerState.play:
                    //BackColor = Settings.Default.ColourPlayerPlay;
                    BackColor = Color.Transparent;
                    break;
                case PlayerState.paused:
                    BackColor = Settings.Default.ColourPlayerPaused;
                    break;
                case PlayerState.error:
                    BackColor = Color.Red;
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
            Settings.Default.LastAudioFolder = Path.GetDirectoryName(FileName); Settings.Default.Save();
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
        /// Generate a mini waveform bitmap and display it as the background of the
        /// description text box so the user can see a representation of the audio.
        /// </summary>
        private void UpdateWaveformBackground()
        {
            // Clear first
            var old = tbDescription.BackgroundImage;
            tbDescription.BackgroundImage = null;
            old?.Dispose();

            if (string.IsNullOrEmpty(SFX.FileName) || !File.Exists(SFX.FileName)) return;
            try
            {
                int w = Math.Max(tbDescription.Width, 1);
                int h = Math.Max(tbDescription.Height, 1);
                var bmp = GenerateWaveformBitmap(SFX.FileName, w, h);
                if (bmp != null)
                {
                    tbDescription.BackgroundImage = bmp;
                    tbDescription.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlayStrip.UpdateWaveformBackground: {ex.Message}");
            }
        }

        private static Bitmap GenerateWaveformBitmap(string fileName, int width, int height)
        {
            const int sampleCount = 200; // number of x-buckets
            float[] peaks = new float[sampleCount];
            float maxPeak = 0;

            try
            {
                using var reader = new NAudio.Wave.AudioFileReader(fileName);
                // Use BlockAlign (bytes per sample frame across all channels) for correct frame count
                long totalFrames = reader.Length / reader.WaveFormat.BlockAlign;
                long framesPerBucket = Math.Max(1, totalFrames / sampleCount);
                float[] buffer = new float[4096];
                int bucket = 0;
                float bucketMax = 0;
                long bucketFilled = 0;
                int read;
                while (bucket < sampleCount && (read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read && bucket < sampleCount; i++)
                    {
                        float abs = Math.Abs(buffer[i]);
                        if (abs > bucketMax) bucketMax = abs;
                        // Count in mono-equivalent frames (divide by channel count)
                        if (i % reader.WaveFormat.Channels == reader.WaveFormat.Channels - 1)
                        {
                            bucketFilled++;
                            if (bucketFilled >= framesPerBucket)
                            {
                                peaks[bucket++] = bucketMax;
                                bucketFilled = 0;
                                bucketMax = 0;
                            }
                        }
                    }
                }
                foreach (var p in peaks) if (p > maxPeak) maxPeak = p;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GenerateWaveformBitmap error: {ex.Message}");
                return null;
            }

            if (maxPeak < 0.0001f) return null;

            var bitmap = new Bitmap(width, height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            using var pen = new Pen(Color.FromArgb(80, 100, 160, 100), 1f);
            float scaleX = (float)width / sampleCount;
            float mid = height / 2f;
            for (int i = 0; i < sampleCount; i++)
            {
                float normalised = peaks[i] / maxPeak;
                float halfH = normalised * mid * 0.9f;
                float x = i * scaleX + scaleX / 2f;
                g.DrawLine(pen, x, mid - halfH, x, mid + halfH);
            }
            return bitmap;
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

        public TimeSpan PlaybackPosition => _musicPlayer.Position;
        public TimeSpan PlaybackLength => _musicPlayer.Length;

        private void bnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                if (PlayerState == PlayerState.paused)
                {
                    UnPause();
                }
                else if (PlayerState == PlayerState.play)
                {
                    Stop();
                }
                else if (PlayerState != PlayerState.play)
                {
                    Play();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("PlayStrip.bnPlay_Click: unexpected exception", ex);
                //ReportStatus(ex.Message);
            }
        }

        private void bnPreview_Click(object sender, EventArgs e)
        {
            if (_PreviewPlayer.PlaybackState == PlaybackState.Playing)
            {
                _PreviewPlayer.Stop();
            }
            else
            {
                if (!File.Exists(SFX.FileName)) return;
                _PreviewPlayer.Open(SFX.FileName, SFXPlayer.CurrentPreviewDeviceIdx);
                _PreviewPlayer.Volume = SFX.Volume; _PreviewPlayer.Position = TimeSpan.Zero;  //this resets the volume!
                _PreviewPlayer.Volume = SFX.Volume;
                _PreviewPlayer.Play();
                BackColor = Settings.Default.ColourPreview;
            }
            UpdatePreviewButton();
        }

        private void bnStop_Click(object sender, EventArgs e)
        {
            Stop();
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
            //UpdatePosition(_musicPlayer.Position);
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
                BackColor = SystemColors.Highlight;
            }
        }

        private void Volume_VolumeChanged(object sender, EventArgs e)
        {
            SFX.Volume = volume.Volume;
            _musicPlayer.Volume = SFX.Volume;
            toolTip1.SetToolTip(bnVolume, "Vol=" + SFX.Volume.ToString());
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
                BackColor = SystemColors.Highlight;
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
            SFX.Speed = newSpeed;
            toolTip1.SetToolTip(bnSpeed, $"Speed={newSpeed:0.00}x");

            // Reload the audio with the new speed if the file is already loaded
            if (PlayerState == PlayerState.loaded || PlayerState == PlayerState.play)
            {
                bool wasPlaying = PlayerState == PlayerState.play;
                if (wasPlaying) _musicPlayer.Stop();
                if (!string.IsNullOrEmpty(SFX.FileName) && File.Exists(SFX.FileName))
                {
                    _musicPlayer.Open(SFX.FileName, SFXPlayer.CurrentPlaybackDeviceIdx, SFX.Speed,
                        SFX.FadeInDurationMs, SFX.FadeOutDurationMs, SFX.FadeCurve);
                    _musicPlayer.Volume = SFX.Volume;
                    PlayerState = PlayerState.loaded;
                    if (wasPlaying)
                    {
                        _musicPlayer.Play();
                        PlayerState = PlayerState.play;
                        PlayingStateChanged?.Invoke(this, true);
                    }
                }
            }
            UpdatePlayButton();
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

                // Reload so the new fade chain takes effect immediately
                if (PlayerState == PlayerState.loaded || PlayerState == PlayerState.play)
                {
                    bool wasPlaying = PlayerState == PlayerState.play;
                    if (wasPlaying) _musicPlayer.Stop();
                    if (!string.IsNullOrEmpty(SFX.FileName) && File.Exists(SFX.FileName))
                    {
                        _musicPlayer.Open(SFX.FileName, SFXPlayer.CurrentPlaybackDeviceIdx, SFX.Speed,
                            SFX.FadeInDurationMs, SFX.FadeOutDurationMs, SFX.FadeCurve);
                        _musicPlayer.Volume = SFX.Volume;
                        PlayerState = PlayerState.loaded;
                        if (wasPlaying)
                        {
                            _musicPlayer.Play();
                            PlayerState = PlayerState.play;
                            PlayingStateChanged?.Invoke(this, true);
                        }
                    }
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
