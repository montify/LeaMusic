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

    public class DatabaseLocation : Location
    {
        public string DatabaseAddres { get; set; }

        public DatabaseLocation(string path, string username, string userPassword)
        {

        }

        internal bool CheckUserCredentials()
        {
            throw new NotImplementedException();
        }

    }
}
