namespace LeaMusicGui.Controls
{
    using System.Windows;
    using System.Windows.Controls;

    public partial class PlayheadControl : UserControl
    {
        public PlayheadControl()
        {
            InitializeComponent();
        }

        public double PlayheadPercentage
        {
            get => (double)GetValue(PlayheadPercentageProperty);
            set => SetValue(PlayheadPercentageProperty, value);
        }

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

        public static readonly DependencyProperty PlayheadPercentageProperty =
            DependencyProperty.Register(
                nameof(PlayheadPercentage),
                typeof(double),
                typeof(PlayheadControl),
                new PropertyMetadata(0.0)
            );

        public static readonly DependencyProperty ParentHeightProperty =
            DependencyProperty.Register(
                nameof(ParentHeight),
                typeof(double),
                typeof(PlayheadControl),
                new PropertyMetadata(0.0)
            );

        public static readonly DependencyProperty ParentWidthProperty = DependencyProperty.Register(
            nameof(ParentWidth),
            typeof(double),
            typeof(PlayheadControl),
            new PropertyMetadata(0.0)
        );
    }
}
