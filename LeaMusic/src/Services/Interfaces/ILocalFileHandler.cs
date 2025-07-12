using NAudio.Wave;

namespace LeaMusic.src.Services.Interfaces
{
    public interface ILocalFileHandler : IResourceHandler
    {
        WaveStream LoadAudioFromFile(string path);
    }
}
