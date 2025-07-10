using LeaMusic.src.ResourceManager_;

namespace LeaMusic.src.Services.ResourceServices_
{
    public interface IMetadataService
    {
        ProjectMetadata? GetMetaData(Location projectLocation);
    }
}
