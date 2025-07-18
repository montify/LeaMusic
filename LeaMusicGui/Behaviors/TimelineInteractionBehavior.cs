namespace LeaMusicGui.Behaviors
{
    using System.Windows;
    using System.Windows.Input;
    using LeaMusicGui.Behaviors.BehaviorDTOs;
    using Microsoft.Xaml.Behaviors;
    using Point = System.Windows.Point;

    public record class SelectionRange
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    public class TimelineInteractionBehavior : Behavior<FrameworkElement>
    {
        private SelectionRange m_selectionRange = new();

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

            Point mousePosition = e.GetPosition(control);
            var loopData = new LoopDataStartEnd(mousePosition, control.ActualWidth);

            if (m_isZoom)
            {
                if (control == null)
                {
                    return;
                }

                ViewModel.ZoomWaveformMouse(mousePosition, control.ActualWidth);
            }

            // resize loop
            if (ViewModel.IsLoopBeginDragLeftHandle)
            {
                LoopStartCommand?.Execute(loopData);
            }

            if (ViewModel.IsLoopBeginDragRightHandle)
            {
                LoopEndCommand?.Execute(loopData);
            }

            if (ViewModel.IsMarkerMoving)
            {
                ViewModel.MoveMarker(Mouse.GetPosition(control), (int)control!.ActualWidth);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var control = sender as FrameworkElement;
            Point mousePosition = e.GetPosition(control);

            if (control != null)
            {
                m_selectionRange.End = (float)mousePosition.X;

                var loopData = new LoopData(
                    m_selectionRange.Start,
                    m_selectionRange.End,
                    control.ActualWidth
                );
                LoopCommand?.Execute(loopData);
            }

            // Loop
            if (ViewModel.IsLoopBeginDragRightHandle)
            {
                var loopData = new LoopDataStartEnd(mousePosition, control.ActualWidth);
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

        public ICommand LoopCommand
        {
            get => (ICommand)GetValue(LoopCommandProperty);
            set => SetValue(LoopCommandProperty, value);
        }

        public ICommand LoopStartCommand
        {
            get => (ICommand)GetValue(LoopStartCommandProperty);
            set => SetValue(LoopStartCommandProperty, value);
        }

        public ICommand LoopEndCommand
        {
            get => (ICommand)GetValue(LoopEndCommandProperty);
            set => SetValue(LoopEndCommandProperty, value);
        }

        public static readonly DependencyProperty LoopCommandProperty = DependencyProperty.Register(
            nameof(LoopCommand),
            typeof(ICommand),
            typeof(TimelineInteractionBehavior)
        );

        public static readonly DependencyProperty LoopStartCommandProperty =
            DependencyProperty.Register(
                nameof(LoopStartCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior)
            );

        public static readonly DependencyProperty LoopEndCommandProperty =
            DependencyProperty.Register(
                nameof(LoopEndCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior)
            );

        // Delete
        public MainViewModel ViewModel
        {
            get { return (MainViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel",
            typeof(MainViewModel),
            typeof(TimelineInteractionBehavior),
            new PropertyMetadata(null)
        );
    }
}
