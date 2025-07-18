namespace LeaMusicGui.Behaviors.BehaviorDTOs
{
    using Point = System.Windows.Point;

    public class LoopData
    {
        public float MousePositionStart { get; set; }

        public float MousePositionEnd { get; set; }

        public double ControlActualWidth { get; set; }

        public LoopData(float mousePositionStart, float mousePositionEnd, double controlActualWidth)
        {
            MousePositionStart = mousePositionStart;
            MousePositionEnd = mousePositionEnd;

            ControlActualWidth = controlActualWidth;
        }
    }
}
