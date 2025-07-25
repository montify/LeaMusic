using LeaMusic.Src.AudioEngine_;

namespace LeaMusic.src.Services.Interfaces
{
    public interface ISnappingService
    {
        public TimeSpan SnapToMarkers(
            TimeSpan originalPosition,
            IEnumerable<BeatMarker> beatMarkers,
            TimeSpan viewStartTime,
            TimeSpan viewDuration,
            double renderWidth,
            int thresholdInMs
        );
    }
}
