namespace LeaMusicGui.Controls.TrackControl_
{
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class TrackControlViewModel : ObservableObject
    {
        private readonly Action<TrackControlViewModel> m_onDeleteRequested;
        private readonly Action<TrackControlViewModel> m_onMuteRequested;
        private readonly Action<TrackControlViewModel> m_onSoloRequested;
        private readonly Action<TrackControlViewModel, float> m_onVolumeChangeRequest;

        [ObservableProperty]
        private ReadOnlyMemory<float> waveform;

        [ObservableProperty]
        private int trackId;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool isMuted;

        [ObservableProperty]
        private bool isSolo;

        [ObservableProperty]
        private float volume;

        public ICommand MuteCommand { get; }

        public ICommand DeleteCommand { get; set; }

        public ICommand SoloCommand { get; set; }

        public ICommand VolumeChangeCommand { get; set; }

        public TrackControlViewModel(
            Action<TrackControlViewModel> onDeleteRequested,
            Action<TrackControlViewModel> onMuteRequested,
            Action<TrackControlViewModel> onSoloRequest,
            Action<TrackControlViewModel, float> onVolumeChangeRequest)
        {
            m_onDeleteRequested = onDeleteRequested;
            m_onMuteRequested = onMuteRequested;
            m_onSoloRequested = onSoloRequest;
            m_onVolumeChangeRequest = onVolumeChangeRequest;

            MuteCommand = new RelayCommand(OnMute);
            DeleteCommand = new RelayCommand(OnDelete);
            SoloCommand = new RelayCommand(OnSolo);
           // VolumeChangeCommand = new RelayCommand(OnVolumeChange);
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

        private void OnSolo()
        {
            m_onSoloRequested?.Invoke(this);
        }

        partial void OnVolumeChanged(float value)
        {
            m_onVolumeChangeRequest?.Invoke(this, value);
        }
    }
}