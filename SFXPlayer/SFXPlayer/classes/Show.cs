using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SFXPlayer.classes {
    class SFXShowEvent {

    }

    /// <summary>
    /// Represents a single entry in the show file's save history.
    /// </summary>
    [Serializable]
    public class SaveRecord {
        public string User = "";
        public DateTime Timestamp = DateTime.MinValue;
        public string Reason = "";
    }

    [Serializable]
    public class Show {
        public ObservableCollection<SFX> Cues = new ObservableCollection<SFX>();
        public event Action UpdateShow;
        [DefaultValue(0)]
        public int NextPlayCueIndex;
        internal Action ShowFileBecameDirty;

        /// <summary>General description for this cue list / show.</summary>
        [DefaultValue("")]
        public string Description = "";

        /// <summary>Preferred playback output device stored with the file.</summary>
        [DefaultValue("")]
        public string PlaybackDevice = "";

        /// <summary>Preferred preview output device stored with the file.</summary>
        [DefaultValue("")]
        public string PreviewDevice = "";

        /// <summary>Chronological log of each save operation.</summary>
        public List<SaveRecord> History = new List<SaveRecord>();

        public Show() {
            Cues.CollectionChanged += Cues_CollectionChanged;
        }

        private void OnUpdateShow() {
            UpdateShow?.Invoke();
        }

        //public event CueCollectionChanged 

        private void Cues_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (SFX item in e.NewItems) {
                        item.SFXBecameDirty += ShowFileBecameDirty;
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (SFX item in e.OldItems) {
                        item.SFXBecameDirty -= ShowFileBecameDirty;
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (SFX item in e.OldItems) {
                        item.SFXBecameDirty -= ShowFileBecameDirty;
                    }
                    foreach (SFX item in e.NewItems) {
                        item.SFXBecameDirty += ShowFileBecameDirty;
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (SFX item in e.OldItems) {
                        item.SFXBecameDirty -= ShowFileBecameDirty;
                    }
                    break;
            }
            OnUpdateShow();
            OnShowFileBecameDirty();            //Dirty = true; need to set it in filehandler
            //Debug.WriteLine(e.Action);
            //Debug.WriteLine("OldItems (" + e.OldItems?.Count + ") " + e.OldItems);
            //Debug.WriteLine("NewItems (" + e.NewItems?.Count + ") " + e.NewItems);
            //Debug.WriteLine("OldStartingIndex " + e.OldStartingIndex);
            //Debug.WriteLine("NewStartingIndex " + e.NewStartingIndex);
        }

        private void OnShowFileBecameDirty() {
            ShowFileBecameDirty?.Invoke();
        }

        internal void AddCue(SFX SFX, int Index) {
            Cues.Insert(Index, SFX);
        }

        internal void MoveCue(int fromIndex, int toIndex) {
            Cues.Move(fromIndex, toIndex);
        }

        internal string CreateArchive(string CurrentFileName) {
            AppLogger.Info($"Show.CreateArchive: \"{CurrentFileName}\"");
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string tempArchiveFileName = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(CurrentFileName) + ".show");
            Directory.CreateDirectory(tempDirectory);
            Program.mainForm.ReportStatus("Creating Archive: Copying files");
            //get a list of the sound files
            List<string> archCues = new List<string>();
            foreach (SFX cue in Cues) {
                archCues.Add(cue.FileName);
            }
            archCues = archCues.Distinct().ToList();    //only need one copy of each file
            foreach (string audioFileName in archCues) {
                if (!string.IsNullOrEmpty(audioFileName)) {
                    Program.mainForm.ReportStatus("Creating Archive: Copying " + Path.GetFileName(audioFileName));
                    File.Copy(audioFileName, Path.Combine(tempDirectory, Path.GetFileName(audioFileName)));
                }
            }

            Program.mainForm.ReportStatus("Creating Archive: Copying Cue List");
            //save a copy of the xml show file with audio file paths removed
            XMLFileHandler<Show>.UntrackedSave(this, Path.Combine(tempDirectory, Path.GetFileName(CurrentFileName)));
            Show tempShow = XMLFileHandler<Show>.Load(Path.Combine(tempDirectory, Path.GetFileName(CurrentFileName)));
            foreach (SFX cue in tempShow.Cues) {
                cue.FileName = Path.GetFileName(cue.FileName);      //remove the path
            }
            XMLFileHandler<Show>.UntrackedSave(tempShow, Path.Combine(tempDirectory, Path.GetFileName(CurrentFileName)));

            //all files now in temp folder
            Program.mainForm.ReportStatus("Creating Archive: Combining Files");
            ZipFile.CreateFromDirectory(tempDirectory, tempArchiveFileName);

            Directory.Delete(tempDirectory, true);
            Program.mainForm.ReportStatus("Creating Archive: Archive Complete");
            return tempArchiveFileName;
        }

        internal static string ExtractArchive(string fnArchive, string ShowFolder) {
            ZipFile.ExtractToDirectory(fnArchive, ShowFolder);
            DirectoryInfo directory = new DirectoryInfo(ShowFolder);
            FileInfo[] fi = directory.GetFiles("*.sfx");
            if (fi.Count() > 0) return fi[0].FullName;

            FileInfo fileToDecompress = new FileInfo(fnArchive);
            using (FileStream originalFileStream = fileToDecompress.OpenRead()) {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName)) {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress)) {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine("Decompressed: {0}", fileToDecompress.Name);
                    }
                }
            }
            return "";
        }

        internal void RemoveCue(SFX sfx) {
            Cues.Remove(sfx);
        }

        /// <summary>
        /// Creates a new show with default sound effects
        /// </summary>
        /// <returns>A Show object with four default sound cues</returns>
        public static Show CreateDefaultShow()
        {
            AppLogger.Info("Show.CreateDefaultShow");
            var show = new Show();
            
            try
            {
                // Create default sounds directory in user's Documents/SFXPlayer folder
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string defaultSoundsPath = Path.Combine(documentsPath, "SFXPlayer", "DefaultSounds");
                
                // Generate default sound files
                string[] soundFiles = DefaultSoundGenerator.CreateDefaultSounds(defaultSoundsPath);
                
                // Create SFX cues for each sound file
                var descriptions = new[]
                {
                    "C Major Scale - Ascending from Middle C to C (C-D-E-F-G-A-B-C)",
                    "Chromatic Scale - All 12 notes from C to C (C-C#-D-D#-E-F-F#-G-G#-A-A#-B-C)",
                    "F# Major Scale - F#-G#-A#-B-C#-D#-E#-F#",
                    "C Minor Scale - Descending from C to C (C-B-Bb-A-G-F-Eb-D-C)"
                };

                var mainTexts = new[]
                {
                    "C MAJOR ASCENDING",
                    "CHROMATIC SCALE",
                    "F# MAJOR",
                    "C MINOR DESCENDING"
                };

                for (int i = 0; i < soundFiles.Length; i++)
                {
                    var sfx = new SFX
                    {
                        FileName = soundFiles[i],
                        Description = descriptions[i],
                        MainText = mainTexts[i],
                        Volume = 70,
                        StopOthers = false
                    };
                    AppLogger.Info($"Show.CreateDefaultShow: cue {i + 1} file=\"{soundFiles[i]}\" description=\"{descriptions[i]}\"");
                    show.AddCue(sfx, i);
                }
                
                Program.mainForm?.ReportStatus($"Default show created with {soundFiles.Length} musical scales");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Show.CreateDefaultShow: error creating default show", ex);
                Debug.WriteLine($"Error creating default show: {ex}");
                Program.mainForm?.ReportStatus($"Error creating default sounds: {ex.Message}");
            }
            
            return show;
        }
    }
}
