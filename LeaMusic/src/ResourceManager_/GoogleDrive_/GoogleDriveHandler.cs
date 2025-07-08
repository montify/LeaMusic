namespace LeaMusic.src.ResourceManager_.GoogleDrive_
{
    using System.IO.Compression;
    using LeaMusic.src.AudioEngine_;

    public class GoogleDriveHandler : IResourceHandler
    {
        // We Import Database from file, not from Database, we only load Audio from Database
        private readonly FileHandler m_fileHandler;
        private GoogleContext m_context;
        private string m_rootFolder;

        public GoogleDriveHandler(string rootFolder, FileHandler fileHandler)
        {
            m_fileHandler = fileHandler;
            m_rootFolder = rootFolder;

            m_context = new GoogleContext();

            if (m_context == null)
            {
                throw new NullReferenceException("Cant create Google Drive Context");
            }

            m_context.CreateDriveService();
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
                var rootFolder = m_context.CreateOrGetFolder(gLocation.GDriverootFolderPath).Id;
                var zipFilePath = "C:/LeaProjects";
                var zipExtractPath = Path.Combine(zipFilePath, gLocation.ProjectName);

                if (string.IsNullOrEmpty(rootFolder))
                {
                    throw new Exception($"Cant find root Location with name {gLocation.GDriverootFolderPath}");
                }

                // 1) check if file exists in gdrive, if yes delete it
                string? file = m_context.GetFileIdByNameInFolder(gLocation.ProjectName + ".zip", gLocation.GDriverootFolderPath);

                if (string.IsNullOrEmpty(file))
                {
                    return null;
                }

                using (var stream = await m_context.GetZipAsStream(file))
                {
                    var projectDirectoryPath = Path.GetDirectoryName(gLocation.LocalProjectFilePath);

                    Directory.Delete(projectDirectoryPath, true);
                    Directory.CreateDirectory(projectDirectoryPath);

                    var extractTo = Path.GetDirectoryName(projectDirectoryPath);

                    ZipFile.ExtractToDirectory(stream, Path.GetDirectoryName(projectDirectoryPath), overwriteFiles: true);
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

            if (!Directory.Exists("C:/LeaProjects"))
            {
                Directory.CreateDirectory("C:/LeaProjects");
            }

            if (!Directory.Exists("C:/LeaProjects/tmpZipFiles"))
            {
                Directory.CreateDirectory("C:/LeaProjects/tmpZipFiles");
            }

            if (Directory.Exists(localExtractedTmpPath))
            {
                Directory.Delete(localExtractedTmpPath, recursive: true);
            }

            await m_fileHandler.SaveProject(new FileLocation(localExtractedTmpPath), project);

            var zipFilePath = $"C:/LeaProjects/tmpZipFiles/{project.Name}.zip";

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            ZipFile.CreateFromDirectory(localExtractedTmpPath, zipFilePath, CompressionLevel.Optimal, true);

            if (!File.Exists(zipFilePath))
            {
                throw new Exception($"cant create Zip file for Project: {project.Name}");
            }

            var rootFolderId = m_context.GetFolderIdByName(m_rootFolder);

            // check if .zip file on Gdrive exists, delete it
            var fileId = m_context.GetFileIdFromFolder($"{project.Name}.zip", m_rootFolder);

            if (!string.IsNullOrEmpty(fileId))
            {
                // Delete file
                m_context.DeleteFileById(fileId);
            }

            if (string.IsNullOrEmpty(rootFolderId))
            {
                throw new Exception("cant find rootFolder");
            }

            await m_context.UploadZipToFolderAsync(zipFilePath, rootFolderId);

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            if (Directory.Exists(localExtractedTmpPath))
            {
                Directory.Delete(localExtractedTmpPath, true);
            }
        }

        public (string? Id, string? Name, DateTime? LastSavedAt)? GetMetaData(string projectName)
        {
            var rootFolderId = m_context.GetFolderIdByName(m_rootFolder);

            if (string.IsNullOrEmpty(rootFolderId))
            {
                throw new Exception("Cant find rootFolder");
            }

            return m_context.GetFileMetadataByNameInFolder(projectName, m_rootFolder);
        }

        public async Task<ProjectMetadata?> GetProjectMetadata(string projectName, Location location, LeaResourceManager resourceManager)
        {
            var rootFolderId = m_context.GetFolderIdByName(m_rootFolder);

            if (string.IsNullOrEmpty(rootFolderId))
            {
                throw new Exception("Cant find rootFolder");
            }

            var rawMetaData = m_context.GetFileMetadataByNameInFolder(projectName + ".zip", m_rootFolder);

            if (rawMetaData == null)
            {
                return await Task.FromResult<ProjectMetadata?>(null);
            }

            var metaData = new ProjectMetadata(rawMetaData.Value.Name, rawMetaData.Value.CreatedTime.Value);

            return await Task.FromResult(metaData);
        }
    }
}