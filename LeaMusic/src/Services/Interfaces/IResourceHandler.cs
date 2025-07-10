namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.ResourceServices_;

    public interface IResourceHandler
    {
        public Track ImportTrack(Location trackLocation);

        public Track LoadAudio(Track track, string projectPath);

        public Task SaveProject(Location projectLocation, Project project);

        public Task<Project?> LoadProject(Location projectLocation);

        public Task<ProjectMetadata?> GetProjectMetadata(string projectName, Location location);
    }
}
