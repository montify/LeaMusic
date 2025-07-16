namespace LeaMusic.src.Services.Interfaces
{
    using LeaMusic.src.Services.ResourceServices_;

    public interface IMetadataService
    {
        Task<ProjectMetadata?> GetMetaData(Location projectLocation);
    }
}
