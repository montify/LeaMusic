namespace LeaMusicGui.Behaviors
{
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;
    using Point = System.Windows.Point;

    public record class SelectionRange
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    public class TimelineInteractionBehavior : Behavior<FrameworkElement>
    {
        private SelectionRange m_selectionRange = new ();

        private bool m_isZoom;

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
                    m_isZoom = true;
                 }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var control = sender as FrameworkElement;

            if (m_isZoom)
            {
                if (control == null)
                {
                    return;
                }

                Point mousePosition = e.GetPosition(control);
                ViewModel.ZoomWaveformMouse(mousePosition, control.ActualWidth);
            }

            // resize loop
            if (ViewModel.IsLoopBeginDragLeftHandle)
            {
                ViewModel.LoopSelectionStart(Mouse.GetPosition(control).X, control!.ActualWidth);
            }

            if (ViewModel.IsLoopBeginDragRightHandle)
            {
                ViewModel.LoopSelectionEnd(Mouse.GetPosition(control).X, control!.ActualWidth);
            }

            if (ViewModel.IsMarkerMoving)
            {
                ViewModel.MoveMarker(Mouse.GetPosition(control), (int)control!.ActualWidth);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var childControl = sender as FrameworkElement;
            if (childControl != null)
            {
               Point mousePosition = e.GetPosition(childControl);

               m_selectionRange.End = (float)mousePosition.X;

               ViewModel.LoopSelection(m_selectionRange.Start, m_selectionRange.End, childControl.ActualWidth);
            }

            // Loop
            if (ViewModel.IsLoopBeginDragRightHandle)
            {
                ViewModel.LoopSelectionEnd(Mouse.GetPosition(childControl).X, childControl.ActualWidth);
            }
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
            {
                return;
            }

            var control = (Window)sender;

            if (m_isZoom)
            {
                ViewModel.ResetZoomParameter();
                m_isZoom = false;
            }

            if (Helpers.DraggedElement != null)
            {
                Helpers.DraggedElement.ReleaseMouseCapture();
                Helpers.DraggedElement = null;

                // ReleaseLoopHandle
                if (ViewModel.IsLoopBeginDragLeftHandle)
                {
                    ViewModel.IsLoopBeginDragLeftHandle = false;
                }

                if (ViewModel.IsLoopBeginDragRightHandle)
                {
                    ViewModel.IsLoopBeginDragRightHandle = false;
                }

                if (ViewModel.IsMarkerMoving)
                {
                    ViewModel.IsMarkerMoving = false;
                }
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var childControl = sender as UIElement;
            if (childControl != null)
            {
                Point mousePosition = e.GetPosition(childControl);

                m_selectionRange = new SelectionRange();
                m_selectionRange.Start = (float)mousePosition.X;
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
