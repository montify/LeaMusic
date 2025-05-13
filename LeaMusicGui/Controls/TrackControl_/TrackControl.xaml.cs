using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LeaMusicGui.Controls.TrackControl_
{
    /// <summary>
    /// Interaction logic for TrackControl.xaml
    /// </summary>
    public partial class TrackControl : UserControl
    {
        public TrackControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TrackIDProperty =
         DependencyProperty.Register(
             nameof(TrackID),
             typeof(float),
             typeof(TrackControl),
             new PropertyMetadata(null));

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
           new PropertyMetadata(null));

        public static readonly DependencyProperty WaveformDataProperty =
           DependencyProperty.Register(
               nameof(WaveformData),
               typeof(ReadOnlyMemory<float>),
               typeof(TrackControl),
               new PropertyMetadata(null));

        public ReadOnlyMemory<float> WaveformData
        {
            get => (ReadOnlyMemory<float>)GetValue(WaveformDataProperty);
            set => SetValue(WaveformDataProperty, value);
        }


        public static readonly DependencyProperty MuteProperty =
        DependencyProperty.Register(
            "Mute",
            typeof(ICommand),
            typeof(TrackControl),
            new UIPropertyMetadata(null));

        public ICommand Mute
        {
            get { return (ICommand)GetValue(MuteProperty); }
            set { SetValue(MuteProperty, value); }
        }

        public static readonly DependencyProperty DeleteProperty =
      DependencyProperty.Register(
          "Delete",
          typeof(ICommand),
          typeof(TrackControl),
          new UIPropertyMetadata(null));

        public ICommand Delete
        {
            get { return (ICommand)GetValue(DeleteProperty); }
            set { SetValue(DeleteProperty, value); }
        }

        public object ParentViewModel
        {
            get { return (object)GetValue(MuteProperty); }
            set { SetValue(MuteProperty, value); }
        }


        public static readonly DependencyProperty ParentViewModelProperty =
    DependencyProperty.Register(nameof(ParentViewModel), typeof(GuiViewModel), typeof(TrackControl));



    }
}