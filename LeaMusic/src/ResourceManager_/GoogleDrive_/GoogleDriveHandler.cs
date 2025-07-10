namespace LeaMusic.src.ResourceManager_.GoogleDrive_
{
    using LeaMusic.src.AudioEngine_;
    using LeaMusic.src.Services.ResourceServices_;

    public class GoogleDriveHandler : IGoogleDriveHandler
    {
        // We Import Database from file, not from Database, we only load Audio from Database
        private readonly ILocalFileHandler m_fileHandler;
        private readonly IFileSystemService m_fileSystemService;
        private readonly IZipService m_zipService;

        private GoogleContext m_driveContext;

        public GoogleDriveHandler(
            ILocalFileHandler fileHandler,
            IFileSystemService fileSystemService,
            IZipService zipService)
        {
            m_fileHandler = fileHandler;
            m_fileSystemService = fileSystemService;
            m_driveContext = new GoogleContext();
            m_zipService = zipService;

            if (m_driveContext == null)
            {
                throw new NullReferenceException("Cant create Google Drive Context");
            }

            m_driveContext.CreateDriveService();
        }

        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager)
        {
            var track = m_fileHandler.ImportTrack(trackLocation, leaResourceManager);

            if (track == null)
            {
                throw new Exception("Cant load Track");
            }

            return track;
        }

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager)
        {
            throw new NotImplementedException();
        }

        public async Task<Project?> LoadProject(Location location, LeaResourceManager resourceManager)
        {
            // appRootFolder is the Folder, that LeaMusic sees as root. It must be in Gdrives root
            if (location is GDriveLocation gLocation)
            {
                var rootFolder = m_driveContext.CreateOrGetFolder(gLocation.GDriverootFolderPath).Id;
                var zipFilePath = "C:/LeaProjects";
                var zipExtractPath = Path.Combine(zipFilePath, gLocation.ProjectName);

                if (string.IsNullOrEmpty(rootFolder))
                {
                    throw new Exception($"Cant find root Location with name {gLocation.GDriverootFolderPath}");
                }

                // 1) check if file exists in gdrive, if yes delete it
                string? file = m_driveContext.GetFileIdByNameInFolder(gLocation.ProjectName + ".zip", gLocation.GDriverootFolderPath);

                if (string.IsNullOrEmpty(file))
                {
                    return null;
                }

                using (var stream = await m_driveContext.GetZipAsStream(file))
                {
                    var projectDirectoryPath = m_fileSystemService.GetDirectoryName(gLocation.LocalProjectFilePath);

                    m_fileSystemService.DeleteDirectoryRecursive(projectDirectoryPath);
                    m_fileSystemService.CreateDirectory(projectDirectoryPath);

                    var extractTo = m_fileSystemService.GetDirectoryName(projectDirectoryPath);

                    m_zipService.ExtractToDirectory(stream, m_fileSystemService.GetDirectoryName(projectDirectoryPath));
                }

                var project = await m_fileHandler.LoadProject(new FileLocation(gLocation.LocalProjectFilePath), resourceManager);

                if (project == null)
                {
                    throw new Exception("Cant load Project");
                }

                return project;
            }
            else
            {
                throw new Exception("Cant cast to GDriveLocation");
            }
        }

        public async Task SaveProject(Location gDriveProjectLocation, Project project)
        {
            var localExtractedTmpPath = $"C:/LeaProjects/tmp/{project.Name}";

            if (!m_fileSystemService.DirectoryExists("C:/LeaProjects"))
            {
                m_fileSystemService.CreateDirectory("C:/LeaProjects");
            }

            if (!m_fileSystemService.DirectoryExists("C:/LeaProjects/tmpZipFiles"))
            {
                m_fileSystemService.CreateDirectory("C:/LeaProjects/tmpZipFiles");
            }

            if (m_fileSystemService.DirectoryExists(localExtractedTmpPath))
            {
                m_fileSystemService.DeleteDirectoryRecursive(localExtractedTmpPath);
            }

            await m_fileHandler.SaveProject(new FileLocation(localExtractedTmpPath), project);

            var zipFilePath = $"C:/LeaProjects/tmpZipFiles/{project.Name}.zip";

            if (m_fileSystemService.FileExists(zipFilePath))
            {
                m_fileSystemService.DeleteFile(zipFilePath);
            }

            m_zipService.CreateFromDirectory(localExtractedTmpPath, zipFilePath);

            if (!m_fileSystemService.FileExists(zipFilePath))
            {
                throw new Exception($"cant create Zip file for Project: {project.Name}");
            }

            var rootFolderId = m_driveContext.GetFolderIdByName(AppConstants.GoogleDriveRootFolderName);

            // check if .zip file on Gdrive exists, delete it
            var fileId = m_driveContext.GetFileIdFromFolder($"{project.Name}.zip", AppConstants.GoogleDriveRootFolderName);

            if (!string.IsNullOrEmpty(fileId))
            {
                // Delete file
                m_driveContext.DeleteFileById(fileId);
            }

            if (string.IsNullOrEmpty(rootFolderId))
            {
                throw new Exception("cant find rootFolder");
            }

            await m_driveContext.UploadZipToFolderAsync(zipFilePath, rootFolderId);

            if (m_fileSystemService.FileExists(zipFilePath))
            {
                m_fileSystemService.DeleteFile(zipFilePath);
            }

            if (m_fileSystemService.DirectoryExists(localExtractedTmpPath))
            {
                m_fileSystemService.DeleteDirectoryRecursive(localExtractedTmpPath);
            }
        }

        public (string? Id, string? Name, DateTime? LastSavedAt)? GetMetaData(string projectName)
        {
            var rootFolderId = m_driveContext.GetFolderIdByName(AppConstants.GoogleDriveRootFolderName);

            if (string.IsNullOrEmpty(rootFolderId))
            {
                throw new Exception("Cant find rootFolder");
            }

            return m_driveContext.GetFileMetadataByNameInFolder(projectName, AppConstants.GoogleDriveRootFolderName);
        }

        public async Task<ProjectMetadata?> GetProjectMetadata(string projectName, Location location, LeaResourceManager resourceManager)
        {
            try
            {
                var rootFolderId = m_driveContext.GetFolderIdByName(AppConstants.GoogleDriveRootFolderName);

                if (string.IsNullOrEmpty(rootFolderId))
                {
                    throw new Exception("Cant find rootFolder");
                }

                var rawMetaData = m_driveContext.GetFileMetadataByNameInFolder(projectName + ".zip", AppConstants.GoogleDriveRootFolderName);

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

        public List<string> ListAllProjects()
        {
            var rootFolderId = m_driveContext.GetFolderIdByName("LeaRoot");
            var projects = m_driveContext.GetAllProjectsName(rootFolderId);

            return projects;
        }
    }
}