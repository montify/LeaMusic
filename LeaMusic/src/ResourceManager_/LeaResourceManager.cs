using LeaMusic.src.AudioEngine_;
using NAudio.Wave;

namespace LeaMusic.src.ResourceManager_
{
    // Import: Create a track from an audio file that does not yet exist in the project (generate a new waveform file).
    // Load: Load a track from an existing project (load an existing waveform file).
    public class LeaResourceManager
    {
        //TODO: Cache Resources!
        //TODO: Relative Paths!

        public async Task<Project> LoadProject(Location location, IResourceHandler handler)
        {
            return await handler.LoadProject(location, this);
        }

        public void SaveProject(Project project, Location projectFilePath, IResourceHandler handler)
        {
            handler.SaveProject(projectFilePath, project);
        }

        //Import: When the Audio IS NOT in the Project/Audio Folder
        //Load: When the Audio IS IN the Project/Audio Folder
        //So Import copy the AudioFile from Source to Project/audioFolder
        public Track ImportTrack(Location trackLocation, IResourceHandler handler)
        {
           return  handler.ImportTrack(trackLocation, this); 
        }

        internal WaveStream LoadAudioFile(string path)
        {
            return new Mp3FileReader(path);
        }
    }
}