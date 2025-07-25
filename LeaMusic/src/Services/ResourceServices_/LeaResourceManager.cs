namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;

    // Import: Create a track from an audio file that does not yet exist in the project (generate a new waveform file).
    // Load: Load a track from an existing project (load an existing waveform file).
    public class LeaResourceManager : IResourceManager
    {
        private readonly ILocalFileHandler m_localFileHandler;
        private readonly IGoogleDriveHandler m_googleDriveHandler;

        public LeaResourceManager(
            ILocalFileHandler localFileHandler,
            IGoogleDriveHandler googleDriveHandler
        )
        {
            m_localFileHandler = localFileHandler;
            m_googleDriveHandler = googleDriveHandler;
        }

        // TODO: Cache Resources!
        public async Task<Project?> LoadProject(Location location)
        {
            if (location is FileLocation fileLocation)
            {
                return await m_localFileHandler.LoadProject(fileLocation);
            }
            else if (location is GDriveLocation gDriveLocation)
            {
                return await m_googleDriveHandler.LoadProject(gDriveLocation);
            }

            throw new NotSupportedException("Unknown location type");
        }

        public async Task SaveProject(Project project, Location location)
        {
            if (location is FileLocation fileLocation)
            {
                await m_localFileHandler.SaveProject(fileLocation, project);
                return;
            }
            else if (location is GDriveLocation gDriveLocation)
            {
                await m_googleDriveHandler.SaveProject(gDriveLocation, project);
                return;
            }

            throw new NotSupportedException("Unknown location type");
        }

        // Import: When the Audio IS NOT in the Project/Audio Folder
        // Load: When the Audio IS IN the Project/Audio Folder
        // So Import copy the AudioFile from Source to Project/audioFolder
        public Track ImportTrack(Location trackLocation)
        {
            return m_localFileHandler.ImportTrack(trackLocation);
        }

        public ProjectMetadata? GetProjectMetaData(string projectName, Location location)
        {
            if (location is FileLocation fileLocation)
            {
                return m_localFileHandler.GetProjectMetadata(projectName, fileLocation).Result;
            }
            else if (location is GDriveLocation gDriveLocation)
            {
                return m_googleDriveHandler.GetProjectMetadata(projectName, location)?.Result;
            }

            throw new NotSupportedException("Unknown location type");
        }
    }
}
