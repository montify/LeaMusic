using LeaMusic.src.Services.ResourceServices_;

namespace LeaMusic.src.Services.Interfaces
{
    public interface IMetadataService
    {
        Task<ProjectMetadata?> GetMetaData(Location projectLocation);
    }
}
