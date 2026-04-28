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

        /// <summary>
        /// Current gain from the fade envelope (0.0–1.0). Returns 1.0 if no fade is active.
        /// </summary>
        public float CurrentFadeGain => _fadeProvider?.CurrentGain ?? 1.0f;

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

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
                if (_waveSource != null)
                    _waveSource.CurrentTime = value;
                if (_soundOut != null)
                {
                    _soundOut.Init(new SampleToWaveProvider(BuildChain()));
                }
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

            if (PlaybackStopped != null) _soundOut.PlaybackStopped += PlaybackStopped;
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