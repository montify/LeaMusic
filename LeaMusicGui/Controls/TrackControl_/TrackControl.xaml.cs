namespace LeaMusicGui.Controls.TrackControl_
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    public partial class TrackControl : UserControl
    {
        public TrackControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TrackIDProperty = DependencyProperty.Register(
            nameof(TrackID),
            typeof(float),
            typeof(TrackControl),
            new PropertyMetadata(null)
        );

        public int TrackID
        {
            get => (int)GetValue(TrackIDProperty);
            set => SetValue(TrackIDProperty, value);
        }

        public float HeightScaleFactor
        {
            get => (float)GetValue(HeightScaleFactorProperty);
            set => SetValue(HeightScaleFactorProperty, value);
        }

        public static readonly DependencyProperty HeightScaleFactorProperty =
            DependencyProperty.Register(
                nameof(HeightScaleFactor),
                typeof(int),
                typeof(TrackControl),
                new PropertyMetadata(null)
            );

        public ICommand Mute
        {
            get { return (ICommand)GetValue(MuteProperty); }
            set { SetValue(MuteProperty, value); }
        }

        public ICommand Delete
        {
            get { return (ICommand)GetValue(DeleteProperty); }
            set { SetValue(DeleteProperty, value); }
        }

        public static readonly DependencyProperty DeleteProperty = DependencyProperty.Register(
            "Delete",
            typeof(ICommand),
            typeof(TrackControl),
            new UIPropertyMetadata(null)
        );

        public static readonly DependencyProperty MuteProperty = DependencyProperty.Register(
            "Mute",
            typeof(ICommand),
            typeof(TrackControl),
            new UIPropertyMetadata(null)
        );

        public static readonly DependencyProperty RequestWaveformUpdateCommandProperty =
            DependencyProperty.Register(
                nameof(RequestWaveformUpdateCommand),
                typeof(ICommand),
                typeof(TrackControl),
                new PropertyMetadata(null)
            ); // No callback needed for the command itself

        public ICommand RequestWaveformUpdateCommand
        {
            get => (ICommand)GetValue(RequestWaveformUpdateCommandProperty);
            set => SetValue(RequestWaveformUpdateCommandProperty, value);
        }
    }
}
