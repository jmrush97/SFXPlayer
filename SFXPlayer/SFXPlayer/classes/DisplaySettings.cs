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
        public bool IsPaused = false;
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

        // Full cue list for the left-panel cue list in the web UI (JSON array)
        public string CueListJson = "[]";

        // True when any cue is currently in the loading/rendering state.
        // The web app disables the Go and Pause buttons while this is true.
        public bool IsLoading = false;

        // Always set to the next-to-play cue (the one "Go" would trigger),
        // independent of whether another cue is currently playing/paused.
        public string GoTrackNum = "";
        public string GoTrackDesc = "";

        // Set to the playing or paused cue reference; empty when nothing is active.
        public string ActiveTrackNum = "";
        public string ActiveTrackDesc = "";

        // Set to the cue after the next-to-play cue (the one "Next" would advance to).
        public string NextNextTrackNum = "";
        public string NextNextTrackDesc = "";
    }
}
