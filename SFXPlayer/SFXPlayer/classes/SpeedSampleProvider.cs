using NAudio.Wave;
using System;

namespace SFXPlayer.classes
{
    /// <summary>
    /// ISampleProvider that changes playback speed (and pitch) using linear interpolation.
    /// A speed of 2.0 plays twice as fast; 0.5 plays at half speed.
    /// </summary>
    public class SpeedSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _speed;
        private readonly float[] _readBuffer;
        private const int MaxReadBufferSamples = 4096;

        public SpeedSampleProvider(ISampleProvider source, float speed)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _speed = Math.Max(0.1f, Math.Min(20.0f, speed));
            _readBuffer = new float[MaxReadBufferSamples];
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int channels = _source.WaveFormat.Channels;
            // number of source frames to read (in frames, not samples)
            int outputFrames = count / channels;
            int inputFrames = (int)Math.Ceiling(outputFrames * _speed);
            int inputSamples = inputFrames * channels;

            // read source into a temporary buffer
            float[] src = inputSamples <= MaxReadBufferSamples ? _readBuffer : new float[inputSamples];
            int samplesRead;
            try
            {
                samplesRead = _source.Read(src, 0, inputSamples);
            }
            catch (System.Runtime.InteropServices.InvalidComObjectException)
            {
                // The underlying COM media-foundation source was disposed while the NAudio
                // playback thread was still reading (e.g. speed was changed mid-playback).
                // Return 0 to signal end-of-stream so WaveOutEvent can stop cleanly.
                return 0;
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
            int framesRead = samplesRead / channels;

            if (framesRead == 0) return 0;

            int outputWritten = 0;
            for (int outFrame = 0; outFrame < outputFrames; outFrame++)
            {
                float srcPos = outFrame * _speed;
                int i0 = (int)srcPos;
                float frac = srcPos - i0;
                int i1 = Math.Min(i0 + 1, framesRead - 1);

                if (i0 >= framesRead) break;

                for (int ch = 0; ch < channels; ch++)
                {
                    float s0 = src[i0 * channels + ch];
                    float s1 = src[i1 * channels + ch];
                    buffer[offset + outFrame * channels + ch] = s0 + frac * (s1 - s0);
                }
                outputWritten = (outFrame + 1) * channels;
            }

            return outputWritten;
        }
    }
}
