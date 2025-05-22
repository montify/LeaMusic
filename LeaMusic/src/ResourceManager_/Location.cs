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
        public string gDriverootFolderPath { get; set; }

        public string ProjectName { get; set; }

        public string LocalProjectFilePath { get; set; }

        public GDriveLocation(string gDriveRootFolder, string localProjectFilePath, string projectName)
        {
            this.gDriverootFolderPath = gDriveRootFolder;
            ProjectName = projectName;
            LocalProjectFilePath = localProjectFilePath;
        }

      

    }
}
