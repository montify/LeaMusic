namespace LeaMusicGui.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using SkiaSharp;

    public class WaveformControl : UserControl
    {
        private readonly DispatcherTimer m_resizeTimer;

        private WriteableBitmap m_writeableBitmap;
        private SKCanvas m_canvas;
        private SKSurface m_surface;
        private SKPaint m_paint;
        private int m_width;
        private int m_height;

        public WaveformControl()
        {
            m_writeableBitmap = CreateImage(1200, 300);
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

            m_resizeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };

            m_resizeTimer.Tick += (s, e) =>
            {
                m_resizeTimer.Stop();
                Resize();
            };
        }

        public WriteableBitmap CreateImage(int width, int height)
        {
            return new WriteableBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                BitmapPalettes.Halftone256Transparent
            );
        }

        public void UpdateImage()
        {
            m_canvas = m_surface.Canvas;
            m_canvas.Clear(SKColor.Parse("#424651"));

            if (WaveformData.IsEmpty)
            {
                return;
            }

            for (int i = 0; i < m_width; i++)
            {
                m_paint.Style = SKPaintStyle.Stroke;
                m_paint.Color = SKColor.Parse("#AF3B6E");

                // When NOT bound to a slider Value, the value is 0
                if (WaveformHeightMulti <= 0.0f)
                {
                    WaveformHeightMulti = 1f;
                }

                float heightMulti = 40 * WaveformHeightMulti;
                float middle = m_height / 2;
                var start = new SKPoint(i, middle);
                float sample = 0;

                sample = WaveformData.Span[Math.Min(i, WaveformData.Length - 1)];

                var end = new SKPoint(i, middle + (sample * heightMulti));
                var end2 = new SKPoint(i, middle - (sample * heightMulti));

                m_canvas.DrawLine(start, end, m_paint);
                m_canvas.DrawLine(start, end2, m_paint);

                m_paint.Color = new SKColor(1, 1, 1);
                m_paint.StrokeWidth = 2;
                m_canvas.DrawPoint(end, m_paint);
                m_canvas.DrawPoint(end2, m_paint);
            }

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
                        sourceBytesToCopy: pixmap.RowBytes * m_height
                    );
                }

                m_writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, m_width, m_height));
                m_writeableBitmap.Unlock();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(m_writeableBitmap, new Rect(0, 0, Width, Height));
            base.OnRender(drawingContext);
        }

        public ReadOnlyMemory<float> WaveformData
        {
            get => (ReadOnlyMemory<float>)GetValue(WaveformDataProperty);
            set => SetValue(WaveformDataProperty, value);
        }

        public float WaveformHeightMulti
        {
            get => (float)GetValue(WaveformHeightMultiProperty);
            set => SetValue(WaveformHeightMultiProperty, value);
        }

        private static void WaveFormRedraw(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaveformControl control)
            {
                if (e.OldValue != e.NewValue)
                {
                    control.UpdateImage();
                }
            }
        }

        private void Resize()
        {
            if (ActualWidth == 0)
            {
                return;
            }

            // TODO: DOnt destroy Bitmap, just update it!
            m_surface.Dispose();
            m_writeableBitmap = null;
            GC.Collect();

            m_writeableBitmap = CreateImage((int)ActualWidth, 300);
            m_width = (int)m_writeableBitmap.Width;
            m_height = (int)m_writeableBitmap.Height;

            var info = new SKImageInfo(m_width, m_height, SKColorType.Bgra8888, SKAlphaType.Premul);

            m_surface = SKSurface.Create(info);

            if (RequestWaveformUpdateCommand != null)
            {
                RequestWaveformUpdateCommand.Execute(ActualWidth);
            }

            UpdateImage();
            InvalidateVisual();
        }

        private Size m_lastSize;

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // Skip invalid sizes
            if (sizeInfo.NewSize.Width < 1 || sizeInfo.NewSize.Height < 1)
            {
                return;
            }

            // Avoid redundant resizing
            if (m_lastSize.Width == sizeInfo.NewSize.Width)
            {
                return;
            }

            m_lastSize = sizeInfo.NewSize;

            m_resizeTimer.Stop();
            m_resizeTimer.Start();

            base.OnRenderSizeChanged(sizeInfo);
        }

        public static readonly DependencyProperty WaveformHeightMultiProperty =
            DependencyProperty.Register(
                nameof(WaveformHeightMulti),
                typeof(float),
                typeof(WaveformControl),
                new FrameworkPropertyMetadata(
                    1.0f,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    WaveFormRedraw
                )
            );

        public static readonly DependencyProperty WaveformDataProperty =
            DependencyProperty.Register(
                nameof(WaveformData),
                typeof(ReadOnlyMemory<float>),
                typeof(WaveformControl),
                new FrameworkPropertyMetadata(
                    default(ReadOnlyMemory<float>),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    WaveFormRedraw
                )
            );

        public static readonly DependencyProperty RequestWaveformUpdateCommandProperty =
            DependencyProperty.Register(
                nameof(RequestWaveformUpdateCommand),
                typeof(ICommand),
                typeof(WaveformControl),
                new PropertyMetadata(null)
            ); // No callback needed for the command itself

        public ICommand RequestWaveformUpdateCommand
        {
            get => (ICommand)GetValue(RequestWaveformUpdateCommandProperty);
            set => SetValue(RequestWaveformUpdateCommandProperty, value);
        }
    }
}
