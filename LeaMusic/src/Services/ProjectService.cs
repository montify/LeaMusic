namespace LeaMusic.src.Services
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.ResourceManager_;
    using LeaMusic.src.ResourceManager_.GoogleDrive_;

    public class ProjectService
    {
        private IDialogService m_dialogService;
        private LeaResourceManager m_resourceManager;
        private ConnectionMonitorService m_connectionMonitorService;

        public ProjectService(IDialogService dialogService, LeaResourceManager resourceManager, ConnectionMonitorService connectionMonitorService)
        {
            if (dialogService == null || resourceManager == null)
            {
                throw new NullReferenceException("DialogService or ResourceManager are null");
            }

            m_dialogService = dialogService;
            m_resourceManager = resourceManager;
            m_connectionMonitorService = connectionMonitorService;
        }

        public async Task<Project?> LoadProjectAsync(bool isGoogleDriveSync, Action<string>? statusCallback)
        {
            var filePath = m_dialogService.OpenFile("Project (*.prj)|*.prj");

            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var location = new FileLocation(filePath);
            var projectName = Path.GetFileNameWithoutExtension(location.Path);

            var fileHandler = new FileHandler();
            var googleDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

            ProjectMetadata? localMeta = m_resourceManager.GetProjectMetaData($"{projectName}.zip", location, fileHandler);
            ProjectMetadata? gDriveMeta = await TryGetGoogleDriveMetadataAsync(projectName, googleDriveHandler, statusCallback);

            bool shouldUseGDrive = gDriveMeta != null &&
                                   localMeta?.lastSavedAt < gDriveMeta.lastSavedAt &&
                                   m_dialogService.AskDownloadGoogleDrive(localDate: localMeta.lastSavedAt, googleDriveDate: gDriveMeta.lastSavedAt);

            if (shouldUseGDrive)
            {
                var gdriveLocation = new GDriveLocation("LeaRoot", filePath, projectName);

                statusCallback?.Invoke("Loading Project from google Drive");
                return await m_resourceManager.LoadProject(gdriveLocation, googleDriveHandler);
            }

            statusCallback?.Invoke("Loading Project from File");

            return await m_resourceManager.LoadProject(location, fileHandler);
        }

        public async Task SaveProject(Project project, Action<string>? statusCallback)
        {
            if (project.Duration == TimeSpan.FromSeconds(1))
            {
                return;
            }

            var oldLastSave = project.LastSaveAt;
            project.LastSaveAt = DateTime.Now;

            try
            {
                string? filePath = m_dialogService.Save();

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                var fileHandler = new FileHandler();

                await SaveLocalAsync(project, filePath, fileHandler);

                if (!await m_connectionMonitorService.CheckInternetConnection())
                {
                    statusCallback?.Invoke("No Internet Connection, cant Sync");
                    return;
                }

                if (m_dialogService.EnableSync())
                {
                    await SaveToGoogleDriveAsync(project, statusCallback, fileHandler);   
                }
            }
            catch (Exception e)
            {
                project.LastSaveAt = oldLastSave;
                statusCallback?.Invoke(e.Message);
            }
        }

        private async Task<ProjectMetadata?> TryGetGoogleDriveMetadataAsync(string projectName, GoogleDriveHandler handler, Action<string>? statusCallback)
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

        private async Task SaveLocalAsync(Project project, string filePath, FileHandler fileHandler)
        {
            await m_resourceManager.SaveProject(project, new FileLocation(filePath), fileHandler);
        }

        private async Task SaveToGoogleDriveAsync(Project project, Action<string>? statusCallback, FileHandler fileHandler)
        {
            var gDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

            statusCallback?.Invoke("Start Save Project to Google Drive");

            await m_resourceManager.SaveProject(project, default, gDriveHandler);

            statusCallback?.Invoke("Project successfully saved to GoogleDrive");
        }
    }
}