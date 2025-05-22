using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;


namespace LeaMusicGui
{
    public partial class LoadProjectViewModel : ObservableObject
    {
        public MainViewModel parentViewModel;


        [ObservableProperty]
        public ObservableCollection<string> googleDriveProjectList = new ObservableCollection<string>();

        [ObservableProperty]
        private string selectedProject;

        public LoadProjectViewModel(MainViewModel parentViewModel)
        {
            if (parentViewModel == null)
                throw new NullReferenceException("Please set ParentViewModel");

            this.parentViewModel = parentViewModel;
        }

        partial void OnSelectedProjectChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            parentViewModel.ProjectName = value;
        }

    }
}
