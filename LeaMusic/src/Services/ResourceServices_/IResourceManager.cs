namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
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
