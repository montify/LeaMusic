using CommunityToolkit.Mvvm.ComponentModel;

namespace LeaMusicGui
{
    public partial class TrackDTO : ObservableObject
    {
        [ObservableProperty]
        private ReadOnlyMemory<float> _waveform;

        public int TrackID { get; set; }
    }
}