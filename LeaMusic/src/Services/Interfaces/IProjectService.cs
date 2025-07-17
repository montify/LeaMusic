namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.ResourceServices_;

    public interface IProjectService
    {
        public Task<Project?> LoadProjectAsync(bool isGoogleDriveSync, Action<string>? statusCallback);

        public Task SaveProject(Project project, Action<string>? statusCallback);

        public Track? ImportTrack(Location location);
    }
}
