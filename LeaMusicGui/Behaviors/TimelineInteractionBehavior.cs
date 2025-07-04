using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LeaMusicGui.Behaviors
{
    public record class SelectionRange
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    public class TimelineInteractionBehavior : Behavior<FrameworkElement>
    {
        SelectionRange selectionRange = new SelectionRange();

        bool isZoom;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseRightButtonDown += OnMouseRightButtonDown;
            Window.GetWindow(AssociatedObject).MouseUp += OnWindowMouseUp;
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var childControl = sender as FrameworkElement;
            if (childControl != null)
            {
                Point mousePosition = e.GetPosition(childControl);

                if (mousePosition.Y > 30)
                {
                    isZoom = true;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var control = sender as FrameworkElement;

            //ZOOM
            if (isZoom)
            {
                if (control != null)
                {
                    Point mousePosition = e.GetPosition(control);
                    ViewModel.ZoomWaveformMouse(mousePosition, control.ActualWidth);
                }
            }

            //resize loop
            if (ViewModel.isLoopBeginDragLeftHandle)
                ViewModel.LoopSelectionStart(Mouse.GetPosition(control).X, control.ActualWidth);

            if (ViewModel.isLoopBeginDragRightHandle)
                ViewModel.LoopSelectionEnd(Mouse.GetPosition(control).X, control.ActualWidth);

            if (ViewModel.isMarkerMoving)
                ViewModel.MoveMarker(Mouse.GetPosition(control), (int)control.ActualWidth);
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var childControl = sender as FrameworkElement;
            if (childControl != null)
            {
                Point mousePosition = e.GetPosition(childControl);

                selectionRange.End = (float)mousePosition.X;

                ViewModel.LoopSelection(selectionRange.Start, selectionRange.End, childControl.ActualWidth);
            }

            //Loop 
            if (ViewModel.isLoopBeginDragRightHandle)
                ViewModel.LoopSelectionEnd(Mouse.GetPosition(childControl).X, childControl.ActualWidth); 
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            var control = (Window)sender;
            var parent = VisualTreeHelper.GetParent(control) as FrameworkElement;

            if (isZoom)
            {
                ViewModel.ResetZoomParameter();
                isZoom = false;
            }

            if (Helpers.DraggedElement != null)
            {
                Helpers.DraggedElement.ReleaseMouseCapture();
                Helpers.DraggedElement = null;

                //ReleaseLoopHandle
                if (ViewModel.isLoopBeginDragLeftHandle)
                    ViewModel.isLoopBeginDragLeftHandle = false;

                if (ViewModel.isLoopBeginDragRightHandle)
                    ViewModel.isLoopBeginDragRightHandle = false;

                if (ViewModel.isMarkerMoving)
                    ViewModel.isMarkerMoving = false;
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var childControl = sender as UIElement;
            if (childControl != null)
            {
                Point mousePosition = e.GetPosition(childControl);

                selectionRange = new SelectionRange();
                selectionRange.Start = (float)mousePosition.X;
            }
        }

        public MainViewModel ViewModel
        {
            get { return (MainViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(
           "ViewModel",
           typeof(MainViewModel),
           typeof(TimelineInteractionBehavior),
           new PropertyMetadata(null));
    }
}
