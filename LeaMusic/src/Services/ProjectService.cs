namespace LeaMusic.src.Services
{
    using System.Diagnostics;
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using LeaMusic.src.Services.ResourceServices_;
    using NAudio.Wave;
    using NAudio.Wave.SampleProviders;

    public class ProjectService
    {
        private readonly IDialogService m_dialogService;
        private readonly IResourceManager m_resourceManager;
        private readonly ConnectionMonitorService m_connectionMonitorService;
        private readonly ILocalFileHandler m_localFileHandler;
        private readonly IGoogleDriveHandler m_googleDriveHandler;
        private readonly IFileSystemService m_fileSystemService;
        private readonly ISyncService m_syncService;

        public ProjectService(
            IDialogService dialogService,
            IResourceManager resourceManager,
            ConnectionMonitorService connectionMonitorService,
            ILocalFileHandler localFileHandler,
            IGoogleDriveHandler googleDriveHandler,
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
            m_localFileHandler = localFileHandler;
            m_googleDriveHandler = googleDriveHandler;
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
                return await m_resourceManager.LoadProject(gdriveLocation, m_googleDriveHandler);
            }

            statusCallback?.Invoke("Loading Project from File");

            return await m_resourceManager.LoadProject(location, m_localFileHandler);
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

                await SaveLocalAsync(project, filePath, m_localFileHandler);

                if (!await m_connectionMonitorService.CheckInternetConnection())
                {
                    statusCallback?.Invoke("No Internet Connection, cant Sync");
                    return;
                }

                if (m_dialogService.EnableSync())
                {
                    await SaveToGoogleDriveAsync(project, statusCallback, m_localFileHandler);
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

                var audio = m_resourceManager.LoadAudioFile(projectFilePath.Path);

                track.AddAudioFile(projectFilePath.Path, audio);
                track.WaveformProvider = ImportWaveform(track);

                return track;
            }
            else
            {
                throw new NotSupportedException("Handler is not a FileHandler");
            }
        }

        private async Task SaveLocalAsync(Project project, string filePath, ILocalFileHandler fileHandler)
        {
            await m_resourceManager.SaveProject(project, new FileLocation(filePath), fileHandler);
        }

        private async Task SaveToGoogleDriveAsync(Project project, Action<string>? statusCallback, ILocalFileHandler fileHandler)
        {
            statusCallback?.Invoke("Start Save Project to Google Drive");

            await m_resourceManager.SaveProject(project, default, m_googleDriveHandler);

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