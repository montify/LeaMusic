namespace LeaMusic.src.Services
{
    using System.Diagnostics;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class ProjectService : IProjectService
    {
        private readonly IDialogService m_dialogService;
        private readonly IResourceManager m_resourceManager;
        private readonly IConnectionMonitorService m_connectionMonitorService;
        private readonly IFileSystemService m_fileSystemService;
        private readonly ISyncService m_syncService;

        public ProjectService(
            IDialogService dialogService,
            IResourceManager resourceManager,
            IConnectionMonitorService connectionMonitorService,
            IFileSystemService fileSystemService,
            ISyncService syncService)
        {
            if (dialogService == null || resourceManager == null)
            {
                throw new NullReferenceException("DialogService or ResourceManager are null");
            }

            m_dialogService = dialogService;
            m_resourceManager = resourceManager;
            m_connectionMonitorService = connectionMonitorService;
            m_fileSystemService = fileSystemService;
            m_syncService = syncService;
        }

        public async Task<Project?> LoadProjectAsync(bool isGoogleDriveSync, Action<string>? statusCallback)
        {
            var filePath = m_dialogService.OpenFile("Project (*.prj)|*.prj");

            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var location = new FileLocation(filePath);
            var projectName = m_fileSystemService.GetFileNameWithoutExtension(location.Path);

            bool shouldUseGDrive = await m_syncService.DetermineSyncLocationAsync(projectName, location, statusCallback);

            if (shouldUseGDrive)
            {
                var gdriveLocation = new GDriveLocation("LeaRoot", filePath, projectName);

                statusCallback?.Invoke("Loading Project from google Drive");
                return await m_resourceManager.LoadProject(gdriveLocation);
            }

            statusCallback?.Invoke("Loading Project from File");

            return await m_resourceManager.LoadProject(location);
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

                await SaveLocalAsync(project, filePath);

                if (!await m_connectionMonitorService.CheckInternetConnection())
                {
                    statusCallback?.Invoke("No Internet Connection, cant Sync");
                    return;
                }

                if (m_dialogService.EnableSync())
                {
                    await SaveToGoogleDriveAsync(project, statusCallback);
                }
            }
            catch (Exception e)
            {
                project.LastSaveAt = oldLastSave;
                statusCallback?.Invoke(e.Message);
            }
        }

        public Track? ImportTrack(Location location)
        {
            if (location is FileLocation projectFilePath)
            {
                var track = new Track();
                track.OriginFilePath = projectFilePath.Path;

                var trackLocation = new FileLocation(projectFilePath.Path);
                var audio = m_resourceManager.ImportTrack(trackLocation);

                track.AddAudioFile(projectFilePath.Path, audio.Audio);
                track.WaveformProvider = ImportWaveform(track);

                return track;
            }
            else
            {
                throw new NotSupportedException("Handler is not a FileHandler");
            }
        }

        private async Task SaveLocalAsync(Project project, string filePath)
        {
            await m_resourceManager.SaveProject(project, new FileLocation(filePath));
        }

        private async Task SaveToGoogleDriveAsync(Project project, Action<string>? statusCallback)
        {
            statusCallback?.Invoke("Start Save Project to Google Drive");

            var gDriveLocation = new GDriveLocation(AppConstants.GoogleDriveRootFolderName, null, project.Name);
            await m_resourceManager.SaveProject(project, gDriveLocation);

            statusCallback?.Invoke("Project successfully saved to GoogleDrive");
        }

        private ISampleProvider ResampleWav(WaveStream wavestream)
        {
            var resampledAudio = new WdlResamplingSampleProvider(wavestream.ToSampleProvider(), 3000);

            return resampledAudio;
        }

        private WaveformProvider ImportWaveform(Track track)
        {
            var downsampleAudio = ResampleWav(track.Audio);
            var waveformProvider = new WaveformProvider(downsampleAudio, (int)track.Audio.TotalTime.TotalSeconds);

            Debug.WriteLine($"Create new Waveform from new ImportedTrack: {track.AudioFileName}");
            return waveformProvider;
        }
    }
}