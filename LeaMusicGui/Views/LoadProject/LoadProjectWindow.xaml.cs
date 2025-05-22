using System.Windows;

namespace LeaMusicGui.Views
{
    public partial class LoadProjectWindow : Window
    {
        public LoadProjectWindow(LoadProjectViewModel vm)
        {
            InitializeComponent();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CodeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
