namespace LeaMusicGui.Behaviors
{
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Xaml.Behaviors;

    public class BeatMarkerBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseRightButtonDown += OnMouseRightButtonDown;
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var control = (UIElement)sender;
            var parent = VisualTreeHelper.GetParent(control) as FrameworkElement;

            var mousePos = Mouse.GetPosition(parent);

            IsBeatMarkerMoving = true;
        }

        public bool IsBeatMarkerMoving
        {
            get => (bool)GetValue(IsBeatMarkerMovingProperty);
            set => SetValue(IsBeatMarkerMovingProperty, value);
        }

        public static readonly DependencyProperty IsBeatMarkerMovingProperty =
            DependencyProperty.Register(
            nameof(IsBeatMarkerMoving),
            typeof(bool),
            typeof(BeatMarkerBehavior));
    }
}
