using System.Windows;
using System.Windows.Forms;

namespace LeaMusicGui.Views.DialogServices
{
    public class DialogService : IDialogService
    {
        public string? OpenFile(string filter)
        {
            var dialog = new OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;

        }

        public string? Save()
        {
            var saveDialog = new FolderBrowserDialog();
            return saveDialog.ShowDialog() == DialogResult.OK ? saveDialog.SelectedPath : null;
        }

        public bool EnableSync()
        {
            var result = System.Windows.MessageBox.Show("Do you want to Sync with GoogleDrive?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                return true;
            else
                return false;
        }

        public bool AskDownloadGoogleDrive(DateTime localDate, DateTime googleDriveDate)
        {
            var result = System.Windows.MessageBox.Show($"The Project on GoogleDrive is newer, do you want do download it? \n Local: {localDate} \n GoogleDrive: {googleDriveDate}", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                return true;
            else
                return false;
        }
    }
}