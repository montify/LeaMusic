using LeaMusic.src.AudioEngine_;

namespace LeaMusic.src.ResourceManager_
{
    public interface IResourceHandler
    {
        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager);
        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager);
        public Task SaveProject(Location projectLocation, Project project);
        public Task<Project> LoadProject(Location projectLocation, LeaResourceManager resourceManager);
    }
}
