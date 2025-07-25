namespace LeaMusicGui
{
    using System.Collections.ObjectModel;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class LoadProjectViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> m_projectList = new ObservableCollection<string>();

        [ObservableProperty]
        private string m_selectedProject = string.Empty;

        public Action? RequestClose { get; set; }

        public LoadProjectViewModel()
        {
            //var gDriveHandler = new GoogleDriveHandler()

            //var projects = new ObservableCollection<string>(gDriveHandler.ListAllProjects());

            //foreach (var p in projects)
            //{
            //    ProjectList.Add(p);
            //}
        }

        partial void OnSelectedProjectChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
        }

        [RelayCommand]
        private void ConfirmSelection()
        {
            if (!string.IsNullOrEmpty(SelectedProject))
            {
                RequestClose?.Invoke(); // Close the window
            }
        }
    }
}
