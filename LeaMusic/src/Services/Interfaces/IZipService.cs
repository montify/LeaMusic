namespace LeaMusic.src.Services.Interfaces
{
    public interface IZipService
    {
        void ExtractToDirectory(Stream stream, string destinationDirectory);

        void CreateFromDirectory(string sourceDirectory, string destinationArchiveFileName);
    }
}
