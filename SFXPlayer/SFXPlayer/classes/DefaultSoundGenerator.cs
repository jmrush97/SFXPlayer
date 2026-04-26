using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Linq;

namespace SFXPlayer.classes
{
    /// <summary>
    /// Generates default sound effects for the application
    /// </summary>
    public static class DefaultSoundGenerator
    {
        private const int SampleRate = 44100;
        private const int DurationSeconds = 30;
        
        /// <summary>
        /// Creates default sound effects in the specified directory
        /// </summary>
        /// <param name="outputDirectory">Directory where sound files will be created</param>
        /// <returns>Array of file paths to the created sound effects</returns>
        public static string[] CreateDefaultSounds(string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var soundFiles = new string[4];
            
            // Sound 1: Ascending C Major Scale (C to C)
            soundFiles[0] = CreateScaleSound(outputDirectory, "01_C_Major_Ascending.wav", 
                GetCMajorScale(), "C Major Ascending Scale");
            
            // Sound 2: Chromatic Scale (C to C)
            soundFiles[1] = CreateScaleSound(outputDirectory, "02_Chromatic_Scale.wav", 
                GetChromaticScale(), "Chromatic Scale C to C");
            
            // Sound 3: F# Major Scale
            soundFiles[2] = CreateScaleSound(outputDirectory, "03_F_Sharp_Major.wav", 
                GetFSharpMajorScale(), "F# Major Scale");
            
            // Sound 4: Descending C Minor Scale (C to C)
            soundFiles[3] = CreateScaleSound(outputDirectory, "04_C_Minor_Descending.wav", 
                GetCMinorDescendingScale(), "C Minor Descending Scale");

            return soundFiles;
        }

        /// <summary>
        /// Gets frequencies for C Major scale (ascending from middle C to C)
        /// </summary>
        private static double[] GetCMajorScale()
        {
            return new[]
            {
                261.63,  // C4 (Middle C)
                293.66,  // D4
                329.63,  // E4
                349.23,  // F4
                392.00,  // G4
                440.00,  // A4
                493.88,  // B4
                523.25   // C5
            };
        }

        /// <summary>
        /// Gets frequencies for chromatic scale (C to C)
        /// </summary>
        private static double[] GetChromaticScale()
        {
            return new[]
            {
                261.63,  // C4
                277.18,  // C#4
                293.66,  // D4
                311.13,  // D#4
                329.63,  // E4
                349.23,  // F4
                369.99,  // F#4
                392.00,  // G4
                415.30,  // G#4
                440.00,  // A4
                466.16,  // A#4
                493.88,  // B4
                523.25   // C5
            };
        }

        /// <summary>
        /// Gets frequencies for F# Major scale
        /// </summary>
        private static double[] GetFSharpMajorScale()
        {
            return new[]
            {
                369.99,  // F#4
                415.30,  // G#4
                466.16,  // A#4
                493.88,  // B4
                554.37,  // C#5
                622.25,  // D#5
                698.46,  // F5 (E#5)
                739.99   // F#5
            };
        }

        /// <summary>
        /// Gets frequencies for C Minor scale descending (C to C)
        /// </summary>
        private static double[] GetCMinorDescendingScale()
        {
            return new[]
            {
                523.25,  // C5
                493.88,  // B4
                466.16,  // A#4 (Bb4)
                440.00,  // A4
                392.00,  // G4
                349.23,  // F4
                311.13,  // D#4 (Eb4)
                293.66,  // D4
                261.63   // C4
            };
        }

        /// <summary>
        /// Creates a scale sound file with smooth transitions between notes
        /// </summary>
        private static string CreateScaleSound(string directory, string filename, double[] frequencies, string description)
        {
            string filepath = Path.Combine(directory, filename);
            
            // Skip if file already exists
            if (File.Exists(filepath))
            {
                return filepath;
            }

            // Calculate duration per note to fill 30 seconds
            // Leave some room for fade in/out
            double totalNoteDuration = DurationSeconds - 1.0; // 1 second for fades
            double noteDuration = totalNoteDuration / frequencies.Length;
            
            // Create a composite of all notes with crossfade
            var noteProviders = frequencies.Select(freq => CreateNote(freq, noteDuration)).ToArray();
            
            // Concatenate all notes
            var scale = new ConcatenatingSampleProvider(noteProviders);
            
            // Apply fade in (0.5 seconds) and fade out (0.5 seconds)
            var fadeInOut = scale
                .ApplyFadeIn(500)   // 0.5 second fade in
                .ApplyFadeOut(500); // 0.5 second fade out

            // Write to WAV file
            WaveFileWriter.CreateWaveFile16(filepath, fadeInOut);

            return filepath;
        }

        /// <summary>
        /// Creates a single note with envelope (attack, sustain, release)
        /// </summary>
        private static ISampleProvider CreateNote(double frequency, double durationSeconds)
        {
            var tone = new SignalGenerator(SampleRate, 1)
            {
                Gain = 0.3, // 30% volume
                Frequency = frequency,
                Type = SignalGeneratorType.Sin
            };

            // Take the duration for this note
            var noteProvider = tone.Take(TimeSpan.FromSeconds(durationSeconds));
            
            // Apply envelope: quick attack, sustain, gentle release
            int attackMs = 50;   // 50ms attack
            int releaseMs = 100; // 100ms release
            
            // noteProvider is already an ISampleProvider, no need to convert
            return noteProvider.ApplyEnvelope(attackMs, releaseMs, durationSeconds);
        }

        /// <summary>
        /// Extension method to apply fade in effect
        /// </summary>
        private static ISampleProvider ApplyFadeIn(this ISampleProvider source, int durationMilliseconds)
        {
            return new FadeInOutSampleProvider(source, true, durationMilliseconds);
        }

        /// <summary>
        /// Extension method to apply fade out effect
        /// </summary>
        private static ISampleProvider ApplyFadeOut(this ISampleProvider source, int durationMilliseconds)
        {
            return new FadeInOutSampleProvider(source, false, durationMilliseconds);
        }

        /// <summary>
        /// Extension method to apply ADSR envelope to a note
        /// </summary>
        private static ISampleProvider ApplyEnvelope(this ISampleProvider source, int attackMs, int releaseMs, double totalDurationSeconds)
        {
            return new EnvelopeSampleProvider(source, attackMs, releaseMs, totalDurationSeconds);
        }
    }

    /// <summary>
    /// Sample provider that applies fade in or fade out effects
    /// </summary>
    internal class FadeInOutSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly bool fadeIn;
        private readonly int fadeSamples;
        private int samplePosition;
        private long totalSamples;

        public FadeInOutSampleProvider(ISampleProvider source, bool fadeIn, int durationMilliseconds)
        {
            this.source = source;
            this.fadeIn = fadeIn;
            fadeSamples = (int)(durationMilliseconds / 1000.0 * source.WaveFormat.SampleRate * source.WaveFormat.Channels);
            samplePosition = 0;
            
            // Calculate total samples for fade out
            if (!fadeIn)
            {
                // We need to know total length for fade out
                totalSamples = 30 * source.WaveFormat.SampleRate * source.WaveFormat.Channels;
            }
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                float multiplier = 1.0f;

                if (fadeIn && samplePosition < fadeSamples)
                {
                    // Fade in
                    multiplier = (float)samplePosition / fadeSamples;
                }
                else if (!fadeIn && samplePosition >= totalSamples - fadeSamples)
                {
                    // Fade out
                    long samplesFromEnd = totalSamples - samplePosition;
                    multiplier = (float)samplesFromEnd / fadeSamples;
                }

                buffer[offset + i] *= multiplier;
                samplePosition++;
            }

            return samplesRead;
        }
    }

    /// <summary>
    /// Sample provider that applies an ADSR envelope (Attack, Decay, Sustain, Release)
    /// </summary>
    internal class EnvelopeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly int attackSamples;
        private readonly int releaseSamples;
        private readonly long totalSamples;
        private int samplePosition;

        public EnvelopeSampleProvider(ISampleProvider source, int attackMs, int releaseMs, double totalDurationSeconds)
        {
            this.source = source;
            attackSamples = (int)(attackMs / 1000.0 * source.WaveFormat.SampleRate * source.WaveFormat.Channels);
            releaseSamples = (int)(releaseMs / 1000.0 * source.WaveFormat.SampleRate * source.WaveFormat.Channels);
            totalSamples = (long)(totalDurationSeconds * source.WaveFormat.SampleRate * source.WaveFormat.Channels);
            samplePosition = 0;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            for (int i = 0; i < samplesRead; i++)
            {
                float multiplier = 1.0f;

                if (samplePosition < attackSamples)
                {
                    // Attack phase - fade in
                    multiplier = (float)samplePosition / attackSamples;
                }
                else if (samplePosition >= totalSamples - releaseSamples)
                {
                    // Release phase - fade out
                    long samplesFromEnd = totalSamples - samplePosition;
                    multiplier = (float)samplesFromEnd / releaseSamples;
                }
                // Else: Sustain phase - full volume (multiplier = 1.0f)

                buffer[offset + i] *= multiplier;
                samplePosition++;
            }

            return samplesRead;
        }
    }
}