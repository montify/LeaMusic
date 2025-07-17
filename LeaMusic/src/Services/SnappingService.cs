namespace LeaMusic.src.Services
{
    using LeaMusic.Src.AudioEngine_;
    using LeaMusic.src.Services.Interfaces;

    public class SnappingService : ISnappingService
    {
        public TimeSpan SnapToMarkers(
                      TimeSpan originalPosition,
                      IEnumerable<BeatMarker> beatMarkers,
                      TimeSpan viewStartTime,
                      TimeSpan viewDuration,
                      double renderWidth,
                      int thresholdInMs)
        {
            TimeSpan snappedPosition = originalPosition;

            foreach (var beatMarker in beatMarkers)
            {
                double pixelsPerSecond = renderWidth / viewDuration.TotalSeconds;
                var snapTimeThreshold = TimeSpan.FromSeconds(thresholdInMs / pixelsPerSecond);

                var diff = (beatMarker.Position - snappedPosition).Duration();

                if (diff < snapTimeThreshold)
                {
                    snappedPosition = beatMarker.Position;
                    return snappedPosition;
                }
            }

            return snappedPosition;
        }
    }
}