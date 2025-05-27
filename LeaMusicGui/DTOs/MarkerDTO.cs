using CommunityToolkit.Mvvm.ComponentModel;
using LeaMusic.src.AudioEngine_;
using System.Drawing;

namespace LeaMusicGui
{
    public partial class MarkerDTO : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private double positionRelativeView;

        [ObservableProperty]
        private Marker marker;

        [ObservableProperty]
        public bool visible = true;
    }
}