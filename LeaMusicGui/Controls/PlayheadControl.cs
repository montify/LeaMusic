namespace LeaMusicGui.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using SkiaSharp;

    public class PlayheadControl : UserControl
    {
        private WriteableBitmap m_writeableBitmap;
        private SKCanvas m_canvas;
        private SKSurface m_surface;
        private SKPaint m_paint;
        private int m_width;
        private int m_height;

        public PlayheadControl()
        {
            m_writeableBitmap = CreateImage(100, 300);
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
            m_canvas = m_surface.Canvas;
            m_canvas.Clear(new SKColor(130, 130, 130));

            for (int i = 0; i < m_width; i++)
            {
                m_paint.Style = SKPaintStyle.Stroke;
                m_paint.Color = SKColor.Parse("#000000");

                // Progress
                var progressStart = new SKPoint(i, -m_height);
                var progressEnd = new SKPoint(i, m_height);

                m_paint.Color = SKColor.Parse("#913d23");
                m_canvas.DrawLine(progressStart, progressEnd, m_paint);
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

        public WriteableBitmap CreateImage(int width, int height)
        {
            return new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(m_writeableBitmap, new Rect(0, 0, Width, Height));
            base.OnRender(drawingContext);
        }
    }
}
