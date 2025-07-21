namespace LeaMusicGui.Behaviors
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;

    public class SliderDragCompletedBehavior : Behavior<Slider>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SliderDragCompletedBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.Template.FindName("PART_Track", AssociatedObject) is Track track &&
                track.Thumb is Thumb thumb)
            {
                thumb.DragCompleted += (s, args) =>
                {
                        Command?.Execute(null);
                };
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
        }
    }
}
