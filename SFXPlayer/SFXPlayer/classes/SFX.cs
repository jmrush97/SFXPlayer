using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SFXPlayer.classes {
    [Serializable]
    [XmlInclude(typeof(MSCEvent))]
    public class SFX {
        internal Action SFXBecameDirty;
        private string _Description = "";
        private string _FileName = "";
        private bool _StopOthers = false;
        private string _MainText = "";
        private int _Volume = 50;
        private float _Speed = 1.0f;
        public SFX()
        {
            Triggers.ListChanged += Triggers_ListChanged;
        }

        private void Triggers_ListChanged(object sender, ListChangedEventArgs e)
        {
            SFXBecameDirty?.Invoke();
        }

        [DefaultValue("")] public string Description { get { return _Description; } set { _Description = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue("")] public string FileName { get { return _FileName; } set { _FileName = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(false)] public bool StopOthers { get { return _StopOthers; } set { _StopOthers = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue("")] public string MainText { get { return _MainText; } set { _MainText = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(50)] public int Volume { get { return _Volume; } set { _Volume = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(1.0f)] public float Speed { get { return _Speed; } set { _Speed = value; SFXBecameDirty?.Invoke(); } }

        private bool _AutoPlay = false;
        private int _AutoPlayPauseMs = 0;
        private int _DebounceStartMs = 0;
        private int _DebounceEndMs = 0;

        private bool _Skipped = false;
        [DefaultValue(false)] public bool Skipped { get { return _Skipped; } set { _Skipped = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(false)] public bool AutoPlay { get { return _AutoPlay; } set { _AutoPlay = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(0)] public int AutoPlayPauseMs { get { return _AutoPlayPauseMs; } set { _AutoPlayPauseMs = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(0)] public int DebounceStartMs { get { return _DebounceStartMs; } set { _DebounceStartMs = value; SFXBecameDirty?.Invoke(); } }
        [DefaultValue(0)] public int DebounceEndMs { get { return _DebounceEndMs; } set { _DebounceEndMs = value; SFXBecameDirty?.Invoke(); } }

        public BindingList<Trigger> Triggers { get; set; } = new BindingList<Trigger>();
        public string ShortFileNameOnly {
            get {
                return Path.GetFileNameWithoutExtension(FileName);
            }
        }
        public string ShortFileName {
            get {
                return Path.GetFileName(FileName);
            }
        }

    }
}
