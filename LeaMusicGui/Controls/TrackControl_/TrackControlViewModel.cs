namespace LeaMusicGui.Controls.TrackControl_
{
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class TrackControlViewModel : ObservableObject
    {
        private readonly Action<TrackControlViewModel> m_onDeleteRequested;
        private readonly Action<TrackControlViewModel> m_onMuteRequested;

        [ObservableProperty]
        private ReadOnlyMemory<float> waveform;

        [ObservableProperty]
        private int trackId;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool isMuted;

        public ICommand MuteCommand { get; }

        public ICommand DeleteCommand { get; set; }

        public TrackControlViewModel(Action<TrackControlViewModel> onDeleteRequested, Action<TrackControlViewModel> onMuteRequested)
        {
            m_onDeleteRequested = onDeleteRequested;
            m_onMuteRequested = onMuteRequested;

            MuteCommand = new RelayCommand(OnMute);
            DeleteCommand = new RelayCommand(OnDelete);
        }

        public TrackControlViewModel()
        {
        }

        private void OnMute()
        {
            m_onMuteRequested?.Invoke(this);
        }

        private void OnDelete()
        {
            m_onDeleteRequested?.Invoke(this);
        }
    }
}