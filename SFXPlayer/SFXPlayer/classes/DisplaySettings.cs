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
    }
}
