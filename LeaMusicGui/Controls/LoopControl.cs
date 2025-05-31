using SkiaSharp;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LeaMusicGui.Controls
{
    public class LoopControl : UserControl
    {
        private WriteableBitmap WriteableBitmap;
        private SKCanvas canvas;
        private SKSurface surface;
        private SKPaint paint;

        public int width;
        private int height;

        public LoopControl()
        {
            WriteableBitmap = new WriteableBitmap(1, 3000, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
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

            UpdateImage();
        }

        public void UpdateImage()
        {
            Debug.WriteLine(width);
            canvas = surface.Canvas;
            canvas.Clear(new SKColor(0, 0, 0,0));

            for (int i = 0; i < width; i++)
            {
                paint.Style = SKPaintStyle.StrokeAndFill;

                paint.Color = SKColors.Green;
                var rect = new SKRect(0, 0, width, 30);
                canvas.DrawRect(rect, paint);
                paint.Color = SKColors.DarkRed.WithAlpha(70);

                rect = new SKRect(0, 0, width, height);
                canvas.DrawRect(rect, paint);

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
            drawingContext.DrawImage(WriteableBitmap, new Rect(0, 0, width, height));
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
        DependencyProperty.Register(nameof(SelectionStartPercentage), typeof(float),
        typeof(LoopControl),
        new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, OnLoopChange));

        public static readonly DependencyProperty SelectionEndProperty =
        DependencyProperty.Register(nameof(SelectionEndPercentage), typeof(float),
        typeof(LoopControl),
        new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, OnLoopChange));
    }
}
