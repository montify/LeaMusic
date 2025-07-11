using LeaMusic.src.Services.ResourceServices_;

namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;
    using NAudio.Wave;

    public interface IResourceManager
    {
        Task<Project> LoadProject(Location location, IResourceHandler handler);

        Task SaveProject(Project project, Location projectFilePath, IResourceHandler handler);

        Track ImportTrack(Location trackLocation, ILocalFileHandler handler);

        ProjectMetadata? GetProjectMetaData(string projectName, Location location, IResourceHandler handler);

        WaveStream LoadAudioFile(string path);
    }
}
