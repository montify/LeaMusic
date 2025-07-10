namespace LeaMusic.src.ResourceManager_.GoogleDrive_
{
    using System.Diagnostics;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Drive.v3;
    using Google.Apis.Services;
    using Google.Apis.Util.Store;
    using File = Google.Apis.Drive.v3.Data.File;

    internal class GoogleContext
    {
        private readonly string[] m_scopes = { DriveService.Scope.Drive };

        private UserCredential? m_credential;
        private DriveService m_driveService = null!;

        public GoogleContext()
        {
        }

        public void CreateDriveService()
        {
            if (m_driveService != null)
            {
                return;
            }

            // TODO: Store credentials not in GIT
            using (var stream = new FileStream(AppConstants.GoogleAuthCredentialPath, FileMode.Open, FileAccess.Read))
            {
                string credPath = "credentials.json";
                m_credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    m_scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            m_driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = m_credential,
                ApplicationName = AppConstants.GoogleDriveRootFolderName,
            });

            if (m_driveService == null)
            {
                throw new NullReferenceException("Cant intialize Drive");
            }
        }

        public File? CreateOrGetFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name can't be null or empty");
            }

            // Check for an existing folder with the given name
            var listRequest = m_driveService.Files.List();

            listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{name}' and trashed = false";
            listRequest.Fields = "files(id, name)";

            var result = listRequest.Execute();

            if (result.Files != null && result.Files.Count > 0)
            {
                // Folder found — return the first match
                var existingFolder = result.Files.First();
                Console.WriteLine($"Existing folder found: {existingFolder.Name} (ID: {existingFolder.Id})");
                return existingFolder;
            }

            // Folder doesn't exist — create new
            var fileMetadata = new File()
            {
                Name = name,
                MimeType = "application/vnd.google-apps.folder",
            };

            var createRequest = m_driveService.Files.Create(fileMetadata);
            createRequest.Fields = "id, name";
            var folder = createRequest.Execute();

            Console.WriteLine("Created new folder with ID: " + folder.Id);
            return folder;
        }

        public File CreateOrGetSubfolder(File parentFolder, string subfolderName)
        {
            // 1. Prüfen, ob Unterordner schon existiert
            var listRequest = m_driveService.Files.List();

            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{subfolderName}' and '{parentFolder.Id}' in parents and trashed=false";
            listRequest.Fields = "files(id, name)";
            var files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                // Unterordner existiert, ID zurückgeben
                return files[0];
            }
            else
            {
                // Ordner existiert nicht, neu erstellen
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = subfolderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<string> { parentFolder.Id },
                };

                var createRequest = m_driveService.Files.Create(fileMetadata);
                createRequest.Fields = "id";

                var folder = createRequest.Execute();

                return folder;
            }
        }

        public List<string> GetAllProjectsName(string folderId)
        {
            var result = new List<string>();
            string? pageToken = null;

            do
            {
                var request = m_driveService.Files.List();

                request.Q = $"'{folderId}' in parents and trashed = false";
                request.Fields = "nextPageToken, files(id, name)";
                request.Spaces = "drive";
                request.PageToken = pageToken;

                var response = request.Execute();

                if (response.Files != null && response.Files.Count > 0)
                {
                    foreach (var file in response.Files)
                    {
                        result.Add(file.Name);
                    }
                }

                pageToken = response.NextPageToken;
            }
            while (pageToken != null);

            return result;
        }

        public string? GetFolderIdByName(string folderName)
        {
            var listRequest = m_driveService.Files.List();

            listRequest.Q = $"name = '{folderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Fields = "files(id, name)";

            var folders = listRequest.Execute().Files;

            return folders.FirstOrDefault()?.Id;
        }

        public string? GetFolderIdByPath(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Remove "root" if user passed it in
            if (segments.First() == "root")
            {
                segments.RemoveAt(0);
            }

            string? parentId = "root";

            foreach (var segment in segments)
            {
                var listRequest = m_driveService.Files.List();
                listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{segment}' and '{parentId}' in parents and trashed = false";
                listRequest.Fields = "files(id, name)";
                var result = listRequest.Execute();

                var folder = result.Files.FirstOrDefault();
                if (folder == null)
                {
                    return null;
                }

                parentId = folder.Id;
            }

            return parentId;
        }

        public (string? Id, string? Name, DateTime? CreatedTime)? GetFileMetadataByNameInFolder(string fileName, string folderPath)
        {
            string? folderId = GetFolderIdByPath(folderPath);

            if (string.IsNullOrEmpty(folderId))
            {
                return null;
            }

            var fileRequest = m_driveService.Files.List();
            fileRequest.Q = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
            fileRequest.Fields = "files(id, name, createdTime)";

            var files = fileRequest.Execute().Files;

            var file = files.FirstOrDefault();

            if (file == null || file.CreatedTimeDateTimeOffset == null)
            {
                return null;
            }

            var createTime = file.CreatedTimeDateTimeOffset.Value.DateTime;

            return (file.Id, file.Name, createTime);
        }

        public string? GetFileIdByNameInFolder(string fileName, string folderPath)
        {
            string? folderId = GetFolderIdByPath(folderPath);

            if (string.IsNullOrEmpty(folderId))
            {
                return null;
            }

            var fileRequest = m_driveService.Files.List();
            fileRequest.Q = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
            fileRequest.Fields = "files(id, name)";

            var files = fileRequest.Execute().Files;

            return files.FirstOrDefault()?.Id;
        }

        public async Task UpdateFileByNameInFolder(string filePath, string fileId, string folderId)
        {
            var fileName = Path.GetFileName(filePath);

            var fileMetadata = new File()
            {
                Name = fileName,
                MimeType = "application/zip",
                Parents = new List<string> { folderId },
            };

            using var stream = new FileStream(filePath, FileMode.Open);

            var updateRequest = m_driveService.Files.Update(new File(), fileId, stream, fileMetadata.MimeType);
            updateRequest.Fields = "id, name";

            await updateRequest.UploadAsync();
        }

        // If file Exist, do nothing
        public async Task UploadZipToFolderAsync(string filePath, string folderId)
        {
            var fileName = Path.GetFileName(filePath);

            var fileMetadata = new File()
            {
                Name = fileName,
                MimeType = "application/zip",
                Parents = new List<string> { folderId },
            };

            FilesResource.CreateMediaUpload request;

            // check if file exists
            var exist = GetFileIdFromFolder(fileName, "Test/lol");

            if (!string.IsNullOrEmpty(exist))
            {
                return;
            }

            using var stream = new FileStream(filePath, FileMode.Open);

            request = m_driveService.Files.Create(fileMetadata, stream, "application/zip");
            request.ProgressChanged += Request_ProgressChanged;
            request.Fields = "id";

            await request.UploadAsync();
        }

        public string? GetFileIdFromFolder(string fileName, string folderPath)
        {
            // First get the folder ID from the folder path (supports nested folders)
            string? folderId = GetFolderIdByPath(folderPath);
            if (string.IsNullOrEmpty(folderId))
            {
                Console.WriteLine($"Folder not found: {folderPath}");
                return null;
            }

            // List files inside the folder
            var fileRequest = m_driveService.Files.List();
            fileRequest.Q = $"name = '{fileName.Replace("'", "\\'")}' and '{folderId}' in parents and trashed = false";
            fileRequest.Fields = "files(id, name)";
            var files = fileRequest.Execute().Files;

            if (files.Count == 0)
            {
                Console.WriteLine($"File '{fileName}' not found in folder '{folderPath}'");
                return null;
            }

            return files.First().Id;
        }

        public void DeleteFileById(string fileId)
        {
            try
            {
                m_driveService.Files.Delete(fileId).Execute();
                Console.WriteLine($"File with ID '{fileId}' has been deleted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete file: {ex.Message}");
            }
        }

        internal async Task<Stream> GetZipAsStream(string id)
        {
            var request = m_driveService.Files.Get(id);
            var memoryStream = new MemoryStream();

            await request.DownloadAsync(memoryStream);

            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        private void Request_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
        {
            Debug.WriteLine($"Status: {progress.Status}| {progress.BytesSent} bytes");
        }
    }
}