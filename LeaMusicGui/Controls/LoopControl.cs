namespace LeaMusicGui.Controls
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using SkiaSharp;

    public class LoopControl : UserControl
    {
        private WriteableBitmap m_writeableBitmap;
        private SKCanvas m_canvas;
        private SKSurface m_surface;
        private SKPaint m_paint;
        private int m_width;
        private int m_height;

        public LoopControl()
        {
            m_writeableBitmap = new WriteableBitmap(1, 300, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            m_width = (int)m_writeableBitmap.Width;
            m_height = (int)m_writeableBitmap.Height;

            var info = new SKImageInfo(m_width, m_height, SKColorType.Bgra8888, SKAlphaType.Premul);

            m_surface = SKSurface.Create(info);

            m_paint = new SKPaint
            {
                Color = SKColor.Parse("#FFFFFF"),
                IsStroke = true,
                IsAntialias = true,
                StrokeWidth = 1,
            };

            UpdateImage();
        }

        public void UpdateImage()
        {
            Debug.WriteLine(m_width);
            m_canvas = m_surface.Canvas;
            m_canvas.Clear(new SKColor(0, 0, 0, 0));

            m_paint.Style = SKPaintStyle.StrokeAndFill;

            // Upper Bar
            m_paint.Color = SKColors.Green;
            var rect = new SKRect(0, 0, m_width, 10);
            m_canvas.DrawRect(rect, m_paint);

            m_paint.Color = SKColors.DarkRed.WithAlpha(90);
            rect = new SKRect(0, 0, m_width, m_height);
            m_canvas.DrawRect(rect, m_paint);

            using (SKImage image = m_surface.Snapshot())
            using (SKPixmap pixmap = image.PeekPixels())
            {
                m_writeableBitmap.Lock();

                unsafe
                {
                    Buffer.MemoryCopy(
                        source: (void*)pixmap.GetPixels(),
                        destination: (void*)m_writeableBitmap.BackBuffer,
                        destinationSizeInBytes: m_writeableBitmap.BackBufferStride * m_height,
                        sourceBytesToCopy: pixmap.RowBytes * m_height);
                }

                m_writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, m_width, m_height));
                m_writeableBitmap.Unlock();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(m_writeableBitmap, new Rect(0, 0, m_width, m_height));

            base.OnRender(drawingContext);
        }

        public float SelectionStartPercentage
        {
            get => (float)GetValue(SelectionStartProperty);
            set => SetValue(SelectionStartProperty, value);
        }

        public float SelectionEndPercentage
        {
            get => (float)GetValue(SelectionEndProperty);
            set => SetValue(SelectionEndProperty, value);
        }

        private static void OnLoopChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LoopControl control)
            {
                Debug.WriteLine($"Loop Redraw");
                control.InvalidateVisual();
                control.UpdateImage();
            }
        }

        public static readonly DependencyProperty SelectionStartProperty =
            DependencyProperty.Register(
            nameof(SelectionStartPercentage),
            typeof(float),
            typeof(LoopControl),
            new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, OnLoopChange));

        public static readonly DependencyProperty SelectionEndProperty =
            DependencyProperty.Register(
            nameof(SelectionEndPercentage),
            typeof(float),
            typeof(LoopControl),
            new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, OnLoopChange));
    }
}
