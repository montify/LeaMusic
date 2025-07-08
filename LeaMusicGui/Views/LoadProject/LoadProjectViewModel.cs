namespace LeaMusicGui
{
    using System.Collections.ObjectModel;
    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class LoadProjectViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<string> googleDriveProjectListValue = new ObservableCollection<string>();

        [ObservableProperty]
        private string m_selectedProject = string.Empty;
        private MainViewModel m_parentViewModel;

        public LoadProjectViewModel(MainViewModel parentViewModel)
        {
            if (parentViewModel == null)
            {
                throw new NullReferenceException("Please set ParentViewModel");
            }

            m_parentViewModel = parentViewModel;
        }

        partial void OnSelectedProjectChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            m_parentViewModel.ProjectName = value;
        }
    }
}