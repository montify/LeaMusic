using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LeaMusic.src
{
    public class Track
    {
        public static int UNIQUE_ID = 0;

        internal WaveStream audio;
        internal RubberBandWaveStream rubberBandWaveStream;
        internal LoopStream loopStream;
        internal WaveformProvider waveformProvider;

        public VolumeSampleProvider volumeStream;
        public TimeSpan ClipDuration { get; set; }
        public WaveFormat Waveformat { get; set; }
        public string RelativePath { get;  set; }
        public string FileName { get;  set; }
        public string OriginFilePath { get; set; }
        public int ID { get; set; }
        public bool IsMuted { get; set; }
        public string WaveformBinaryFilePath { get; set; }

       
        public Track()
        { 
        }

        public void ImportTrack(string path, LeaResourceManager resourceManager)
        {
            OriginFilePath = path;
            RelativePath = path;
            FileName = Path.GetFileName(path);

            audio = resourceManager.LoadAudioFile(path);

            Waveformat = audio.WaveFormat;
            ClipDuration = audio.TotalTime;

            loopStream = new LoopStream(audio, 0, ClipDuration.TotalSeconds);
            rubberBandWaveStream = new RubberBandWaveStream(loopStream);
            volumeStream = new VolumeSampleProvider(rubberBandWaveStream.ToSampleProvider());

            ID = ++UNIQUE_ID;
        }

        public void JumpToPosition(TimeSpan position)
        {
            loopStream.JumpToSeconds(position.TotalSeconds);
        }

        public void SetVolumte(float volume)
        {
            volumeStream.Volume = volume;
        }
    }
}