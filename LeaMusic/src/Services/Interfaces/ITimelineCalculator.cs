namespace LeaMusic.src.Services.Interfaces
{
    using System.Drawing;

    public interface ITimelineCalculator
    {
        public (double newZoomFactor, TimeSpan zoomStartPosition) ZoomWaveformMouse(
            Point p,
            TimeSpan viewStartTime,
            TimeSpan viewDuration,
            double zoom,
            double width
        );

        public (double newZoomFactor, TimeSpan zoomStartPosition) ZoomWaveformSlider(
            double zoom,
            double width
        );

        public void ResetZoomParameter();

        public double ConvertPixelToSecond(
            double pixelPos,
            double viewStartTimeSec,
            double viewDurationSec,
            int renderWidth
        );

        public double CalculateSecRelativeToViewWindowPercentage(
            TimeSpan positionSec,
            TimeSpan viewStartTimeSec,
            TimeSpan viewDurationSec
        );

        public (TimeSpan paddedStart, TimeSpan paddedEnd, double zoomFactor) FitLoopToView(
            TimeSpan loopStart,
            TimeSpan loopEnd,
            TimeSpan totalDuration
        );
    }
}
