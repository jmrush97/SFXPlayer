using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SFXPlayer.classes
{
    public class MusicPlayer : Component
    {
        private WaveOutEvent _soundOut;
        private WaveStream _waveSource;
        private SpeedSampleProvider _speedProvider;
        private float _speed = 1.0f;

        // Stored so the full chain (including fade) can be rebuilt on seek/position reset
        private int _fadeInMs;
        private int _fadeOutMs;
        private FadeCurve _fadeCurve;
        private FadeSampleProvider _fadeProvider;

        // Suppresses the PlaybackStopped event while a seek re-init is in progress.
        // Reset is posted through _seekSyncContext (same context NAudio uses) to guarantee
        // it runs AFTER any queued spurious PlaybackStopped that Stop() may have posted.
        private bool _isSeeking = false;
        private SynchronizationContext _seekSyncContext;
        private EventHandler<StoppedEventArgs> _externalPlaybackStopped;

        /// <summary>
        /// Current gain from the fade envelope (0.0–1.0). Returns 1.0 if no fade is active.
        /// </summary>
        public float CurrentFadeGain => _fadeProvider?.CurrentGain ?? 1.0f;

        public event EventHandler<StoppedEventArgs> PlaybackStopped
        {
            add => _externalPlaybackStopped += value;
            remove => _externalPlaybackStopped -= value;
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (!_isSeeking)
                _externalPlaybackStopped?.Invoke(sender, e);
        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (_soundOut != null)
                    return _soundOut.PlaybackState;
                return PlaybackState.Stopped;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.CurrentTime;
                return TimeSpan.Zero;
            }
            set
            {
                if (_waveSource == null) return;
                double totalSeconds = _waveSource.TotalTime.TotalSeconds;
                double fraction = totalSeconds > 0 ? value.TotalSeconds / totalSeconds : 0.0;
                Seek(Math.Max(0.0, Math.Min(1.0, fraction)));
            }
        }

        public TimeSpan Length
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.TotalTime;
                return TimeSpan.Zero;
            }
        }

        public int Volume
        {
            get
            {
                if (_soundOut != null)
                    return Math.Min(100, Math.Max((int)(_soundOut.Volume * 100), 0));
                return 100;
            }
            set
            {
                if (_soundOut != null)
                {
                    _soundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
                }
            }
        }

        public void Open(string filename, int deviceNumber, float speed = 1.0f,
            int fadeInMs = 0, int fadeOutMs = 0, FadeCurve fadeCurve = FadeCurve.Linear)
        {
            // Suppress any pending PlaybackStopped that was posted to the SynchronizationContext
            // by a prior Stop() call. Without this guard the stale event can arrive after the new
            // Play() has already started and incorrectly reset PlayerState back to 'loaded' —
            // and if SFX.AutoPlay is true, trigger a spurious second auto-advance. This mirrors
            // the same _isSeeking pattern used in Seek().
            _seekSyncContext = SynchronizationContext.Current;
            _isSeeking = true;
            CleanupPlayback();
            AppLogger.Info($"MusicPlayer.Open: file=\"{filename}\", device={deviceNumber}, speed={speed}, fadeIn={fadeInMs}ms, fadeOut={fadeOutMs}ms, curve={fadeCurve}");
            _waveSource = new AudioFileReader(filename);
            _soundOut = new WaveOutEvent();
            _soundOut.DeviceNumber = deviceNumber;
            _speed = speed;
            _fadeInMs = fadeInMs;
            _fadeOutMs = fadeOutMs;
            _fadeCurve = fadeCurve;

            if (Math.Abs(speed - 1.0f) > 0.001f)
                _speedProvider = new SpeedSampleProvider((ISampleProvider)_waveSource, speed);
            else
                _speedProvider = null;

            _soundOut.Init(new SampleToWaveProvider(BuildChain()));
            _soundOut.PlaybackStopped += OnPlaybackStopped;

            // Post the _isSeeking reset after the new device is fully configured. Because
            // SynchronizationContext.Post is FIFO, the stale PlaybackStopped (from the prior
            // Stop()) will be processed — and suppressed — before this reset fires.
            if (_seekSyncContext != null)
                _seekSyncContext.Post(_ => _isSeeking = false, null);
            else
                _isSeeking = false;
        }

        /// <summary>
        /// Builds the sample-provider chain from the current _waveSource position.
        /// Always creates a fresh FadeSampleProvider so the fade counters start at zero.
        /// </summary>
        private ISampleProvider BuildChain()
        {
            ISampleProvider chain = _speedProvider != null
                ? (ISampleProvider)_speedProvider
                : (ISampleProvider)_waveSource;

            if (_fadeInMs > 0 || _fadeOutMs > 0)
            {
                double totalSeconds = _waveSource.TotalTime.TotalSeconds;
                _fadeProvider = new FadeSampleProvider(chain, _fadeInMs, _fadeOutMs, totalSeconds, _fadeCurve);
                chain = _fadeProvider;
            }
            else
            {
                _fadeProvider = null;
            }

            return chain;
        }

        /// <summary>
        /// Seeks to a fractional position (0.0–1.0) without firing PlaybackStopped.
        /// Automatically resumes playback if the player was playing when Seek was called.
        /// </summary>
        public void Seek(double fraction)
        {
            if (_waveSource == null || _soundOut == null) return;
            fraction = Math.Max(0.0, Math.Min(1.0, fraction));
            var targetTime = TimeSpan.FromSeconds(fraction * _waveSource.TotalTime.TotalSeconds);
            bool wasPlaying = _soundOut.PlaybackState == PlaybackState.Playing;
            bool wasPaused  = _soundOut.PlaybackState == PlaybackState.Paused;
            _isSeeking = true;
            try
            {
                // NAudio's WaveOutEvent.Init() requires the device to be in Stopped state.
                // Stop any active (playing or paused) output before reinitialising.
                if (wasPlaying || wasPaused) _soundOut.Stop();
                _waveSource.CurrentTime = targetTime;
                // NAudio's WaveOutEvent requires re-initialization after Stop() because
                // Stop() releases the internal buffer; without re-Init the next Play()
                // would throw an InvalidOperationException.
                _soundOut.Init(new SampleToWaveProvider(BuildChain()));
                if (wasPlaying) _soundOut.Play();
            }
            finally
            {
                // NAudio delivers PlaybackStopped via SynchronizationContext.Post() when a
                // sync context was captured at WaveOutEvent construction time (which happens
                // when Open() is called on the UI thread).  Post() is FIFO, so posting our
                // reset HERE guarantees it is processed AFTER the spurious PlaybackStopped
                // that Stop() enqueued — keeping _isSeeking == true long enough to suppress
                // that event.  Without this the finally block would reset _isSeeking before
                // the queued event arrived, causing a spurious playback-stopped notification.
                if (_seekSyncContext != null)
                    _seekSyncContext.Post(_ => _isSeeking = false, null);
                else
                    _isSeeking = false;
            }
        }

        public void Play()
        {
            AppLogger.Info("MusicPlayer.Play");
            _soundOut?.Play();
        }

        public void Pause()
        {
            if (_soundOut != null)
            {
                AppLogger.Info("MusicPlayer.Pause");
                _soundOut.Pause();
            }
        }

        public void Resume()
        {
            if (_soundOut != null)
            {
                AppLogger.Info("MusicPlayer.Resume");
                _soundOut.Play();
            }
        }

        public void Stop()
        {
            if (_soundOut != null)
            {
                AppLogger.Info("MusicPlayer.Stop");
                _soundOut.Stop();
            }
        }

        private void CleanupPlayback()
        {
            _fadeProvider = null;
            if (_soundOut != null)
            {
                Debug.WriteLine("_soundOut.Dispose");
                _soundOut.Dispose();
                _soundOut = null;
            }
            if (_waveSource != null)
            {
                Debug.WriteLine("_waveSource.Dispose");
                _waveSource.Dispose();
                _waveSource = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CleanupPlayback();
        }
    }
}