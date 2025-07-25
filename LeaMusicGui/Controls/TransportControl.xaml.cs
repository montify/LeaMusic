namespace LeaMusicGui.Controls
{
    using System.Windows;
    using System.Windows.Controls;

    public partial class TransportControl : UserControl
    {
        public TransportControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty HeightScaleFactorProperty =
            DependencyProperty.Register(
                nameof(HeightScaleFactor),
                typeof(double),
                typeof(TransportControl),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                )
            );

        public double HeightScaleFactor
        {
            get => (double)GetValue(HeightScaleFactorProperty);
            set => SetValue(HeightScaleFactorProperty, value);
        }
    }
}
