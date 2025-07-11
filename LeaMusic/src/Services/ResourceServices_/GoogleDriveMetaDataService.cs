namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_.GoogleDrive_;

    public class GoogleDriveMetaDataService : IMetadataService
    {
        private readonly IGoogleContext m_googleContext;

        public GoogleDriveMetaDataService(IGoogleContext googleDriveHandler)
        {
            m_googleContext = googleDriveHandler;
        }

        public async Task<ProjectMetadata?> GetMetaData(Location projectLocation)
        {
            if (projectLocation is GDriveLocation googleDriveLocation)
            {
                try
                {
                    var rootFolderId = m_googleContext.GetFolderIdByName(AppConstants.GoogleDriveRootFolderName);

                    if (string.IsNullOrEmpty(rootFolderId))
                    {
                        throw new Exception("Cant find rootFolder");
                    }

                    var rawMetaData = m_googleContext.GetFileMetadataByNameInFolder(googleDriveLocation.ProjectName + ".zip", AppConstants.GoogleDriveRootFolderName);

                    if (rawMetaData == null)
                    {
                        return await Task.FromResult<ProjectMetadata?>(null);
                    }

                    var metaData = new ProjectMetadata(rawMetaData.Value.Name, rawMetaData.Value.CreatedTime.Value);

                    return await Task.FromResult(metaData);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                throw new Exception("Location is not a GoogleDrive Location");
            }
        }
    }
}
