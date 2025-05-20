namespace LeaMusic.src.ResourceManager_
{
    public class Location
    {
    }

    public class FileLocation : Location
    {
        public string Path { get; set; }
        public FileLocation(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("Path cant be null");

            Path = path;
        }
    }

    public class GDriveLocation : Location
    {
        public string LeaRootPath { get; set; }

        public string ProjectName { get; set; }

        public string LocalPath { get; set; }

        public GDriveLocation(string leaRootPath, string localPath, string projectName)
        {
            LeaRootPath = leaRootPath;
            ProjectName = projectName;
            LocalPath = localPath;
        }

      

    }
}
