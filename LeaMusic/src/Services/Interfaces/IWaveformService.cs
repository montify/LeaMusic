namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;

    public interface IWaveformService
    {
        WaveformProvider GenerateFromAudio(Track track);
    }
}
