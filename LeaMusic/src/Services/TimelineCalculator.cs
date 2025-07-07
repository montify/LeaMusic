using Point = System.Drawing.Point;

namespace LeaMusic.src.Services
{ 
    public class TimelineCalculator
    {
        private TimeSpan zoomMouseStartPosition;
        private double zoomStartMouseY;
        private bool zoomStartPositionSetOnce;


        public (double newZoomFactor, TimeSpan zoomStartPosition) ZoomWaveformMouse(Point p,  TimeSpan viewStartTime, TimeSpan viewDuration, double Zoom, double width)
        {
            if (!zoomStartPositionSetOnce)
            {
                zoomMouseStartPosition = TimeSpan.FromSeconds(ConvertPixelToSecond(p.X, viewStartTime.TotalSeconds, viewDuration.TotalSeconds, (int)width));
                zoomStartPositionSetOnce = true;
                zoomStartMouseY = p.Y;
            }

            double zoomSensitivity = 0.002f;
            double maxZoom = 60;
            double minZoom = 1;
            double zoomRange = maxZoom - minZoom;

            var delta = p.Y - zoomStartMouseY;

            double relativeScale = 1 + (delta * zoomSensitivity);

            //Avoid crazy Zoom :D
            relativeScale = Math.Clamp(relativeScale, 0.5, 2.0);

            double newZoomFactor = Zoom * relativeScale;

            newZoomFactor = Math.Clamp(newZoomFactor, minZoom, maxZoom);

            return (newZoomFactor, zoomMouseStartPosition);
        }

        public void ResetZoomParameter()
        {
            zoomStartPositionSetOnce = false;
        }

        public double ConvertPixelToSecond(double pixelPos, double viewStartTimeSec, double viewDurationSec, int renderWidth)
        {
            double pixelPercentage = pixelPos / renderWidth;
            double timeInSeconds = viewStartTimeSec + (pixelPercentage * viewDurationSec);

            return timeInSeconds;
        }

        public double CalculateSecRelativeToViewWindowPercentage(TimeSpan positionSec, TimeSpan viewStartTimeSec, TimeSpan viewDurationSec)
        {
            TimeSpan positionInViewSec = positionSec - viewStartTimeSec;

            double relativePercentage = positionInViewSec.TotalSeconds / viewDurationSec.TotalSeconds;

            relativePercentage = Math.Max(0.0f, Math.Min(1.0f, relativePercentage));

            return relativePercentage * 100;
        }

        public (TimeSpan paddedStart, TimeSpan paddedEnd, double zoomFactor) FitLoopToView(TimeSpan loopStart, TimeSpan loopEnd, TimeSpan totalDuration)
        {
            if (loopEnd - loopStart <= TimeSpan.Zero)
                return default;

          
            var loopDuration = loopEnd - loopStart;

            //Add Padding left and right 
            double paddingFactor = 0.05;
            var padding = TimeSpan.FromSeconds(loopDuration.TotalSeconds * paddingFactor);
            var paddedStart = loopStart - padding;
            var paddedEnd = loopEnd + padding;
            paddedStart = TimeSpan.FromSeconds(Math.Max(0, paddedStart.TotalSeconds));
            paddedEnd = TimeSpan.FromSeconds(Math.Min(totalDuration.TotalSeconds, paddedEnd.TotalSeconds));

            var paddedDuration = paddedEnd - paddedStart;

            double zoomFactor = totalDuration.TotalSeconds / paddedDuration.TotalSeconds;

            return (paddedStart, paddedEnd, zoomFactor);
        }
    }
}
