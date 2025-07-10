namespace LeaMusic.src.Services
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;

    public class SyncService : ISyncService
    {
        private readonly IResourceManager m_resourceManager;
        private readonly ConnectionMonitorService m_connectionMonitorService;
        private readonly IDialogService m_dialogService;

        private readonly IGoogleDriveHandler m_googleDriveHandler;
        private readonly ILocalFileHandler m_localFileHandler;

        public SyncService(
            IResourceManager resourceManager,
            ConnectionMonitorService connectionMonitorService,
            IDialogService dialogService,
            IGoogleDriveHandler googleDriveHandler,
            ILocalFileHandler localFileHandler)
        {
            m_resourceManager = resourceManager;
            m_connectionMonitorService = connectionMonitorService;
            m_dialogService = dialogService;
            m_localFileHandler = localFileHandler;
            m_googleDriveHandler = googleDriveHandler;
        }

        public async Task<bool> DetermineSyncLocationAsync(string projectName, Location location, Action<string> statusCallback)
        {
            ProjectMetadata? localMeta = m_resourceManager.GetProjectMetaData($"{projectName}.zip", location, m_localFileHandler);
            ProjectMetadata? gDriveMeta = await TryGetGoogleDriveMetadataAsync(projectName, m_googleDriveHandler, statusCallback);

            bool shouldUseGDrive = gDriveMeta != null &&
                                   localMeta?.lastSavedAt < gDriveMeta.lastSavedAt &&
                                   m_dialogService.AskDownloadGoogleDrive(localDate: localMeta.lastSavedAt, googleDriveDate: gDriveMeta.lastSavedAt);

            return shouldUseGDrive;
        }

        private async Task<ProjectMetadata?> TryGetGoogleDriveMetadataAsync(string projectName, IGoogleDriveHandler handler, Action<string>? statusCallback)
        {
            if (!await m_connectionMonitorService.CheckInternetConnection())
            {
                return null;
            }

            try
            {
                return m_resourceManager.GetProjectMetaData(projectName, null, handler);
            }
            catch
            {
                statusCallback?.Invoke("No Internet Connection, load Project from Local File");
                return null;
            }
        }

    }
}
