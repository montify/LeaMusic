using LeaMusic.src.AudioEngine_.Streams;
using LeaMusic.src.ResourceManager_;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Text.Json.Serialization;

namespace LeaMusic.src.AudioEngine_
{
    public class Track : IDisposable
    {
        public static int UNIQUE_ID = 0;

        internal WaveStream audio = null!;
        internal RubberBandWaveStream? rubberBandWaveStream;
        internal LoopStream loopStream = null!;
        internal WaveformProvider waveformProvider = null!;

        public VolumeSampleProvider volumeStream = null!;
        public TimeSpan ClipDuration { get; set; }
        public WaveFormat? Waveformat { get; set; }
        public int ID { get; set; }
        public string? AudioFileName { get; set; }
        public string? AudioRelativePath { get; set; }
        public string? WaveformRelativePath { get; set; }

        public string? Name { get; private set; }
        [JsonIgnore]
        public string? OriginFilePath { get; set; }

        public bool IsMuted { get; set; }


        public Track()
        {
        }

        public void LoadAudioFile(string audioFilePath, LeaResourceManager resourceManager)
        {
            OriginFilePath = audioFilePath;

            AudioFileName = Path.GetFileName(audioFilePath);

            audio = resourceManager.LoadAudioFile(audioFilePath);
            Name = Path.GetFileNameWithoutExtension(OriginFilePath);

            Waveformat = audio.WaveFormat;
            ClipDuration = audio.TotalTime;

            try
            {
                loopStream = new LoopStream(audio, 0, ClipDuration.TotalSeconds);
                rubberBandWaveStream = new RubberBandWaveStream(loopStream);
                volumeStream = new VolumeSampleProvider(rubberBandWaveStream.ToSampleProvider());

                ID = ++UNIQUE_ID;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void JumpToPosition(TimeSpan position)
        {
            loopStream.JumpToSeconds(position.TotalSeconds);
        }

        public void SetVolumte(float volume)
        {
            volumeStream.Volume = volume;
        }

        public void Dispose()
        {
            audio?.Dispose();
            loopStream?.Dispose();
            rubberBandWaveStream = null;
            volumeStream = null;
        }
    }
}