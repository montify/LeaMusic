using LeaMusicGui.Controls.TrackControl_;
using SkiaSharp;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LeaMusicGui.Controls
{
    public class WaveformControl : UserControl
    {
        private WriteableBitmap WriteableBitmap;
        private SKCanvas canvas;
        private SKSurface surface;
        private SKPaint paint;

        private int width;
        private int height;

        public WaveformControl()
        {
            WriteableBitmap = CreateImage(1200, 300);
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

          
         //   CompositionTarget.Rendering += Update;
        }

        bool updateOnce = false;
        private void Update(object? sender, EventArgs e)
        {
            if (updateOnce && !WaveformData.IsEmpty)
            {
                UpdateImage();
                updateOnce = true;
            }
        }

        public WriteableBitmap CreateImage(int width, int height)
        {
            return new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
        }

        public void UpdateImage()
        {
            canvas = surface.Canvas;
            canvas.Clear(SKColor.Parse("#424651"));

            for (int i = 0; i < width; i++)
            {
                paint.Style = SKPaintStyle.Stroke;
                paint.Color = SKColor.Parse("#AF3B6E");

                //if (WaveformHeightMulti <= 0)
                //    WaveformHeightMulti = 1;

                float heightMulti = 40 * WaveformHeightMulti;

                
                float middle = height / 2;

                var start = new SKPoint(i, middle);

               

                float sample = 0;

               
                sample = WaveformData.Span[Math.Min(i, WaveformData.Length-1)];

                var end = new SKPoint(i, middle + sample * heightMulti);
                var end2 = new SKPoint(i, middle - sample * heightMulti);

                canvas.DrawLine(start, end, paint);
                canvas.DrawLine(start, end2, paint);

                paint.Color = new SKColor(1, 0, 0);
                canvas.DrawPoint(end, paint);
                canvas.DrawPoint(end2, paint);
                ////Progress
                //var progress = (Percentage / 100.0f) * width;
                //var progressStart = new SKPoint(progress, -height);
                //var progressEnd = new SKPoint(progress, height);

                //paint.Color = SKColor.Parse("#913d23");
                //canvas.DrawLine(progressStart, progressEnd, paint);


                //paint.Color = SKColors.Blue;

                //var rect = new SKRect((SelectionStartPercentage / 100.0f) * width, 0, (SelectionEndPercentage / 100.0f) * width, height);
                //canvas.DrawRect(rect, paint);

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

        public ReadOnlyMemory<float> WaveformData
        {
            get => (ReadOnlyMemory<float>)GetValue(WaveformDataProperty);
            set => SetValue(WaveformDataProperty, value);
        }
       
        public float Percentage
        {
            get => (float)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }
        public float WaveformHeightMulti
        {
            get => (float)GetValue(WaveformHeightMultiProperty);
            set => SetValue(WaveformHeightMultiProperty, value);
        }
        public float Zoom
        {
            get => (float)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
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

        
        private static void WaveFormRedraw(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WaveformControl control)
            {
                if(e.OldValue != e.NewValue)
                {
                    Debug.WriteLine("Redraw Waveform");
                    control.InvalidateVisual();
                    control.UpdateImage();
                }
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            Debug.WriteLine($"WaveFormControl renderSize Change Width: {sizeInfo.NewSize.Width}");

            if (sizeInfo.NewSize.Width == 0)
                return;

            //ViewModel fetch new Waveform

            var vm = ParentViewModel as GuiViewModel;
            if (vm != null)
            {
                vm.UpdateWaveform(sizeInfo.NewSize.Width);


                surface.Dispose();
                WriteableBitmap = null;
                GC.Collect();

                WriteableBitmap = CreateImage((int)sizeInfo.NewSize.Width, 300);
                width = (int)WriteableBitmap.Width;
                height = (int)WriteableBitmap.Height;

                var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

                surface = SKSurface.Create(info);

                InvalidateVisual();
                UpdateImage();

            }
                base.OnRenderSizeChanged(sizeInfo);
        }

        public static readonly DependencyProperty WaveformHeightMultiProperty =
          DependencyProperty.Register(nameof(WaveformHeightMulti), typeof(float),
          typeof(WaveformControl),
          new FrameworkPropertyMetadata(1.0f, FrameworkPropertyMetadataOptions.AffectsRender, WaveFormRedraw));



        public static readonly DependencyProperty WaveformDataProperty =
             DependencyProperty.Register(nameof(WaveformData), typeof(ReadOnlyMemory<float>),
             typeof(WaveformControl),
             new FrameworkPropertyMetadata(default(ReadOnlyMemory<float>), FrameworkPropertyMetadataOptions.AffectsRender, WaveFormRedraw));

        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register(nameof(Percentage), typeof(float),
            typeof(WaveformControl),
            new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, null));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(float),
            typeof(WaveformControl),
            new FrameworkPropertyMetadata(1.0f, FrameworkPropertyMetadataOptions.AffectsRender, null));

        public static readonly DependencyProperty SelectionStartProperty =
         DependencyProperty.Register(nameof(SelectionStartPercentage), typeof(float),
         typeof(WaveformControl),
         new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, WaveFormRedraw));

        public static readonly DependencyProperty SelectionEndProperty =
         DependencyProperty.Register(nameof(SelectionEndPercentage), typeof(float),
         typeof(WaveformControl),
         new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.AffectsRender, WaveFormRedraw));


        public object ParentViewModel
        {
            get { return (object)GetValue(ParentViewModelProperty); }
            set { SetValue(ParentViewModelProperty, value); }
        }


        public static readonly DependencyProperty ParentViewModelProperty =
    DependencyProperty.Register(nameof(ParentViewModel), typeof(GuiViewModel), typeof(WaveformControl));

    }
}