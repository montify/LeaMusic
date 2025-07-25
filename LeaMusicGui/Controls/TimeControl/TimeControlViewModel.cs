namespace LeaMusicGui.Controls.TimeControl
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using LeaMusic.src.AudioEngine_.Interfaces;
    using LeaMusic.src.Services.Interfaces;

    public partial class TimeControlViewModel : ObservableObject
    {
        private readonly IViewWindowProvider m_viewWindowProvider;
        private readonly IAudioEngine m_audioEngine;

        [ObservableProperty]
        public TimeSpan totalSeconds;

        [ObservableProperty]
        public TimeSpan viewStart;

        [ObservableProperty]
        public TimeSpan viewEnd;

        [ObservableProperty]
        public double zoomFactor = 1;

        public TimeControlViewModel(
            IViewWindowProvider viewWindowProvider,
            IAudioEngine audioEngine
        )
        {
            m_viewWindowProvider = viewWindowProvider;
            m_audioEngine = audioEngine;

            m_audioEngine.OnUpdate += M_audioEngine_OnUpdate;
        }

        private void M_audioEngine_OnUpdate(TimeSpan obj)
        {
            ViewStart = m_viewWindowProvider.ViewStartTime;
            ViewEnd = m_viewWindowProvider.ViewEndTime;
            TotalSeconds = m_audioEngine.TotalDuration;
            ZoomFactor = m_viewWindowProvider.Zoom;
        }
    }
}
