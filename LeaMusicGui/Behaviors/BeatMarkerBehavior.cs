using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LeaMusicGui.Behaviors
{
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

            Helpers.DraggedElement = control;
            Helpers.DraggedElement.CaptureMouse();

            ViewModel.isMarkerMoving = true;
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
           typeof(BeatMarkerBehavior),
           new PropertyMetadata(null));

    }
}
