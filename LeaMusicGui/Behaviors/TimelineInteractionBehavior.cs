namespace LeaMusicGui.Behaviors
{
    using System.Windows;
    using System.Windows.Input;
    using LeaMusicGui.Behaviors.BehaviorDTOs;
    using Microsoft.Xaml.Behaviors;
    using Point = System.Windows.Point;

    public class TimelineInteractionBehavior : Behavior<FrameworkElement>
    {
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

                ZoomWaveformMouseCommand?.Execute(
                    new MousePositionData(mousePosition, control.ActualWidth)
                );
            }

            if (IsBeatMarkerMoving)
            {
                MoveBeatMarkerCommand?.Execute(loopData);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

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

            if (IsBeatMarkerMoving)
            {
                IsBeatMarkerMoving = false;
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }

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

        public bool IsBeatMarkerMoving
        {
            get => (bool)GetValue(IsBeatMarkerMovingProperty);
            set => SetValue(IsBeatMarkerMovingProperty, value);
        }

        public static readonly DependencyProperty ResetZoomParameterCommandProperty =
            DependencyProperty.Register(
                nameof(ResetZoomParameterCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior)
            );

        public static readonly DependencyProperty ZoomWaveformMouseCommandProperty =
            DependencyProperty.Register(
                nameof(ZoomWaveformMouseCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior)
            );

        public static readonly DependencyProperty MoveBeatMarkerCommandProperty =
            DependencyProperty.Register(
                nameof(MoveBeatMarkerCommand),
                typeof(ICommand),
                typeof(TimelineInteractionBehavior)
            );

        public static readonly DependencyProperty IsBeatMarkerMovingProperty =
            DependencyProperty.Register(
                nameof(IsBeatMarkerMoving),
                typeof(bool),
                typeof(TimelineInteractionBehavior)
            );
    }
}
