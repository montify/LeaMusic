using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;


namespace LeaMusic.src.ResourceManager_.GoogleDrive_
{
    public class GoogleDriveHandler : IResourceHandler
    {
        //We Import Database from file, not from Database, we only load Audio from Database
        private readonly FileHandler fileHandler;


        string[] scopes = { DriveService.Scope.DriveFile };
        string applicationName = "Drive API .NET Upload";

       private UserCredential credential;
       private DriveService driveService;
       private GoogleContext context;

        public GoogleDriveHandler(string address, string username, string password, FileHandler fileHandler)
        {
            this.fileHandler = fileHandler;
            context = new GoogleContext();

            context.CreateDriveService();
        }

        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager)
        {
            throw new NotImplementedException();
        }

        public Track LoadAudio(Track track, string projectPath, LeaResourceManager resourceManager)
        {
            throw new NotImplementedException();
        }

        public async Task<Project?> LoadProject(Location location, LeaResourceManager resourceManager)
        {
            //appRootFolder is the Folder, that LeaMusic sees as root. It must be in Gdrives root 
            if (location is GDriveLocation gLocation)
            {
                var root = context.GetFolderIdByPath(gLocation.LeaRootPath);

                if (string.IsNullOrEmpty(root))
                    throw new Exception($"Cant find root Location with name {gLocation.LeaRootPath}");


                //var s = context.GetFileIdByNameInFolder("aaa.txt", "Test/lol");
               // var id = context.GetFolderIdByPath("Test/lol");
                //await context.UpdateFileByNameInFolder("C:t/aaa.txt", s, id);
                //await context.UploadZipToFolderAsync("C:/t/aaa.rar", id);

                //1) check if file exists in gdrive, if yes delete it
                string? file = context.GetFileIdByNameInFolder(gLocation.ProjectName, gLocation.LeaRootPath);

                if (string.IsNullOrEmpty(file))
                    return null;

                await context.DownloadZipToFolderAsync(gLocation.LocalPath, file);


                //2) Download to tmp
                //3) check if file exists tmp/extracedProjects(Hdd), if yes delete it
                //3) extrack it to tmp/extracedProjects
                //4) FileHandler.LoadProject(tmp/extracedProjects/Name.prj


                Console.WriteLine();

                return default;
            }
            else
                { throw new Exception("Cant cast to GDriveLocation"); }
        }

        public Task SaveProject(Location projectLocation, Project project)
        {
            //1) Save to tmpProject
            //2) Zip File
            //3) check if file exists(drive), if yes delete it || or just update ?
            //4) upload new Zip File


            throw new NotImplementedException();
        }
    }
}