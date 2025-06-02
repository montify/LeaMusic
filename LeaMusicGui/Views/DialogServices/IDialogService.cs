namespace LeaMusicGui.Views.DialogServices
{
    public interface IDialogService
    {
        string? Save();
        string? OpenFile(string filter);
        public bool EnableSync();
        public bool AskDownloadGoogleDrive(DateTime localDate, DateTime googleDriveDate);
    }
}
