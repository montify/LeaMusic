namespace LeaMusicGui.Behaviors.BehaviorDTOs
{
    using Point = System.Windows.Point;

    public class LoopDataStartEnd
    {
        public Point MousePosition { get; set; }

        public double ControlActualWidth { get; set; }

        public LoopDataStartEnd(Point mousePosition, double controlActualWidth)
        {
            MousePosition = mousePosition;
            ControlActualWidth = controlActualWidth;
        }
    }
}
