namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;
    using NAudio.Wave;

    public interface IMixer : ISampleProvider
    {
        void RemoveAllMixerInputs();

        void AddMixerInput(Track track);
    }
}
