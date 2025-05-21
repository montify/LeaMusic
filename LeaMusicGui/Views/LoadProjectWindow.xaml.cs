using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LeaMusicGui.Views
{
    /// <summary>
    /// Interaktionslogik für LoadProjectWindow.xaml
    /// </summary>
    public partial class LoadProjectWindow : Window
    {
        public LoadProjectWindow(LoadProjectViewModel vm)
        {
            InitializeComponent();
        

            vm.OnClose += CloseWindow;
        }

        public void CloseWindow()
        {
            this.Close();
        }

    }
}
