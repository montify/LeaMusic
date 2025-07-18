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
            var loopData = new MousePositionData(mousePosition, control.ActualWidth);

            if (m_isZoom)
            {
                if (control == null)
                {
                    return;
                }

                ZoomWaveformMouseCommand?.Execute(new MousePositionData(mousePosition, control.ActualWidth));
            }

            // resize loop
            if (IsLoopBeginDragLeftHandle)
            {
                LoopStartCommand?.Execute(loopData);
            }

            if (IsLoopBeginDragRightHandle)
            {
                LoopEndCommand?.Execute(loopData);
            }

            if (IsBeatMarkerMoving)
            {
                MoveBeatMarkerCommand?.Execute(loopData);
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

            if (IsLoopBeginDragRightHandle)
            {
                var loopData = new MousePositionData(mousePosition, control.ActualWidth);
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
                ResetZoomParameterCommand?.Execute(control);
                m_isZoom = false;
            }

            if (Helpers.DraggedElement != null)
            {
                Helpers.DraggedElement.ReleaseMouseCapture();
                Helpers.DraggedElement = null;

                if (IsLoopBeginDragLeftHandle)
                {
                    IsLoopBeginDragLeftHandle = false;
                }

                if (IsLoopBeginDragRightHandle)
                {
                    IsLoopBeginDragRightHandle = false;
                }

                if (IsBeatMarkerMoving)
                {
                    IsBeatMarkerMoving = false;
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

        public ICommand ResetZoomParameterCommand
        {
            get => (ICommand)GetValue(ResetZoomParameterCommandProperty);
            set => SetValue(ResetZoomParameterCommandProperty, value);
        }

        public ICommand ZoomWaveformMouseCommand
        {
            get => (ICommand)GetValue(ZoomWaveformMouseCommandProperty);
            set => SetValue(ZoomWaveformMouseCommandProperty, value);
        }

        public ICommand MoveBeatMarkerCommand
        {
            get => (ICommand)GetValue(MoveBeatMarkerCommandProperty);
            set => SetValue(MoveBeatMarkerCommandProperty, value);
        }

        public bool IsLoopBeginDragLeftHandle
        {
            get => (bool)GetValue(IsLoopBeginDragLeftHandleProperty);
            set => SetValue(IsLoopBeginDragLeftHandleProperty, value);
        }

        public bool IsLoopBeginDragRightHandle
        {
            get => (bool)GetValue(IsLoopBeginDragRightHandleProperty);
            set => SetValue(IsLoopBeginDragRightHandleProperty, value);
        }

        public bool IsBeatMarkerMoving
        {
            get => (bool)GetValue(IsBeatMarkerMovingProperty);
            set => SetValue(IsBeatMarkerMovingProperty, value);
        }

        public static readonly DependencyProperty ResetZoomParameterCommandProperty = DependencyProperty.Register(
           nameof(ResetZoomParameterCommand),
           typeof(ICommand),
           typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty LoopCommandProperty = DependencyProperty.Register(
            nameof(LoopCommand),
            typeof(ICommand),
            typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty LoopStartCommandProperty =
            DependencyProperty.Register(
                nameof(LoopStartCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty LoopEndCommandProperty =
            DependencyProperty.Register(
                nameof(LoopEndCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty ZoomWaveformMouseCommandProperty =
           DependencyProperty.Register(
               nameof(ZoomWaveformMouseCommand),
               typeof(ICommand),
               typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty MoveBeatMarkerCommandProperty =
          DependencyProperty.Register(
              nameof(MoveBeatMarkerCommand),
              typeof(ICommand),
              typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty IsLoopBeginDragLeftHandleProperty =
            DependencyProperty.Register(
          nameof(IsLoopBeginDragLeftHandle),
          typeof(bool),
          typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty IsLoopBeginDragRightHandleProperty =
            DependencyProperty.Register(
          nameof(IsLoopBeginDragRightHandle),
          typeof(bool),
          typeof(TimelineInteractionBehavior));

        public static readonly DependencyProperty IsBeatMarkerMovingProperty =
          DependencyProperty.Register(
        nameof(IsBeatMarkerMoving),
        typeof(bool),
        typeof(TimelineInteractionBehavior));
    }
}