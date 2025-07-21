namespace LeaMusic.Src.Services
{
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;
    using Point = System.Drawing.Point;

    public class TimelineCalculator : ITimelineCalculator
    {
        private readonly IAudioEngine m_audioEngine;
        private readonly IViewWindowProvider m_viewWindowProvider;

        private TimeSpan m_zoomMouseStartPosition;
        private double m_zoomStartMouseY;
        private bool m_zoomStartPositionSetOnce;


        public TimelineCalculator(
            IAudioEngine audioEngine,
            IViewWindowProvider viewWindowProvider)
        {
            m_audioEngine = audioEngine;
            m_viewWindowProvider = viewWindowProvider;
        }

        public (double newZoomFactor, TimeSpan zoomStartPosition) ZoomWaveformMouse(Point p,  TimeSpan viewStartTime, TimeSpan viewDuration, double zoom, double width)
        {
            if (!m_zoomStartPositionSetOnce)
            {
                m_zoomMouseStartPosition = TimeSpan.FromSeconds(ConvertPixelToSecond(p.X, viewStartTime.TotalSeconds, viewDuration.TotalSeconds, (int)width));
                m_zoomStartPositionSetOnce = true;
                m_zoomStartMouseY = p.Y;
            }

            double zoomSensitivity = AppConstants.ZoomSensitivity;
            double maxZoom = AppConstants.MaxZoom;
            double minZoom = AppConstants.MinZoom;
            double zoomRange = maxZoom - minZoom;

            var delta = p.Y - m_zoomStartMouseY;
            double relativeScale = 1 + (delta * zoomSensitivity);

            // Avoid crazy Zoom :D
            relativeScale = Math.Clamp(relativeScale, 0.5, 2.0);

            double newZoomFactor = zoom * relativeScale;

            newZoomFactor = Math.Clamp(newZoomFactor, minZoom, maxZoom);

            return (newZoomFactor, m_zoomMouseStartPosition);
        }

        public (double newZoomFactor, TimeSpan zoomStartPosition) ZoomWaveformSlider(double zoom, double width)
        {
            var pa = ConvertSecondToPixel(m_audioEngine.CurrentPosition.TotalSeconds, m_viewWindowProvider.ViewStartTime.TotalSeconds, m_viewWindowProvider.ViewDuration.TotalSeconds, (int)width);

            Point p = new Point((int)pa, 0);

            if (!m_zoomStartPositionSetOnce)
            {
                m_zoomMouseStartPosition = TimeSpan.FromSeconds(ConvertPixelToSecond(p.X, m_viewWindowProvider.ViewStartTime.TotalSeconds, m_viewWindowProvider.ViewDuration.TotalSeconds, (int)width));
                m_zoomStartPositionSetOnce = true;
                m_zoomStartMouseY = p.Y;
            }

            double zoomSensitivity = AppConstants.ZoomSensitivity;
            double maxZoom = AppConstants.MaxZoom;
            double minZoom = AppConstants.MinZoom;
            double zoomRange = maxZoom - minZoom;

           // var delta = p.Y - m_zoomStartMouseY;
            double relativeScale = 1 + (zoom * zoomSensitivity);

            // Avoid crazy Zoom :D
            relativeScale = Math.Clamp(relativeScale, 0.5, 2.0);

            double newZoomFactor = zoom * relativeScale;

            newZoomFactor = Math.Clamp(newZoomFactor, minZoom, maxZoom);

            return (newZoomFactor, m_zoomMouseStartPosition);
        }

        public void ResetZoomParameter()
        {
            m_zoomStartPositionSetOnce = false;
        }

        public double ConvertPixelToSecond(double pixelPos, double viewStartTimeSec, double viewDurationSec, int renderWidth)
        {
            double pixelPercentage = pixelPos / renderWidth;
            double timeInSeconds = viewStartTimeSec + (pixelPercentage * viewDurationSec);

            return timeInSeconds;
        }

        public double ConvertSecondToPixel(double timeInSeconds, double viewStartTimeSec, double viewDurationSec, int renderWidth)
        {
            double timeOffset = timeInSeconds - viewStartTimeSec;
            double timePercentage = timeOffset / viewDurationSec;
            double pixelPos = timePercentage * renderWidth;

            return pixelPos;
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
            {
                return default;
            }

            var loopDuration = loopEnd - loopStart;

            // Add Padding left and right
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