using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace RX_SSDV
{
    public class CanvasGraphicDrawer
    {
        Canvas imageCanvas;
        Image drawTarget;
        private int width;
        private int height;

        public int Width => width;
        public int Height => height;

        public Action<int, int> onSizeChange = (w, h) => { };

        public CanvasGraphicDrawer(int width, int height, Canvas imageCanvas, Image drawTarget)
        {
            this.drawTarget = drawTarget;
            this.imageCanvas = imageCanvas;

            this.width = width;
            this.height = height;

            InitGraphics();
            MainWindow.Instance.onSizeChange += OnSizeChange;
        }

        public Bitmap? backBitmap;
        public Graphics? graphics;
        private WriteableBitmap drawable;
        public WriteableBitmap Drawable => drawable;

        public void InitGraphics()
        {
            drawable = new WriteableBitmap(width, height, 72, 72, PixelFormats.Bgr24, null);
            drawTarget.Source = drawable;
            backBitmap = new Bitmap(width, height, drawable.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, drawable.BackBuffer);
            graphics = Graphics.FromImage(backBitmap);
        }

        public void OnSizeChange()
        {
            width = (int)(imageCanvas.ActualWidth);
            height = (int)(imageCanvas.ActualHeight);

            drawTarget.Width = width;
            drawTarget.Height = height;

            onSizeChange(width, height);

            if(width > 0 && height > 0)
                InitGraphics();
        }

        public void Draw(Action<Graphics> drawAction)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                {
                    drawable.Lock();
                    graphics.Clear(System.Drawing.Color.White);
                    try
                    {
                        drawAction(graphics);
                    }
                    catch(ArgumentException)
                    {
                        //Usually that means no audio input.
                    }
                    //graphics.Flush();
                    //graphics.Dispose();
                    //backBitmap.Dispose();

                    drawable.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    drawable.Unlock();
                }
            });
        }

        public void SetImage(WriteableBitmap bitmap)
        {
            drawTarget.Source = bitmap;
        }
    }
}
