using LeaMusic.src.Services.ResourceServices_;

namespace LeaMusic.src.Services.Interfaces
{
    public interface IMetadataService
    {
        ProjectMetadata? GetMetaData(Location projectLocation);
    }
}
