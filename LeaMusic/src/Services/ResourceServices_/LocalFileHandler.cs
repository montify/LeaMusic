namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;
    using NAudio.Wave;

    public class LocalFileHandler : ILocalFileHandler
    {
        private readonly IProjectSerializer m_projectSerializer;
        private readonly IWaveformService m_waveformService;
        private readonly ILocalFileMetaDataService m_localmedatDataService;
        private readonly IFileSystemService m_localFileSystemService;
        private readonly IBinaryWriter m_waveformBinaryWriter;

        public LocalFileHandler(
            IProjectSerializer serializer,
            IWaveformService waveformService,
            ILocalFileMetaDataService localmedatDataService,
            IFileSystemService localFileSystemService,
            IBinaryWriter waveformBinaryWriter)
        {
            m_projectSerializer = serializer;
            m_waveformService = waveformService;
            m_localmedatDataService = localmedatDataService;
            m_localFileSystemService = localFileSystemService;
            m_waveformBinaryWriter = waveformBinaryWriter;
        }

        public async Task<ProjectMetadata?> GetProjectMetadata(string projectName, Location location)
        {
            return await m_localmedatDataService.GetMetaData(location);
        }

        public Track ImportTrack(Location trackLocation)
        {
            if (trackLocation is FileLocation projectFilePath)
            {
                var track = new Track();
                track.OriginFilePath = projectFilePath.Path;
                track.Volume = 1;
                var audio = LoadAudioFromFile(projectFilePath.Path);
                track.AddAudioFile(projectFilePath.Path, audio);

                track.WaveformProvider = m_waveformService.GenerateFromAudio(track);
                return track;
            }
            else
            {
                throw new NotSupportedException("Handler is not a FileHandler");
            }
        }

        public Track LoadAudio(Track track, string projectPath)
        {
            var audioFilePath = m_localFileSystemService.CombinePaths(projectPath, track.AudioRelativePath);

            var audio = LoadAudioFromFile(audioFilePath);
            track.AddAudioFile(audioFilePath, audio);

            track.WaveformProvider = m_waveformService.GenerateFromAudio(track);
            return track;
        }

        public WaveStream LoadAudioFromFile(string path)
        {
            return new Mp3FileReader(path);
        }

        public async Task<Project?> LoadProject(Location projectLocation)
        {
            if (projectLocation is FileLocation location)
            {
                if (string.IsNullOrEmpty(location.Path))
                {
                    throw new ArgumentNullException("Path cant be null");
                }

                var file = await m_localFileSystemService.ReadAllTextAsync(location.Path);

                var project = m_projectSerializer.Deserialize(file);

                if (project == null)
                {
                    throw new NullReferenceException($"Cant load Project Path: {location.Path}");
                }

                var projectPath = m_localFileSystemService.GetDirectoryName(location.Path);

                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentNullException($"Cant find Project path {projectPath}");
                }

                await Task.Run(() =>
                {
                    for (int i = 0; i < project.Tracks.Count; i++)
                    {
                        var track = project.Tracks[i];

                        var trackPath = m_localFileSystemService.CombinePaths(projectPath, track.AudioRelativePath);

                        track = LoadAudio(track, projectPath);
                    }
                });

                // We assume, for this Programm, that every Audio is the same ( Stems from the Same Audio file) so its safe to use the First track´s Waveformat
                project.WaveFormat = project.Tracks[0].Waveformat;

                return project;
            }
            else
            {
                throw new NotSupportedException("Handler is not a FileHandler");
            }
        }

        public Task SaveProject(Location projectLocation, Project project)
        {
            if (projectLocation is FileLocation projectDirectoryPath)
            {
                DirectoryInfo projectDirectory = null;

                var pathName = m_localFileSystemService.GetFileName(projectDirectoryPath.Path);

                // Detect if the user select the project folder or the parentFolder
                if (pathName == project.Name)
                {
                    projectDirectory = m_localFileSystemService.CreateDirectory(projectDirectoryPath.Path);
                }
                else
                {
                    var path = m_localFileSystemService.CombinePaths(projectDirectoryPath.Path, project.Name);
                    projectDirectory = m_localFileSystemService.CreateDirectory(path);
                }

                var audioFilesDirectory = m_localFileSystemService.CreateDirectory($"{projectDirectory.FullName}/AudioFiles");
                var waveformDirectory = m_localFileSystemService.CreateDirectory($"{projectDirectory.FullName}/Waveforms");

                foreach (var track in project.Tracks)
                {
                    track.AudioRelativePath = $"{audioFilesDirectory.Name}/{track.AudioFileName}";
                    track.WaveformRelativePath = $"{waveformDirectory.Name}/{track.AudioFileName}.waveformat";

                    // only trigger for first time Save
                    if (!m_localFileSystemService.FileExists(audioFilesDirectory.FullName + "/" + track.AudioFileName))
                    {
                        m_localFileSystemService.CopyFile(track.OriginFilePath, audioFilesDirectory.FullName + "/" + track.AudioFileName, overwrite: true);
                    }

                    var waveform = track.WaveformProvider.WaveformBuffer;

                    var waveFormFilePath = waveformDirectory.FullName + $"\\{track.AudioFileName}.waveformat";

                    if (!m_localFileSystemService.FileExists(waveFormFilePath))
                    {
                        WriteWaveformBinary(waveform, waveFormFilePath);
                    }
                }

                var metaData = m_projectSerializer.Serialize(project);

                m_localFileSystemService.WriteAllTextAsync(Path.Combine(projectDirectory.ToString(), project.Name + ".prj"), metaData);
            }

            return Task.CompletedTask;
        }

        private void WriteWaveformBinary(float[] audioSamples, string path)
        {
            m_waveformBinaryWriter.WriteWaveformBinary(audioSamples, path);
        }
    }
}
