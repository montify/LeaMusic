using LeaMusic.src.Services.Interfaces;

namespace LeaMusic.src.Services.ResourceServices_
{
    public class LocalFileSystemService : IFileSystemService
    {
        public async Task<string> ReadAllTextAsync(string path) =>
            await File.ReadAllTextAsync(path);

        public async Task WriteAllTextAsync(string path, string contents) =>
            await File.WriteAllTextAsync(path, contents);

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
            File.Copy(sourcePath, destinationPath, overwrite);

        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);

        public DirectoryInfo GetDirectoryInfo(string path) => new DirectoryInfo(path);

        public string CombinePaths(params string[] paths) => Path.Combine(paths);

        public string GetFileName(string path) => Path.GetFileName(path);

        public string GetFileNameWithoutExtension(string path) =>
            Path.GetFileNameWithoutExtension(path);

        public string GetDirectoryName(string path) => Path.GetDirectoryName(path);

        public void WriteBytes(string path, byte[] data) => File.WriteAllBytes(path, data);

        public void DeleteFile(string path) => File.Delete(path);

        public void DeleteDirectoryRecursive(string path) => Directory.Delete(path, true);

        // Implement WriteWaveformBinary logic here as well, possibly by taking a float[] and converting to byte[]
    }
}
