namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;
    using NAudio.Wave;

    public interface ILocalFileHandler : IResourceHandler
    {
        WaveStream LoadAudioFromFile(string path);

        Track LoadAudio(Track track, string projectPath);
    }
}
