using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFXPlayer.classes
{
    public class DisplaySettings
    {
        public string PrevMainText = "";
        public string MainText = "";
        public string TrackName = null;
        public string TrackInfo = "";
        public string Title = "";
        public double TrackPositionSeconds = 0.0;
        public double TrackDurationSeconds = 0.0;
        public int CurrentVolume = 50;
        public float CurrentSpeed = 1.0f;
        public bool StopOthers = false;

        // Current (next) cue details
        public string CueNumber = "";
        public string CueDescription = "";
        public string CueFileName = "";
        public bool CueAutoRun = false;
        public double CuePauseSeconds = 0.0;

        // Fade settings for the current (next) cue
        public int CueFadeInMs = 0;
        public int CueFadeOutMs = 0;
        public string CueFadeCurve = "Linear";

        // Previous cue details
        public string PrevCueNumber = "";
        public string PrevCueDescription = "";
        public string PrevCueFileName = "";

        // Currently playing track state (distinct from next-cue values above)
        public bool IsPlaying = false;
        public int PlayingVolume = 50;
        public float PlayingSpeed = 1.0f;
        public float PlayingFadeGain = 1.0f;

        // Audio device selection (pipe-separated list of available device names)
        public string AvailablePlaybackDevices = "";
        public string CurrentPlaybackDevice = "";

        // Preview device selection
        public string AvailablePreviewDevices = "";
        public string CurrentPreviewDevice = "";

        // Waveform peak data for the current/next track (comma-separated normalised 0-1 values)
        public string WaveformData = "";
    }
}
