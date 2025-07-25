namespace LeaMusic.src.Services.Interfaces
{
    public interface ITimelineService
    {
        public Memory<float> RequestSample(int trackId, int renderWidth);

        public Memory<float> RequestSample(
            int trackId,
            int renderWidth,
            TimeSpan start,
            TimeSpan end
        );
    }
}
