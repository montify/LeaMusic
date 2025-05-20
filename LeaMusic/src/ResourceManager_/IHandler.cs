namespace LeaMusic.src.ResourceManager_
{
    public interface IHandler
    {
        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager);
        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager);
        public void SaveProject(Location projectLocation, Project project);
        public Task<Project> LoadProjectFromFileAsync(Location projectLocation, LeaResourceManager resourceManager);
    }
}
