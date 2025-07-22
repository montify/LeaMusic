namespace LeaMusicGui.Behaviors
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using LeaMusicGui.Behaviors.BehaviorDTOs;
    using Microsoft.Xaml.Behaviors;

    public record class SelectionRange
    {
        public float Start { get; set; }
        public float End { get; set; }
    }

    public class LoopBehavior : Behavior<FrameworkElement>
    {
        private SelectionRange m_selectionRange = new();

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
            AssociatedObject.MouseMove += OnMouseMove;
            //AssociatedObject.MouseRightButtonDown += OnMouseRightButtonDown;
            Window.GetWindow(AssociatedObject).MouseUp += OnWindowMouseUp;
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
        {

            if (IsLoopBeginDragLeftHandle)
            {
                IsLoopBeginDragLeftHandle = false;
            }

            if (IsLoopBeginDragRightHandle)
            {
                IsLoopBeginDragRightHandle = false;
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as UIElement;
            if (control != null)
            {
                Point mousePosition = e.GetPosition(control);

                m_selectionRange = new SelectionRange();
                m_selectionRange.Start = (float)mousePosition.X;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var control = sender as FrameworkElement;

            Point mousePosition = e.GetPosition(control);
            var loopData = new MousePositionData(mousePosition, control.ActualWidth);

            // resize loop
            if (IsLoopBeginDragLeftHandle)
            {
                LoopStartCommand?.Execute(loopData);
            }

            if (IsLoopBeginDragRightHandle)
            {
                LoopEndCommand?.Execute(loopData);
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

        public static readonly DependencyProperty LoopCommandProperty = DependencyProperty.Register(
         nameof(LoopCommand),
         typeof(ICommand),
         typeof(LoopBehavior));

        public static readonly DependencyProperty LoopStartCommandProperty =
            DependencyProperty.Register(
                nameof(LoopStartCommand),
                typeof(ICommand),
                typeof(LoopBehavior));

        public static readonly DependencyProperty LoopEndCommandProperty =
            DependencyProperty.Register(
                nameof(LoopEndCommand),
                typeof(ICommand),
                typeof(LoopBehavior));

        public static readonly DependencyProperty IsLoopBeginDragLeftHandleProperty =
            DependencyProperty.Register(
          nameof(IsLoopBeginDragLeftHandle),
          typeof(bool),
          typeof(LoopBehavior));

        public static readonly DependencyProperty IsLoopBeginDragRightHandleProperty =
            DependencyProperty.Register(
          nameof(IsLoopBeginDragRightHandle),
          typeof(bool),
          typeof(LoopBehavior));
    }
}