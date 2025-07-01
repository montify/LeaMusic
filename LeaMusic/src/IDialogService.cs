namespace LeaMusic.src.AudioEngine_
{
    public interface IDialogService
    {
        string? Save();
        string? OpenFile(string filter);
        public bool EnableSync();
        public bool AskDownloadGoogleDrive(DateTime localDate, DateTime googleDriveDate);
    }
}
