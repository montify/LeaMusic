using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System.IO.Compression;
using System.Diagnostics;

namespace LeaMusic.src.ResourceManager_
{
    public class GoogleDriveHandler : IResourceHandler
    {
        //We Import Database from file, not from Database, we only load Audio from Database
        private readonly FileHandler fileHandler;


        string[] scopes = { DriveService.Scope.DriveFile };
        string applicationName = "Drive API .NET Upload";

        UserCredential credential;
        DriveService driveService;


        public GoogleDriveHandler(string address, string username, string password, FileHandler fileHandler)
        {
            this.fileHandler = fileHandler;

        }

        public void CreateDriveService()
        {
            if (driveService != null)
                return;

            //TODO: Store credentials not in GIT
            using (var stream = new FileStream("C:/t/credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "credentials.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

        }

        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager)
        {
            if (trackLocation is not FileLocation fileLocation)
                throw new Exception("Expect a FileLocation, because Track imported From file");
            else
            {
                var track = fileHandler.ImportTrack(fileLocation, leaResourceManager);

                if (track == null)
                    throw new FileNotFoundException($"Cant load audio file from: {fileLocation.Path}");

                track.OriginFilePath = fileLocation.Path;

                return track;
            }

        }

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager)
        {
            throw new NotImplementedException();
        }

        public async Task<Project> LoadProject(Location projectLocation, LeaResourceManager resourceManager)
        {
            CreateDriveService();


            //var fileID = "1PItz-pjN_jObg49HQEnbZYVVIFQoDioS";
            var mainfolder = GetFolderIdByName(driveService, "LEA_PROJECTS");

            var list = ListAllFolders(driveService, mainfolder);


            //var fileID = GetFileIdByNameInFolder(driveService, { list[0].Name} , "LEA_PROJECTS");
            var fileID = "";

            //Download File to Stream
            var fileName = driveService.Files.Get(fileID).Execute().Name;
            fileName = Path.GetFileNameWithoutExtension(fileName);

            var request = driveService.Files.Get(fileID);
            var stream = new MemoryStream();
            request.Download(stream);

            if (Directory.Exists($"C:/t/downloads/{fileName}"))
                Directory.Delete($"C:/t/downloads/{fileName}",true);

            //Extract File to Disk
            ZipFile.ExtractToDirectory(stream, "C:/t/downloads");

            //Use Filehandler to load Project from Disk
            var project = await fileHandler.LoadProject(new FileLocation($"C:/t/downloads/{fileName}/Test.prj"), resourceManager);

            return project;
        }

        public async Task SaveProject(Location projectLocation, Project project)
        {
            CreateDriveService();

            var tmpPath = $"C:/t/tmp/{project.Name}";


            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
          
            //save project TMP
            fileHandler.SaveProject(new FileLocation(tmpPath), project);

            
            string sourceFolder = tmpPath; // The folder to zip
            string zipFilePath = @$"C:\t\tmpZip\{project.Name}.zip";

            //Delete old file
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);

            //ZIP Project
            ZipFile.CreateFromDirectory(sourceFolder, zipFilePath, CompressionLevel.Optimal, includeBaseDirectory: true);


            //Create or Get Google Drive folder
            var mainFolder = GetOrCreateFolder(driveService, "LEA_PROJECTS");
            var projectFolder = CreateOrGetSubfolder(driveService, mainFolder, project.Name);

            //Upload file to Google Drive
            await UploadFileToDriveAsync(zipFilePath, projectFolder);

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

        }
        public List<(string Name, string Id)> ListAllFolders(DriveService service, string parentFolderId)
        {
            var result = new List<(string Name, string Id)>();
            string pageToken = null;

            do
            {
                var request = service.Files.List();
                request.Q = $"mimeType = 'application/vnd.google-apps.folder' and '{parentFolderId}' in parents and trashed = false";
                request.Fields = "nextPageToken, files(id, name)";
                request.PageToken = pageToken;

                var response = request.Execute();

                foreach (var file in response.Files)
                {
                    result.Add((file.Name, file.Id));
                }

                pageToken = response.NextPageToken;
            } while (pageToken != null);

            return result;
        }


        public string GetOrCreateFolder(DriveService driveService, string folderName)
        {
            // Step 1: Search for existing folder with the given name
            var listRequest = driveService.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
            listRequest.Fields = "files(id, name)";
            var files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                // Folder exists - return its ID
                return files[0].Id;
            }
            else
            {

                // Folder doesn't exist - create it
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = folderName,
                    MimeType = "application/vnd.google-apps.folder",
                };

                var createRequest = driveService.Files.Create(fileMetadata);
                createRequest.Fields = "id";
                var folder = createRequest.Execute();

                return folder.Id;
            }
        }

        public string CreateOrGetSubfolder(DriveService driveService, string parentFolderId, string subfolderName)
        {
            // 1. Prüfen, ob Unterordner schon existiert
            var listRequest = driveService.Files.List();
            listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{subfolderName}' and '{parentFolderId}' in parents and trashed=false";
            listRequest.Fields = "files(id, name)";
            var files = listRequest.Execute().Files;

            if (files != null && files.Count > 0)
            {
                // Unterordner existiert, ID zurückgeben
                return files[0].Id;
            }
            else
            {
                // Ordner existiert nicht, neu erstellen
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = subfolderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Parents = new List<string> { parentFolderId }
                };

                var createRequest = driveService.Files.Create(fileMetadata);
                createRequest.Fields = "id";
                var folder = createRequest.Execute();

                return folder.Id;
            }
        }

        public async Task UploadFileToDriveAsync(string zipFilePath, string folderId)
        {

            CreateDriveService();

            // Prepare file metadata
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(zipFilePath),
                MimeType = "application/zip",
                Parents = new List<string> { folderId } 
            };

           
            // Step 1: Check if file already exists in the folder
            var listRequest = driveService.Files.List();
            listRequest.Q = $"name = '{fileMetadata.Name}' and '{folderId}' in parents and trashed = false";
            listRequest.Fields = "files(id, name)";


            var existingFiles = await listRequest.ExecuteAsync();
         
            // Step 2: If exists, delete it
            if (existingFiles != null && existingFiles.Files.Count > 0)
            {
                foreach (var existingFile in existingFiles.Files)
                {
                    driveService.Files.Delete(existingFile.Id).Execute();
                }
            }



            //Upload
            FilesResource.CreateMediaUpload request;

            using (var stream = new FileStream(zipFilePath, FileMode.Open))
            {

                request = driveService.Files.Create(fileMetadata, stream, "application/zip");
                request.ProgressChanged += Request_ProgressChanged;
                request.Fields = "id";
                await request.UploadAsync();
            }

            //var file = request.ResponseBody;

            //Console.WriteLine("File ID: " + file.Id);
        }

        private void Request_ProgressChanged(Google.Apis.Upload.IUploadProgress progress)
        {
            Debug.WriteLine($"Status: {progress.Status}| {progress.BytesSent} bytes");
        }

        private string GetFolderIdByName(DriveService service, string folderName)
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"name = '{folderName}' and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
            listRequest.Fields = "files(id, name)";
            var folders = listRequest.Execute().Files;

            return folders.FirstOrDefault()?.Id;
        }

        private string GetFileIdByNameInFolder(DriveService service, string fileName, string folderName)
        {
            string folderId = GetFolderIdByName(service, folderName);
            if (string.IsNullOrEmpty(folderId)) return null;

            var fileRequest = service.Files.List();
            fileRequest.Q = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
            fileRequest.Fields = "files(id, name)";
            var files = fileRequest.Execute().Files;

            return files.FirstOrDefault()?.Id;
        }

    }
}