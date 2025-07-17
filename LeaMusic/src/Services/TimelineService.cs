namespace LeaMusic.src.Services
{
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;

    public class TimelineService : ITimelineService
    {
        private readonly IProjectProvider m_projectProvider;

        private readonly IViewWindowProvider m_viewWindowProvider;

        public TimelineService(
            IProjectProvider projectProvider,
            IViewWindowProvider viewWindowProvider)
        {
            m_projectProvider = projectProvider;
            m_viewWindowProvider = viewWindowProvider;
        }

        public Memory<float> RequestSample(int trackId, int renderWidth)
        {
           return m_projectProvider.Project.RequestSample(trackId, m_viewWindowProvider.ViewStartTime.TotalSeconds, m_viewWindowProvider.ViewEndTime.TotalSeconds, renderWidth);
        }

        public Memory<float> RequestSample(int trackId, int renderWidth, TimeSpan start, TimeSpan end)
        {
            return m_projectProvider.Project.RequestSample(trackId, start.TotalSeconds, end.TotalSeconds, renderWidth);
        }
    }
}
