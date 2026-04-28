using NAudio.Wave;
using System;

namespace SFXPlayer.classes
{
    /// <summary>
    /// ISampleProvider that applies a configurable fade-in at the start and/or
    /// fade-out at the end of playback, using either a linear (triangle) or
    /// logarithmic volume curve.
    ///
    /// The fade operates on sample index, not wall-clock time, so it is
    /// independent of speed changes applied upstream.
    /// </summary>
    public class FadeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _fadeInSamples;   // channel-interleaved samples for fade-in
        private readonly int _fadeOutSamples;  // channel-interleaved samples for fade-out
        private readonly long _totalSamples;   // total channel-interleaved samples in the stream
        private readonly FadeCurve _curve;
        private long _samplePosition;

        /// <param name="source">The upstream sample provider (already speed-adjusted if needed).</param>
        /// <param name="fadeInMs">Fade-in duration in milliseconds (0 = no fade-in).</param>
        /// <param name="fadeOutMs">Fade-out duration in milliseconds (0 = no fade-out).</param>
        /// <param name="totalLengthSeconds">Total track length in seconds (needed to position fade-out).</param>
        /// <param name="curve">Volume progression curve.</param>
        public FadeSampleProvider(
            ISampleProvider source,
            int fadeInMs,
            int fadeOutMs,
            double totalLengthSeconds,
            FadeCurve curve = FadeCurve.Linear)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _curve = curve;

            int channels = source.WaveFormat.Channels;
            int sampleRate = source.WaveFormat.SampleRate;

            _fadeInSamples  = (int)(fadeInMs  / 1000.0 * sampleRate * channels);
            _fadeOutSamples = (int)(fadeOutMs / 1000.0 * sampleRate * channels);
            _totalSamples   = (long)(totalLengthSeconds * sampleRate * channels);
            _samplePosition = 0;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                float gain = ComputeGain(_samplePosition);
                buffer[offset + i] *= gain;
                _samplePosition++;
            }

            return samplesRead;
        }

        private float ComputeGain(long pos)
        {
            float t;

            // Fade-in phase
            if (_fadeInSamples > 0 && pos < _fadeInSamples)
            {
                t = (float)pos / _fadeInSamples;
                return ApplyCurve(t);
            }

            // Fade-out phase (only if we know the total length)
            if (_fadeOutSamples > 0 && _totalSamples > 0)
            {
                long fadeOutStart = _totalSamples - _fadeOutSamples;
                if (pos >= fadeOutStart)
                {
                    long samplesFromEnd = _totalSamples - pos;
                    t = (float)samplesFromEnd / _fadeOutSamples;
                    return ApplyCurve(t);
                }
            }

            return 1.0f;
        }

        /// <summary>
        /// Maps a linear progress value t ∈ [0,1] to a gain using the chosen curve.
        /// Linear gives a straight ramp; Logarithmic gives a perceptually even ramp.
        /// </summary>
        private float ApplyCurve(float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            if (_curve == FadeCurve.Logarithmic)
            {
                // Map linear t to dB: from -60 dB (silence) to 0 dB (full volume)
                // gain = 10^(t*6 - 6) but clamped so t=0 → ~0 and t=1 → 1
                return (float)Math.Pow(10.0, t * 6.0 - 6.0);
            }
            // Linear (triangle)
            return t;
        }
    }
}
