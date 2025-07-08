namespace LeaMusic.src.ResourceManager_
{
    using LeaMusic.src.AudioEngine_;

    public interface IResourceHandler
    {
        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager);

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager);

        public Task SaveProject(Location projectLocation, Project project);

        public Task<Project?> LoadProject(Location projectLocation, LeaResourceManager resourceManager);

        public Task<ProjectMetadata?> GetProjectMetadata(string projectName, Location location, LeaResourceManager resourceManager);
    }
}
