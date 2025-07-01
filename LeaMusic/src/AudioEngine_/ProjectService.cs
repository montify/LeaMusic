using LeaMusic.src.ResourceManager_;
using LeaMusic.src.ResourceManager_.GoogleDrive_;

namespace LeaMusic.src.AudioEngine_
{
    public class ProjectService
    {
        private IDialogService DialogService;
        private LeaResourceManager ResourceManager;

        public ProjectService(IDialogService dialogService, LeaResourceManager resourceManager)
        {
            if (dialogService == null || resourceManager == null)
                throw new NullReferenceException("DialogService or ResourceManager are null");

            DialogService = dialogService;
            ResourceManager = resourceManager;
        }

        public async Task<Project?> LoadProjectAsync(bool isGoogleDriveSync, Action<string>? statusCallback)
        {
            var dialogResult = DialogService.OpenFile("Project (*.prj)|*.prj");
            if (string.IsNullOrEmpty(dialogResult))
                return null;


            var fileHandler = new FileHandler();

            var location = new FileLocation(dialogResult);
            var projectName = Path.GetFileNameWithoutExtension(location.Path);

            var googleDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

            //Fetch project Metadata, and compare on Date
            ProjectMetadata? fileMetaData = ResourceManager.GetProjectMetaData($"{projectName}.zip", location, fileHandler);
            ProjectMetadata? gDriveMetaData = ResourceManager.GetProjectMetaData($"{projectName}", null, googleDriveHandler);

            Project project = null;

            if (isGoogleDriveSync &&
                        gDriveMetaData?.lastSavedAt > fileMetaData?.lastSavedAt &&
                        DialogService.AskDownloadGoogleDrive(localDate: fileMetaData.lastSavedAt, googleDriveDate: gDriveMetaData.lastSavedAt))
            {
                var gdriveLocation = new GDriveLocation("LeaRoot", dialogResult, projectName);

                statusCallback?.Invoke("Loading Project from google Drive");
                project = await ResourceManager.LoadProject(gdriveLocation, googleDriveHandler);
            }
            else
            {
                project = await ResourceManager.LoadProject(new FileLocation(dialogResult), fileHandler);
                statusCallback?.Invoke("Loading Project from File");
            }

            return project;
        }

        public async Task SaveProject(Project project, Action<string>? statusCallback)
        {           
            if (project.Duration == TimeSpan.FromSeconds(1))
                return;

            var oldLastSave = project.LastSaveAt;
            project.LastSaveAt = DateTime.Now;

            try
            {
                string? dialogResult = DialogService.Save();

                if (!string.IsNullOrEmpty(dialogResult))
                {
                    var fileHandler = new FileHandler();

                    ResourceManager.SaveProject(project, new FileLocation(dialogResult), fileHandler);

                    if (DialogService.EnableSync())
                    {
                        var gDriveHandler = new GoogleDriveHandler("LeaRoot", fileHandler);

                        statusCallback?.Invoke("Start Save Project to Google Drive");

                        await ResourceManager.SaveProject(project, default, gDriveHandler);

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