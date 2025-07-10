namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;

    // Import: Create a track from an audio file that does not yet exist in the project (generate a new waveform file).
    // Load: Load a track from an existing project (load an existing waveform file).
    public class LeaResourceManager : IResourceManager
    {
        // TODO: Cache Resources!
        public async Task<Project> LoadProject(Location location, IResourceHandler handler)
        {
            return await handler.LoadProject(location);
        }

        public async Task SaveProject(Project project, Location projectFilePath, IResourceHandler handler)
        {
           await handler.SaveProject(projectFilePath, project);
        }

        // Import: When the Audio IS NOT in the Project/Audio Folder
        // Load: When the Audio IS IN the Project/Audio Folder
        // So Import copy the AudioFile from Source to Project/audioFolder
        public Track ImportTrack(Location trackLocation, ILocalFileHandler handler)
        {
           return handler.ImportTrack(trackLocation);
        }

        public ProjectMetadata? GetProjectMetaData(string projectName, Location location, IResourceHandler handler)
        {
            return handler.GetProjectMetadata(projectName, location)?.Result;
        }

        public WaveStream LoadAudioFile(string path)
        {
            return new Mp3FileReader(path);
        }
    }
}