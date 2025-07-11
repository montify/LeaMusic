namespace LeaMusic.src.Services.ResourceServices_.GoogleDrive_
{
    using File = Google.Apis.Drive.v3.Data.File;

    public interface IGoogleContext
    {
        void CreateDriveService();

        File? CreateOrGetFolder(string name);

        File CreateOrGetSubfolder(File parentFolder, string subfolderName);

        List<string> GetAllProjectsName(string folderId);

        string? GetFolderIdByName(string folderName);

        string? GetFolderIdByPath(string path);

        (string? Id, string? Name, DateTime? CreatedTime)? GetFileMetadataByNameInFolder(string fileName, string folderPath);

        string? GetFileIdByNameInFolder(string fileName, string folderPath);

        string? GetFileIdFromFolder(string fileName, string folderPath);

        Task UpdateFileByNameInFolder(string filePath, string fileId, string folderId);

        Task UploadZipToFolderAsync(string filePath, string folderId);

        void DeleteFileById(string fileId);

        Task<Stream> GetZipAsStream(string id);
    }
}
