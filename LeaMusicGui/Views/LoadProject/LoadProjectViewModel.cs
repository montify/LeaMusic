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
        public string projectName;


        [ObservableProperty]
        public double speed;

        [ObservableProperty]
        public ObservableCollection<string> googleDriveProjectList = new ObservableCollection<string>();

        [ObservableProperty]
        private string selectedProject;

        public Action OnClose { get; set; } 

        //public ICommand OnCloseCommand => new RelayCommand(() =>
        //{
        //    // Perform cleanup if needed
        //    OnClose?.Invoke();
        //});

        public LoadProjectViewModel(MainViewModel parentViewModel)
        {
            this.parentViewModel = parentViewModel;


            parentViewModel.PropertyChanged += ParentViewModel_PropertyChanged;

            foreach (var projectName in parentViewModel.GetProjectFromGoogleDrive())
            {
                GoogleDriveProjectList.Add(projectName);
            }
           
        }

        private void ParentViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(parentViewModel.Speed))
            {
                if (Speed != parentViewModel.Speed)
                {
                    Speed = parentViewModel.Speed;
                }
            }
        }

        [RelayCommand]
        private async Task Save()
        {
           
        }


        partial void OnSpeedChanged(double value)
        {
            parentViewModel.Speed = value;

        }

        partial void OnProjectNameChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            var result = MessageBox.Show($"Do you want to load Project: {value} ","", System.Windows.MessageBoxButton.YesNo);

            if (result == System.Windows.MessageBoxResult.No)
            {
                parentViewModel.ProjectName = null;
                return;
            }
              

            
            parentViewModel.ProjectName = value;
            OnClose?.Invoke();
           

            
        }


    }
}
