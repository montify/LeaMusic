namespace LeaMusic.src.AudioEngine_
{
    using System.Text.Json.Serialization;
    using LeaMusic.src.AudioEngine_.Streams;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class Track : IDisposable
    {
        private static int m_uniqueId = 0;

        [JsonIgnore]
        public WaveStream Audio { get; set; }

        [JsonIgnore]
        public RubberBandWaveStream RubberBandWaveStream { get; private set; }

        [JsonIgnore]
        public WaveformProvider WaveformProvider { get; set; }

        [JsonIgnore]
        public VolumeSampleProvider VolumeStream { get; private set; }

        [JsonIgnore]
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

        public bool IsSolo { get; set; }

        public float Volume { get; set; }

        public float? PreviousVolume { get; set; }

        public Track() { }

        public void AddAudioFile(string audioFilePath, WaveStream audio)
        {
            Audio = audio;
            OriginFilePath = audioFilePath;

            AudioFileName = Path.GetFileName(audioFilePath);

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

        public void SetVolume(float volume)
        {
            Volume = volume;
            VolumeStream.Volume = volume;
        }

        public void Mute()
        {
            if (!IsMuted)
            {
                PreviousVolume = Volume;
                SetVolume(0);
                IsMuted = true;
            }
        }

        public void Unmute()
        {
            if (IsMuted)
            {
                SetVolume(PreviousVolume ?? Volume);
                PreviousVolume = null;
                IsMuted = false;
            }
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
