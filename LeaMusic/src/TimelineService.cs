namespace LeaMusic.src
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
    }
}
