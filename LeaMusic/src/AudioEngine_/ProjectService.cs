using LeaMusic.src.ResourceManager_;
using LeaMusic.src.ResourceManager_.GoogleDrive_;

namespace LeaMusic.src.AudioEngine_
{
    public class ProjectService
    {
        private IDialogService m_dialogService;
        private LeaResourceManager m_resourceManager;

        public ProjectService(IDialogService dialogService, LeaResourceManager resourceManager)
        {
            if (dialogService == null || resourceManager == null)
            {
                throw new NullReferenceException("DialogService or ResourceManager are null");
            }

            m_dialogService = dialogService;
            m_resourceManager = resourceManager;
        }

        public async Task<Project?> LoadProjectAsync(bool isGoogleDriveSync, Action<string>? statusCallback)
        {
            var dialogResult = m_dialogService.OpenFile("Project (*.prj)|*.prj");
            if (string.IsNullOrEmpty(dialogResult))
            {
                return null;
            }

            var fileHandler = new FileHandler();

            var location = new FileLocation(dialogResult);
            var projectName = Path.GetFileNameWithoutExtension(location.Path);

            var googleDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

            // Fetch project Metadata, and compare on Date
            ProjectMetadata? fileMetaData = m_resourceManager.GetProjectMetaData($"{projectName}.zip", location, fileHandler);

            ProjectMetadata? gDriveMetaData = null;

            try
            {
               gDriveMetaData = m_resourceManager.GetProjectMetaData($"{projectName}", null, googleDriveHandler);
            }
            catch (Exception)
            {
                statusCallback?.Invoke("No Internet Connection, load Project from Local File");
                isGoogleDriveSync = false;
            }

            Project? project = null;

            if (isGoogleDriveSync &&
                        gDriveMetaData?.lastSavedAt > fileMetaData?.lastSavedAt &&
                        m_dialogService.AskDownloadGoogleDrive(localDate: fileMetaData.lastSavedAt, googleDriveDate: gDriveMetaData.lastSavedAt))
            {
                var gdriveLocation = new GDriveLocation("LeaRoot", dialogResult, projectName);

                statusCallback?.Invoke("Loading Project from google Drive");
                project = await m_resourceManager.LoadProject(gdriveLocation, googleDriveHandler);
            }
            else
            {
                project = await m_resourceManager.LoadProject(new FileLocation(dialogResult), fileHandler);
                statusCallback?.Invoke("Loading Project from File");
            }

            return project;
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
                string? dialogResult = m_dialogService.Save();

                if (!string.IsNullOrEmpty(dialogResult))
                {
                    var fileHandler = new FileHandler();

                    await m_resourceManager.SaveProject(project, new FileLocation(dialogResult), fileHandler);

                    if (m_dialogService.EnableSync())
                    {
                        var gDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

                        statusCallback?.Invoke("Start Save Project to Google Drive");

                        await m_resourceManager.SaveProject(project, default, gDriveHandler);

                        statusCallback?.Invoke("Project successfully saved to GoogleDrive");
                    }
                }
            }
            catch (Exception e)
            {
                project.LastSaveAt = oldLastSave;
                statusCallback?.Invoke(e.Message);
                // StatusMessages = $"Error: {e.Message}";
                return;
            }
        }
    }
}