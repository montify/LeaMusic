using LeaMusic.Src.AudioEngine_;

namespace LeaMusic.src.AudioEngine_
{
    public class TimelineService
    {
        public AudioEngine AudioEngine { get; private set; }

        public TimelineService(AudioEngine audioEngine)
        {
            AudioEngine = audioEngine;
        }

        public Memory<float> RequestSample(int trackId, int renderWidth)
        {
           return AudioEngine.Project.RequestSample(trackId, AudioEngine.ViewStartTime.TotalSeconds, AudioEngine.ViewEndTime.TotalSeconds, renderWidth);
        }

        public Memory<float> RequestSample(int trackId, int renderWidth, TimeSpan start, TimeSpan end)
        {
            return AudioEngine.Project.RequestSample(trackId, start.TotalSeconds, end.TotalSeconds, renderWidth);
        }
    }
}
