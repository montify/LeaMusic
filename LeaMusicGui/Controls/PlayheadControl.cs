using SkiaSharp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LeaMusicGui.Controls
{
    public class PlayheadControl : UserControl
    {
        private WriteableBitmap WriteableBitmap;
        private SKCanvas canvas;
        private SKSurface surface;
        private SKPaint paint;

        private int width;
        private int height;


        public PlayheadControl()
        {
            WriteableBitmap = CreateImage(100, 300);
            width = (int)WriteableBitmap.Width;
            height = (int)WriteableBitmap.Height;

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            surface = SKSurface.Create(info);

            paint = new SKPaint
            {
                Color = SKColor.Parse("#FFFFFF"),
                IsStroke = true,
                IsAntialias = true,
                StrokeWidth = 1
            };

            //Plan is to render the playhead once, and move its position with offset instead redraw everytime, so just draw it once
            UpdateImage();
        }

        public void UpdateImage()
        {
            canvas = surface.Canvas;
            canvas.Clear(new SKColor(130, 130, 130));

            for (int i = 0; i < width; i++)
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.Color = SKColor.Parse("#000000");

                //Progress
                var progressStart = new SKPoint(i, -height);
                var progressEnd = new SKPoint(i, height);

                paint.Color = SKColor.Parse("#913d23");
                canvas.DrawLine(progressStart, progressEnd, paint);

            }

            using (SKImage image = surface.Snapshot())
            using (SKPixmap pixmap = image.PeekPixels())
            {
                WriteableBitmap.Lock();

                unsafe
                {
                    Buffer.MemoryCopy(
                        source: (void*)pixmap.GetPixels(),
                        destination: (void*)WriteableBitmap.BackBuffer,
                        destinationSizeInBytes: WriteableBitmap.BackBufferStride * height,
                        sourceBytesToCopy: pixmap.RowBytes * height);
                }

                WriteableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
                WriteableBitmap.Unlock();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(WriteableBitmap, new Rect(0, 0, Width, Height));
            base.OnRender(drawingContext);
        }


        public WriteableBitmap CreateImage(int width, int height)
        {
            return new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
        }


        //public float Percentage
        //{
        //    get => (float)GetValue(PercentageProperty);
        //    set => SetValue(PercentageProperty, value);
        //}

        //public static readonly DependencyProperty PercentageProperty =
        //   DependencyProperty.Register(nameof(Percentage), typeof(float),
        //   typeof(PlayheadControl),
        //   new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, OnWaveformChanged));

        //private static void OnWaveformChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
           
        //}
    }
}
