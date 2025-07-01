using LeaMusicGui.Controls;
using LeaMusicGui.Views.DialogServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

        //DataContext = new MainViewModel();
        
        //if (DataContext is MainViewModel vm)
        //{
        //    vm.DialogService = new DialogService();
        //}
    }

    public record class SelectionRange
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    static UIElement? draggedElement = null;

    SelectionRange selectionRange = new SelectionRange();

    private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var childControl = sender as UIElement;
        if (childControl != null)
        {
            Point mousePosition = e.GetPosition(childControl);
            var viewModel = (MainViewModel)DataContext;

            selectionRange = new SelectionRange();
            selectionRange.Start = (float)mousePosition.X;

        }
    }

    private void MainCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var control = sender as FrameworkElement;
        var viewModel = (MainViewModel)DataContext;

        //ZOOM
        if (isZoom)
        {
            if (control != null)
            {
                Point mousePosition = e.GetPosition(control);
                viewModel.ZoomWaveformMouse(mousePosition, control.ActualWidth);
            }
        }

        //resize loop
        if (isLoopBeginDragLeftHandle)
            viewModel.LoopSelectionStart(Mouse.GetPosition(control).X, control.ActualWidth);

        if (isLoopBeginDragRightHandle)
            viewModel.LoopSelectionEnd(Mouse.GetPosition(control).X, control.ActualWidth);

        if (isMarkerMoving)
            viewModel.MoveMarker(Mouse.GetPosition(control), (int)control.ActualWidth);
  
    }

    private void MainCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var childControl = sender as FrameworkElement;
        if (childControl != null)
        {
            Point mousePosition = e.GetPosition(childControl);
            var viewModel = (MainViewModel)DataContext;

            selectionRange.End = (float)mousePosition.X;

            viewModel.LoopSelection(selectionRange.Start, selectionRange.End, childControl.ActualWidth);
        }

        
        //Loop 
        if (isLoopBeginDragRightHandle)
        {
            var viewModel = (MainViewModel)DataContext;
            viewModel.LoopSelectionEnd(Mouse.GetPosition(childControl).X, childControl.ActualWidth);
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;

        if (e.Key == Key.T)
            viewModel.SetTextMarker();
    }

    private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (e.NewSize.Width != vm.RenderWidth)
                vm.RenderWidth = (int)e.NewSize.Width;
        }
    }

    private bool IsOnLefEdgeLoop(object sender, double EdgeThreshold = 20.04f)
    {
        var control = (LoopControl)sender;
        var mousePos = Mouse.GetPosition(control);

        //Because Control is 1px width and we scale based on LoopPercentage it, we need the scaled Width Value with GetScaleX
        double scaleX = GetScaleX(control);

        var controlWidth = control.ActualWidth * scaleX;

        return mousePos.X * scaleX <= EdgeThreshold && mousePos.Y < 30;
    }

    private bool IsOnRightEdgeLoop(object sender, double EdgeThreshold = 20.04f)
    {
        var control = (LoopControl)sender;
        var mousePos = Mouse.GetPosition(control);

        //Because COntrol is 1px width and we scale based on LoopPercentage it, we need the scaled Width Value with GetScaleX
        double scaleX = GetScaleX(control);

        var controlWidth = control.ActualWidth * scaleX;
        
        return mousePos.X * scaleX > controlWidth- EdgeThreshold && mousePos.Y < 30;  
        
    }

    bool isZoom;
    private void MainCanvas_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var childControl = sender as FrameworkElement;
        if (childControl != null)
        {
            Point mousePosition = e.GetPosition(childControl);
            var viewModel = (MainViewModel)DataContext;

            if(mousePosition.Y > 30)
            {
                isZoom = true;   
            }
        }
    }

    bool isLoopBeginDragLeftHandle;
    bool isLoopBeginDragRightHandle;

    private void LoopControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Right)
            return;

        var control = (LoopControl)sender;

        if (IsOnLefEdgeLoop(control))
            isLoopBeginDragLeftHandle = true;
        
        if(IsOnRightEdgeLoop(control))
            isLoopBeginDragRightHandle = true;

        draggedElement = control;
        draggedElement.CaptureMouse();
    }

    private void LoopControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        Mouse.OverrideCursor = null;
    }

    private void LoopControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var control = (LoopControl)sender;
        var parent = VisualTreeHelper.GetParent(control) as FrameworkElement;

        if (IsOnLefEdgeLoop(control) || IsOnRightEdgeLoop(control))
            Mouse.OverrideCursor = System.Windows.Input.Cursors.SizeWE;
        else
            Mouse.OverrideCursor = null;
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Right)
            return;

        var viewModel = (MainViewModel)DataContext;

        var control = (Window)sender;
        var parent = VisualTreeHelper.GetParent(control) as FrameworkElement;
       
        if(isZoom)
        {
            viewModel.ResetZoomParameter();
            isZoom = false; 
        }

        if (draggedElement != null)
        {
            draggedElement.ReleaseMouseCapture();
            draggedElement = null;

            //ReleaseLoopHandle
            if (isLoopBeginDragLeftHandle)
                isLoopBeginDragLeftHandle = false;

            if (isLoopBeginDragRightHandle)
                isLoopBeginDragRightHandle = false;

            if (isMarkerMoving)
                isMarkerMoving = false;
        }
    }

    bool isMarkerMoving;
    private void Marker_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var control = (UIElement)sender;
        var parent = VisualTreeHelper.GetParent(control) as FrameworkElement;

        var mousePos = Mouse.GetPosition(parent);
        var viewModel = (MainViewModel)DataContext;

        draggedElement = control;
        draggedElement.CaptureMouse();

        isMarkerMoving = true;
        
    }

    private double GetScaleX(UIElement element)
    {
        if (element.RenderTransform is TransformGroup tg)
        {
            var scale = tg.Children.OfType<ScaleTransform>().FirstOrDefault();
            if (scale != null)
                return scale.ScaleX;
        }
        else if (element.RenderTransform is ScaleTransform s)
        {
            return s.ScaleX;
        }

        return 1.0;
    }
}