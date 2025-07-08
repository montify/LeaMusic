namespace LeaMusic.src.AudioEngine_
{
    public interface IDialogService
    {
        public string? Save();

        public string? OpenFile(string filter);

        public bool EnableSync();

        public bool AskDownloadGoogleDrive(DateTime localDate, DateTime googleDriveDate);
    }
}
