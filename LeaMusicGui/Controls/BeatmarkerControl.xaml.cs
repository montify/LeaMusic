namespace LeaMusicGui.Controls
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using LeaMusic.Src.AudioEngine_;

    public partial class BeatmarkerControl : UserControl
    {
        public BeatmarkerControl()
        {
            InitializeComponent();
        }

        public ObservableCollection<BeatMarkerViewModel> BeatMarkers
        {
            get => (ObservableCollection<BeatMarkerViewModel>)GetValue(BeatMarkersProperty);
            set => SetValue(BeatMarkersProperty, value);
        }

        public static readonly DependencyProperty BeatMarkersProperty = DependencyProperty.Register(
            nameof(BeatMarkers),
            typeof(ObservableCollection<BeatMarkerViewModel>),
            typeof(BeatmarkerControl)
        );

        public bool IsBeatMarkerMoving
        {
            get => (bool)GetValue(IsBeatMarkerMovingProperty);
            set => SetValue(IsBeatMarkerMovingProperty, value);
        }

        public static readonly DependencyProperty IsBeatMarkerMovingProperty =
            DependencyProperty.Register(
                nameof(IsBeatMarkerMoving),
                typeof(bool),
                typeof(BeatmarkerControl),
                new PropertyMetadata(false)
            );

        public ICommand MarkerClickCommand
        {
            get => (ICommand)GetValue(MarkerClickCommandProperty);
            set => SetValue(MarkerClickCommandProperty, value);
        }

        public static readonly DependencyProperty MarkerClickCommandProperty =
            DependencyProperty.Register(
                nameof(MarkerClickCommand),
                typeof(ICommand),
                typeof(BeatmarkerControl)
            );

        public ICommand MarkerDeleteCommand
        {
            get => (ICommand)GetValue(MarkerDeleteCommandProperty);
            set => SetValue(MarkerDeleteCommandProperty, value);
        }

        public static readonly DependencyProperty MarkerDeleteCommandProperty =
            DependencyProperty.Register(
                nameof(MarkerDeleteCommand),
                typeof(ICommand),
                typeof(BeatmarkerControl)
            );

        public double ParentHeight
        {
            get => (double)GetValue(ParentHeightProperty);
            set => SetValue(ParentHeightProperty, value);
        }

        public double ParentWidth
        {
            get => (double)GetValue(ParentWidthProperty);
            set => SetValue(ParentWidthProperty, value);
        }

        public static readonly DependencyProperty ParentHeightProperty =
            DependencyProperty.Register(
                nameof(ParentHeight),
                typeof(double),
                typeof(BeatmarkerControl),
                new PropertyMetadata(0.0)
            );

        public static readonly DependencyProperty ParentWidthProperty = DependencyProperty.Register(
            nameof(ParentWidth),
            typeof(double),
            typeof(BeatmarkerControl),
            new PropertyMetadata(0.0)
        );
    }
}
