namespace LeaMusicGui
{
    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class TrackDTO : ObservableObject
    {
        [ObservableProperty]
        private ReadOnlyMemory<float> waveform;

        public int TrackID { get; set; }

        public string Name { get; set; }
    }
}