namespace LeaMusic.src.Services.ResourceServices_
{
    public interface IZipService
    {
        void ExtractToDirectory(Stream stream, string destinationDirectory);

        void CreateFromDirectory(string sourceDirectory, string destinationArchiveFileName);
    }
}
