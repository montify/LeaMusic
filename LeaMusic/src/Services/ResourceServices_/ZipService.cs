
namespace LeaMusic.src.Services.ResourceServices_
{
    using System.IO.Compression;

    public class ZipService : IZipService
    {
        private readonly IFileSystemService m_fileSystemService;

        public ZipService(IFileSystemService fileSystemService)
        {
            m_fileSystemService = fileSystemService;
        }

        public void CreateFromDirectory(string sourceDirectory, string destinationArchiveFileName)
        {
            ZipFile.CreateFromDirectory(sourceDirectory, destinationArchiveFileName, CompressionLevel.Optimal, true);
        }

        public void ExtractToDirectory(Stream stream, string destinationDirectory)
        {
            ZipFile.ExtractToDirectory(stream, m_fileSystemService.GetDirectoryName(destinationDirectory), overwriteFiles: true);
        }
    }
}
