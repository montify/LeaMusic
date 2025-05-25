using LeaMusic.src.AudioEngine_;
using System;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;

namespace LeaMusic.src.ResourceManager_.GoogleDrive_
{
    public class GoogleDriveHandler : IResourceHandler
    {
        //We Import Database from file, not from Database, we only load Audio from Database
        private readonly FileHandler fileHandler;

       private GoogleContext context;
        private string rootFolder;

        public GoogleDriveHandler(string rootFolder, FileHandler fileHandler)
        {
            this.fileHandler = fileHandler;
            this.rootFolder = rootFolder;

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
                var rootFolder = context.CreateOrGetFolder(gLocation.gDriverootFolderPath).Id;

                var zipFilePath = "C:/LeaProjects";
                var zipExtractPath = Path.Combine(zipFilePath, gLocation.ProjectName);


                if (string.IsNullOrEmpty(rootFolder))
                    throw new Exception($"Cant find root Location with name {gLocation.gDriverootFolderPath}");

                //1) check if file exists in gdrive, if yes delete it
                string? file = context.GetFileIdByNameInFolder(gLocation.ProjectName + ".zip", gLocation.gDriverootFolderPath);
                
                if (string.IsNullOrEmpty(file))
                    return null;

               // var localfilePath = Path.Combine(gLocation.LocalProjectFilePath, gLocation.ProjectName);

                
                using (var stream = await context.GetZipAsStream(file))
                {
                    var projectDirectoryPath = Path.GetDirectoryName(gLocation.LocalProjectFilePath);

                    Directory.Delete(projectDirectoryPath, true);
                    Directory.CreateDirectory(projectDirectoryPath);

                    var extractTo = Path.GetDirectoryName(projectDirectoryPath);

                    ZipFile.ExtractToDirectory(stream, Path.GetDirectoryName(projectDirectoryPath), overwriteFiles: true);

                }

                var project = await fileHandler.LoadProject(new FileLocation(gLocation.LocalProjectFilePath), resourceManager);

                if (project == null)
                    throw new Exception("Cant load Project");

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
                Directory.CreateDirectory("C:/LeaProjects");

            if (!Directory.Exists("C:/LeaProjects/tmpZipFiles"))
                Directory.CreateDirectory("C:/LeaProjects/tmpZipFiles");


            if (Directory.Exists(localExtractedTmpPath))
                Directory.Delete(localExtractedTmpPath, recursive: true);

            await fileHandler.SaveProject(new FileLocation(localExtractedTmpPath), project);

            var ZipFilePath = $"C:/LeaProjects/tmpZipFiles/{project.Name}.zip";

            if (File.Exists(ZipFilePath))
                File.Delete(ZipFilePath);

            ZipFile.CreateFromDirectory(localExtractedTmpPath, ZipFilePath, CompressionLevel.Optimal, true);

            if (!File.Exists(ZipFilePath))
                throw new Exception($"cant create Zip file for Project: {project.Name}");

           //var gDriveLocation = gDriveProjectLocation as GDriveLocation;
            
            var rootFolderId = context.GetFolderIdByName(rootFolder);

            //check if .zip file on Gdrive exists, delete it
            var fileId = context.GetFileIdFromFolder($"{project.Name}.zip", rootFolder);

            if (!string.IsNullOrEmpty(fileId))
            {
                //Delete file
                context.DeleteFileById(fileId);
            }


            if (string.IsNullOrEmpty(rootFolderId))
                throw new Exception("cant find rootFolder");

            await context.UploadZipToFolderAsync(ZipFilePath, rootFolderId);

            if (File.Exists(ZipFilePath))
                File.Delete(ZipFilePath);

            if (Directory.Exists(localExtractedTmpPath))
                Directory.Delete(localExtractedTmpPath, true);
        }

        public (string? Id, string? Name, DateTime? LastSavedAt)? GetMetaData(string projectName)
        {
            var rootFolderId = context.GetFolderIdByName(rootFolder);

            if (string.IsNullOrEmpty(rootFolderId))
                throw new Exception("Cant find rootFolder");


            return context.GetFileMetadataByNameInFolder(projectName, rootFolder);
        }

        public Task<ProjectMetadata>? GetProjectMetadata(string projectName, Location location, LeaResourceManager resourceManager)
        {
            var rootFolderId = context.GetFolderIdByName(rootFolder);

            if (string.IsNullOrEmpty(rootFolderId))
                throw new Exception("Cant find rootFolder");

            
            var rawMetaData = context.GetFileMetadataByNameInFolder(projectName+".zip", rootFolder);

            if (rawMetaData == null)
                return null;

            var metaData = new ProjectMetadata(rawMetaData.Value.Name, rawMetaData.Value.CreatedTime.Value);

            return Task.FromResult(metaData);
        }
    }
}