using System.Windows;

namespace LeaMusicGui.Views
{
    public partial class LoadProjectWindow : Window
    {
        public LoadProjectWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                if (DataContext is LoadProjectViewModel vm)
                {
                    vm.RequestClose = () =>
                    {
                        DialogResult = true; // mark as success
                        Close();
                    };
                }
            };
        }
    }
}
