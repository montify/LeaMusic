namespace LeaMusicGui.Behaviors.BehaviorDTOs
{
    using Point = System.Windows.Point;

    public class MousePositionData
    {
        public Point MousePosition { get; set; }

        public double ControlActualWidth { get; set; }

        public MousePositionData(Point mousePosition, double controlActualWidth)
        {
            MousePosition = mousePosition;
            ControlActualWidth = controlActualWidth;
        }
    }
}
