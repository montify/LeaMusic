namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class WaveformService : IWaveformService
    {
        public WaveformProvider GenerateFromAudio(Track track)
        {
            var resampler = new WdlResamplingSampleProvider(track.Audio.ToSampleProvider(), 3000);
            return new WaveformProvider(resampler, (int)track.Audio.TotalTime.TotalSeconds);
        }
    }
}
