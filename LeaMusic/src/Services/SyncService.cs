namespace LeaMusic.src.Services
{
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;

    public class SyncService : ISyncService
    {
        private readonly IResourceManager m_resourceManager;
        private readonly ConnectionMonitorService m_connectionMonitorService;
        private readonly IDialogService m_dialogService;

        public SyncService(
            IResourceManager resourceManager,
            ConnectionMonitorService connectionMonitorService,
            IDialogService dialogService,
            IGoogleDriveHandler googleDriveHandler)
        {
            m_resourceManager = resourceManager;
            m_connectionMonitorService = connectionMonitorService;
            m_dialogService = dialogService;
        }

        public async Task<bool> DetermineSyncLocationAsync(string projectName, Location location, Action<string> statusCallback)
        {
            ProjectMetadata? localMeta = m_resourceManager.GetProjectMetaData($"{projectName}.zip", location);
            ProjectMetadata? gDriveMeta = await TryGetGoogleDriveMetadataAsync(projectName, statusCallback);

            bool shouldUseGDrive = gDriveMeta != null &&
                                   localMeta?.lastSavedAt < gDriveMeta.lastSavedAt &&
                                   m_dialogService.AskDownloadGoogleDrive(localDate: localMeta.lastSavedAt, googleDriveDate: gDriveMeta.lastSavedAt);

            return shouldUseGDrive;
        }

        private async Task<ProjectMetadata?> TryGetGoogleDriveMetadataAsync(string projectName, Action<string>? statusCallback)
        {
            if (!await m_connectionMonitorService.CheckInternetConnection())
            {
                return null;
            }

            try
            {
                var location = new GDriveLocation(AppConstants.GoogleDriveRootFolderName, null, projectName);
                return m_resourceManager.GetProjectMetaData(projectName, location);
            }
            catch
            {
                statusCallback?.Invoke("No Internet Connection, load Project from Local File");
                return null;
            }
        }
    }
}