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
                    if (_speedProvider != null)
                        _soundOut.Init(new SampleToWaveProvider(_speedProvider));
                    else
                        _soundOut.Init(_waveSource);
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

            ISampleProvider chain = (ISampleProvider)_waveSource;

            if (Math.Abs(speed - 1.0f) > 0.001f)
            {
                _speedProvider = new SpeedSampleProvider(chain, speed);
                chain = _speedProvider;
            }
            else
            {
                _speedProvider = null;
            }

            // Apply fade envelope if requested
            if (fadeInMs > 0 || fadeOutMs > 0)
            {
                double totalSeconds = _waveSource.TotalTime.TotalSeconds;
                chain = new FadeSampleProvider(chain, fadeInMs, fadeOutMs, totalSeconds, fadeCurve);
            }

            _soundOut.Init(new SampleToWaveProvider(chain));

            if (PlaybackStopped != null) _soundOut.PlaybackStopped += PlaybackStopped;
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