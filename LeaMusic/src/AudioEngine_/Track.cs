namespace LeaMusic.src.AudioEngine_
{
    using System.Text.Json.Serialization;
    using LeaMusic.src.AudioEngine_.Streams;
    using LeaMusic.src.ResourceManager_;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class Track : IDisposable
    {
        private static int m_uniqueId = 0;

        public WaveStream Audio { get; private set; }

        public RubberBandWaveStream RubberBandWaveStream { get; private set; }

        public WaveformProvider WaveformProvider { get; set; }

        public VolumeSampleProvider VolumeStream { get; private set; }

        public LoopStream LoopStream { get; private set; }
    
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

            Audio = resourceManager.LoadAudioFile(audioFilePath);
            Name = Path.GetFileNameWithoutExtension(OriginFilePath);

            Waveformat = Audio.WaveFormat;
            ClipDuration = Audio.TotalTime;

            try
            {
                LoopStream = new LoopStream(Audio, 0, ClipDuration.TotalSeconds);
                RubberBandWaveStream = new RubberBandWaveStream(LoopStream);
                VolumeStream = new VolumeSampleProvider(RubberBandWaveStream.ToSampleProvider());

                ID = ++m_uniqueId;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void JumpToPosition(TimeSpan position)
        {
            LoopStream.JumpToSeconds(position.TotalSeconds);
        }

        public void SetVolumte(float volume)
        {
            VolumeStream.Volume = volume;
        }

        public void Dispose()
        {
            Audio?.Dispose();
            LoopStream?.Dispose();
            RubberBandWaveStream = null;
            VolumeStream = null;
        }
    }
}