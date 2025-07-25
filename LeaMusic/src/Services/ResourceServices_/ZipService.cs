﻿namespace LeaMusic.src.Services.ResourceServices_
{
    using System.IO.Compression;
    using LeaMusic.src.Services.Interfaces;

    public class ZipService : IZipService
    {
        public void CreateFromDirectory(string sourceDirectory, string destinationArchiveFileName)
        {
            ZipFile.CreateFromDirectory(
                sourceDirectory,
                destinationArchiveFileName,
                CompressionLevel.Optimal,
                true
            );
        }

        public void ExtractToDirectory(Stream stream, string destinationDirectory)
        {
            ZipFile.ExtractToDirectory(stream, destinationDirectory, overwriteFiles: true);
        }
    }
}
