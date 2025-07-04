using LeaMusic.src.AudioEngine_;
using System.Windows;

namespace LeaMusicGui
{
    internal class Helpers
    {
        public static TimeSpan CheckSnapping(AudioEngine audioEngine, double renderWidth, TimeSpan markerPosition, TimeSpan checkPosition, double treshholdInMs)
        {
            var beatmarkers = audioEngine.Project.BeatMarkers;
            var endLoopPosition = checkPosition;
            double pixelsPerSecond = renderWidth / audioEngine.ViewDuration.TotalSeconds;
            var diff = (markerPosition - endLoopPosition).Duration();
            var snapTimeThreshold = TimeSpan.FromSeconds(treshholdInMs / pixelsPerSecond);

            if (diff < snapTimeThreshold)
                checkPosition = markerPosition;

            return checkPosition;
        }

        public static UIElement? DraggedElement { get; set; } = null;

    }
}
