namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;

    public interface IWaveformService
    {
        WaveformProvider GenerateFromAudio(Track track);

        byte[] GetWaveformData(WaveformProvider provider);

        WaveformProvider LoadFromData(byte[] data);
    }
}
