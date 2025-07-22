namespace LeaMusicGui.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;

    public class PlayheadPositionBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as FrameworkElement;
            Point mousePosition = e.GetPosition(control);

            PlayheadPositionCommand?.Execute(mousePosition);
        }

        public ICommand PlayheadPositionCommand
        {
            get => (ICommand)GetValue(PlayheadPositionCommandProperty);
            set => SetValue(PlayheadPositionCommandProperty, value);
        }

        public static readonly DependencyProperty PlayheadPositionCommandProperty = DependencyProperty.Register(
         nameof(PlayheadPositionCommand),
         typeof(ICommand),
         typeof(PlayheadPositionBehavior));

    }
}
