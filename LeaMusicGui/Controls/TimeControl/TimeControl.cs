namespace LeaMusicGui.Controls.TimeControl
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using SkiaSharp;

    public class TimeControl : UserControl
    {
        private WriteableBitmap m_writeableBitmap;
        private SKCanvas m_canvas;
        private SKSurface m_surface;
        private SKPaint m_paint;
        private int m_width;
        private int m_height;

        public TimeControl()
        {
            m_writeableBitmap = new WriteableBitmap(1334, 30, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
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
            if (ViewModel == null)
            {
                return;
            }

            m_canvas = m_surface.Canvas;
            m_canvas.Clear(new SKColor(0, 0, 0, 0));

            var pixelsPerSecond = m_width / ViewModel.TotalSeconds.TotalSeconds;
            pixelsPerSecond *= ViewModel.ZoomFactor;

            double startSec = ViewModel.ViewStart.TotalSeconds;
            double endSec = ViewModel.ViewEnd.TotalSeconds;

            double step = CalculateSpacing(pixelsPerSecond);

            for (double second = Math.Floor(startSec / step) * step; second <= Math.Ceiling(endSec); second += step)
            {
                float x = (float)((second - startSec) * pixelsPerSecond);

                m_canvas.DrawLine(x, 0, x, 10, m_paint);
                m_canvas.DrawText($"{second}s", x + 2, 25, m_paint);
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
                        sourceBytesToCopy: pixmap.RowBytes * m_height);
                }

                m_writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, m_width, m_height));
                m_writeableBitmap.Unlock();
            }
        }

        private double CalculateSpacing(double pixelsPerSecond)
        {
            double minLabelSpacing = 60; // pixels
            double step = Math.Max(1, Math.Ceiling(minLabelSpacing / pixelsPerSecond));
            return step;
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

            m_writeableBitmap = new WriteableBitmap(m_width, m_height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            m_width = (int)m_writeableBitmap.Width;
            m_height = (int)m_writeableBitmap.Height;

            var info = new SKImageInfo(m_width, m_height, SKColorType.Bgra8888, SKAlphaType.Premul);

            m_surface = SKSurface.Create(info);
            UpdateImage();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(m_writeableBitmap, new Rect(0, 0, m_width, m_height));

            base.OnRender(drawingContext);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            // Skip invalid sizes
            if (sizeInfo.NewSize.Width < 1 || sizeInfo.NewSize.Height < 1)
            {
                return;
            }

            m_width = (int)sizeInfo.NewSize.Width;
            m_height = (int)sizeInfo.NewSize.Height;

            Resize();
            UpdateImage();

            base.OnRenderSizeChanged(sizeInfo);
        }

        private static void OnRulerControlChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TimeControl)d;
            if (e.OldValue is TimeControlViewModel oldVm)
            {
                oldVm.PropertyChanged -= control.OnViewModelPropertyChanged;
            }

            if (e.NewValue is TimeControlViewModel newVm)
            {
                newVm.PropertyChanged += control.OnViewModelPropertyChanged;
            }

            control.UpdateImage();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
                UpdateImage();
        }

        public static readonly DependencyProperty ViewModelProperty =
       DependencyProperty.Register(
           nameof(ViewModel),
           typeof(TimeControlViewModel),
           typeof(TimeControl),
           new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnRulerControlChange));

        public TimeControlViewModel ViewModel
        {
            get => (TimeControlViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
    }
}
