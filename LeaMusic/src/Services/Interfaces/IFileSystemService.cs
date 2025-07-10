namespace LeaMusic.src.Services.Interfaces
{
    public interface IFileSystemService
    {
        Task<string> ReadAllTextAsync(string path);

        Task WriteAllTextAsync(string path, string contents); 

        void CopyFile(string sourcePath, string destinationPath, bool overwrite);

        bool FileExists(string path);

        bool DirectoryExists(string path);

        DirectoryInfo CreateDirectory(string path);

        DirectoryInfo GetDirectoryInfo(string path); 

        string CombinePaths(params string[] paths);

        string GetFileName(string path);

        string GetFileNameWithoutExtension(string path);

        string GetDirectoryName(string path);

        void WriteBytes(string path, byte[] data);

        void DeleteFile(string path);

        void DeleteDirectoryRecursive(string path);
    }
}
