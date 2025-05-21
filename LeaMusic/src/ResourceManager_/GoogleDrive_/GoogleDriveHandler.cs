using System.IO.Compression;

namespace LeaMusic.src.ResourceManager_.GoogleDrive_
{
    public class GoogleDriveHandler : IResourceHandler
    {
        //We Import Database from file, not from Database, we only load Audio from Database
        private readonly FileHandler fileHandler;

       private GoogleContext context;

        public GoogleDriveHandler(string address, string username, string password, FileHandler fileHandler)
        {
            this.fileHandler = fileHandler;
            context = new GoogleContext();

            context.CreateDriveService();
        }

        public Track ImportTrack(Location trackLocation, LeaResourceManager leaResourceManager)
        {
            var track = fileHandler.ImportTrack(trackLocation, leaResourceManager);

            if (track == null)
                throw new Exception("Cant load Track");
      
            return track;
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
                var rootFolder = context.CreateOrGetFolder(gLocation.rootFolderPath).Id;
                
                if (string.IsNullOrEmpty(rootFolder))
                    throw new Exception($"Cant find root Location with name {gLocation.rootFolderPath}");

                //1) check if file exists in gdrive, if yes delete it
                string? file = context.GetFileIdByNameInFolder(gLocation.ProjectName, gLocation.rootFolderPath);
                
                if (string.IsNullOrEmpty(file))
                    return null;

                var localfilePath = Path.Combine(gLocation.LocalPath, gLocation.ProjectName);


                using (var stream = await context.DownloadZipToFolderAsync(gLocation.LocalPath, file))
                {
                    ZipFile.ExtractToDirectory(stream, gLocation.LocalPath, overwriteFiles: true);
                }

                var localProjectPath = Path.Combine(gLocation.LocalPath, Path.GetFileNameWithoutExtension(gLocation.ProjectName), Path.GetFileNameWithoutExtension(gLocation.ProjectName) + ".prj");
               

                var project = await fileHandler.LoadProject(new FileLocation(localProjectPath), resourceManager);

                if (project == null)
                    throw new Exception("Cant load Project");

                return project;

            }
            else
            {
                throw new Exception("Cant cast to GDriveLocation");
            }
        }

        public async Task SaveProject(Location projectLocation, Project project)
        {
            var localExtractedTmpPath = $"C:/t/tmp/{project.Name}";

            if (Directory.Exists(localExtractedTmpPath))
                Directory.Delete(localExtractedTmpPath, recursive:true);

             await fileHandler.SaveProject(new FileLocation(localExtractedTmpPath), project);

            var ZipFilePath = $"C:/t/tmpZipFiles/{project.Name}.zip";

            if (File.Exists(ZipFilePath))
                File.Delete(ZipFilePath);

            ZipFile.CreateFromDirectory(localExtractedTmpPath, ZipFilePath, CompressionLevel.Optimal, true);

            if (!File.Exists(ZipFilePath))
                throw new Exception($"cant create Zip file for Project: {project.Name}");

            var gDriveLocation = projectLocation as GDriveLocation;

           var rootFolderId = context.GetFolderIdByName(gDriveLocation.rootFolderPath);

            //check if .zip file on Gdrive exists, delete it
            var fileId = context.GetFileIdFromFolder($"{project.Name}.zip", gDriveLocation.rootFolderPath);

            if(!string.IsNullOrEmpty(fileId))
            {
                //Delete file
                context.DeleteFileById(fileId);
            }


            if (string.IsNullOrEmpty(rootFolderId))
                throw new Exception("cant find rootFolder");

            await context.UploadZipToFolderAsync(ZipFilePath, rootFolderId);
        
        }
    }
}