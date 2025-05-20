namespace LeaMusic.src.ResourceManager_
{
    public class DatabaseHandler : IHandler
    {

        public DatabaseHandler(string address, string username, string password)
        {
            
        }

        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager)
        {
            if(trackLocation is DatabaseLocation databaseLocation)
            {
                Console.WriteLine(  );
            }

            return default;
        }

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager)
        {
            throw new NotImplementedException();
        }

        public Task<Project> LoadProjectFromFileAsync(Location projectLocation, LeaResourceManager resourceManager)
        {
            throw new NotImplementedException();
        }

        public void SaveProject(Location projectLocation, Project project)
        {
            throw new NotImplementedException();
        }
    }
}
