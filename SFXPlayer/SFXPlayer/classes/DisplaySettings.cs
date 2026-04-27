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
        [DefaultValue("")]
        public string PrevMainText = "";
        [DefaultValue("")]
        public string MainText = "";
        [DefaultValue(null)]
        public string TrackName = null;
        [DefaultValue("")]
        public string Title = "";
        [DefaultValue(0.0)]
        public double TrackPositionSeconds = 0.0;
        [DefaultValue(0.0)]
        public double TrackDurationSeconds = 0.0;
        [DefaultValue(50)]
        public int CurrentVolume = 50;
        [DefaultValue(1.0f)]
        public float CurrentSpeed = 1.0f;
        [DefaultValue(false)]
        public bool StopOthers = false;

        // Current (next) cue details
        [DefaultValue("")]
        public string CueNumber = "";
        [DefaultValue("")]
        public string CueDescription = "";
        [DefaultValue("")]
        public string CueFileName = "";
        [DefaultValue(false)]
        public bool CueAutoRun = false;
        [DefaultValue(0.0)]
        public double CuePauseSeconds = 0.0;

        // Previous cue details
        [DefaultValue("")]
        public string PrevCueNumber = "";
        [DefaultValue("")]
        public string PrevCueDescription = "";
        [DefaultValue("")]
        public string PrevCueFileName = "";
    }
}
