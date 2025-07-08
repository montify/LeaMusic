namespace LeaMusicGui
{
    using System.Windows;
    using System.Windows.Input;

    // Notes:
    /*
     * 1)
     * use double the whole line down to actual index the buffer (buffer[(int)start, (int)end]),
     * cast it at the very end, because int can only index whole numbers x, no x.xxx :D
     * 2)
     * Use "VisibleWindow(VisibleSecondStart, VisibleSecondEnd) for calculation(Relative to VisibleWindow) MousePosition and Progressbar
     * because when zoom in (more than 1x) it starts to break :D
     *
    */

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var viewModel = (MainViewModel)DataContext;

            if (e.Key == Key.T)
            {
                viewModel.SetTextMarker();
            }
        }

        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (e.NewSize.Width != vm.RenderWidth)
                {
                    vm.RenderWidth = (int)e.NewSize.Width;
                }
            }
        }
    }
}