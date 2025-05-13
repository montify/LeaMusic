using System.Windows;
using System.Windows.Input;

namespace LeaMusicGui;

//Notes:
/*
 * 1) 
 * use double the whole line down to actual index the buffer (buffer[(int)start, (int)end]), 
 * cast it at the very end, because int can only index whole numbers x, no x.xxx :D
 * 
 * 2)
 * Use "VisibleWindow(VisibleSecondStart, VisibleSecondEnd) for calculation(Relative to VisibleWindow) MousePosition and Progressbar
 * because when zoom in (more than 1x) it starts to break :D
 * 
*/

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

    }

    public record class SelectionRange
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    SelectionRange selectionRange;

    private void TrackControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var childControl = sender as UIElement;
        if (childControl != null)
        {
            Point mousePosition = e.GetPosition(childControl);
            var viewModel = (GuiViewModel)DataContext;

            selectionRange = new SelectionRange();
            selectionRange.Start = (float)mousePosition.X;
        }
    }

    private void TrackControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var childControl = sender as FrameworkElement;
        if (childControl != null)
        {
            Point mousePosition = e.GetPosition(childControl);
            var viewModel = (GuiViewModel)DataContext;

            selectionRange.End = (float)mousePosition.X;

            viewModel.LoopSelection(selectionRange.Start, selectionRange.End, childControl.ActualWidth);
        }
    }

    private void ItemsControl_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var viewModel = (GuiViewModel)DataContext;

        if (e.Key == Key.T)
            viewModel.SetTextMarker();
    }


    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {

        if (DataContext is GuiViewModel vm)
        {
            if (e.NewSize.Width != vm.RenderWidth)
                vm.RenderWidth = (int)e.NewSize.Width;
        }
    }

    private void TrackControl_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var childControl = sender as FrameworkElement;
        if (childControl != null)
        {
            Point mousePosition = e.GetPosition(childControl);
            var viewModel = (GuiViewModel)DataContext;
            
                
            viewModel.MouseClick(mousePosition, childControl.ActualWidth);

        }
    }
}

