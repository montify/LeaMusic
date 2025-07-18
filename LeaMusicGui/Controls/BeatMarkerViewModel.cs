namespace LeaMusicGui
{
    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class BeatMarkerViewModel : ObservableObject
    {
        [ObservableProperty]
        private int id;

        [ObservableProperty]
        private double positionRelativeView;

        [ObservableProperty]
        private bool visible = true;
    }
}