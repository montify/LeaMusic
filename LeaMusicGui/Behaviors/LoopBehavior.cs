using LeaMusicGui.Controls;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace LeaMusicGui.Behaviors
{
    public class LoopBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {        
            AssociatedObject.MouseDown += OnMouseDown;
            AssociatedObject.MouseLeave += OnMouseLeave;
            AssociatedObject.MouseMove += OnMouseMove;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var control = (LoopControl)sender;
            var parent = VisualTreeHelper.GetParent(control) as FrameworkElement;

            if (IsOnLefEdgeLoop(control) || IsOnRightEdgeLoop(control))
                Mouse.OverrideCursor =Cursors.SizeWE;
            else
                Mouse.OverrideCursor = null;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right)
                return;

            var control = (LoopControl)sender;

            if (IsOnLefEdgeLoop(control))
                ViewModel.isLoopBeginDragLeftHandle = true;

            if (IsOnRightEdgeLoop(control))
                ViewModel.isLoopBeginDragRightHandle = true;

            Helpers.DraggedElement = control;
            Helpers.DraggedElement.CaptureMouse();
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

            return mousePos.X * scaleX > controlWidth - EdgeThreshold && mousePos.Y < 30;
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

        public MainViewModel ViewModel
        {
            get { return (MainViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(
           "ViewModel",
           typeof(MainViewModel),
           typeof(LoopBehavior),
           new PropertyMetadata(null));
    }
}
