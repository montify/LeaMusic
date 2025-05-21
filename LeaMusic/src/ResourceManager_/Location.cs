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
        public string rootFolderPath { get; set; }

        public string ProjectName { get; set; }

        public string LocalPath { get; set; }

        public GDriveLocation(string rootFolder, string localPath, string projectName)
        {
            this.rootFolderPath = rootFolder;
            ProjectName = projectName;
            LocalPath = localPath;
        }

      

    }
}
