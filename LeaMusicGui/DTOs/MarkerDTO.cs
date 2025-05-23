using CommunityToolkit.Mvvm.ComponentModel;
using LeaMusic.src.AudioEngine_;

namespace LeaMusicGui
{
    public partial class MarkerDTO : ObservableObject
    {
        [ObservableProperty]
        private double positionRelativeView;

        [ObservableProperty]
        private Marker marker;

        [ObservableProperty]
        public bool visible = true;
    }
}