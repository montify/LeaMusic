namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.ResourceServices_;

    public interface IResourceManager
    {
        Task<Project> LoadProject(Location location);

        Task SaveProject(Project project, Location projectFilePath);

        Track ImportTrack(Location trackLocation);

        ProjectMetadata? GetProjectMetaData(string projectName, Location location);
    }
}
