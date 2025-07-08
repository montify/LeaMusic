namespace LeaMusicGui
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using LeaMusic.Src.AudioEngine_;

    public partial class MarkerDTO : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private double positionRelativeView;

        [ObservableProperty]
        private BeatMarker marker;

        [ObservableProperty]
        private bool visible = true;
    }
}