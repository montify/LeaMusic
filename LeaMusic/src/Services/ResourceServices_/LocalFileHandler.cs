namespace LeaMusic.src.Services.ResourceServices_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.ResourceManager_;

    public class LocalFileHandler : ILocalFileHandler
    {
        private readonly IProjectSerializer m_projectSerializer;
        private readonly IWaveformService m_waveformService;
        private readonly IMetadataService m_localmedatDataService;
        private readonly IFileSystemService m_localFileSystemService;

        public LocalFileHandler(IProjectSerializer serializer, IWaveformService waveformService, IMetadataService localmedatDataService, IFileSystemService localFileSystemService)
        {
            m_projectSerializer = serializer;
            m_waveformService = waveformService;
            m_localmedatDataService = localmedatDataService;
            m_localFileSystemService = localFileSystemService;
        }

        public Task<ProjectMetadata?> GetProjectMetadata(string projectName, Location location, LeaResourceManager resourceManager)
        {
            return Task.FromResult(m_localmedatDataService.GetMetaData(location));
        }

        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager)
        {
            if (trackLocation is FileLocation projectFilePath)
            {
                var track = new Track();
                track.OriginFilePath = projectFilePath.Path;
                track.LoadAudioFile(projectFilePath.Path, leaResourceManager);

                track.WaveformProvider = m_waveformService.GenerateFromAudio(track);
                return track;
            }
            else
            {
                throw new NotSupportedException("Handler is not a FileHandler");
            }
        }

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager)
        {
            var audioFilePath = m_localFileSystemService.CombinePaths(projectPath, track.AudioRelativePath);

            track.LoadAudioFile(audioFilePath, resourceManager);

            track.WaveformProvider = m_waveformService.GenerateFromAudio(track);
            return track;
        }

        public async Task<Project?> LoadProject(Location projectLocation, LeaResourceManager resourceManager)
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

                        track = LoadAudio(track, projectPath, resourceManager);
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

            throw new NotImplementedException();
        }

        //private DirectoryInfo OpenOrCreateDirectory(string path)
        //{
        //    var projectDirectory = new DirectoryInfo(path);

        //    if (!projectDirectory.Exists)
        //    {
        //        projectDirectory = Directory.CreateDirectory($"{path}");
        //    }

        //    return projectDirectory;
        //}

        private void WriteWaveformBinary(float[] audioSamples, string path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                foreach (float sample in audioSamples)
                {
                    writer.Write(sample);
                }
            }
        }

    }
}
