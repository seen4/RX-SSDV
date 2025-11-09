using NWaves.Transforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;

namespace RX_SSDV
{
    public class MainDSP
    {
        /*Drawing*/
        private CanvasGraphicDrawer spectrum;
        private Point[] points;
        private Font font = new Font("Arial", 8);
        private SolidBrush brush = new SolidBrush(Color.Black);

        /*FFT Spectrum*/
        public RealFft rFft;
        float[] fftRe;
        float[] fftIm;
        public const int FFT_SIZE = 1024;
        public const int FFT_POS = 100;
        public const int FFT_RANGE = 550;
        public const float FFT_SAMPLE2FREQ = 23.4f;
        public List<double[]> fftDataset = new List<double[]>();
        public Bitmap spectrumCacheBitmap;

        public MainDSP(CanvasGraphicDrawer spectrumArea)
        {
            spectrum = spectrumArea;
            Init();
        }

        private void Init()
        {
            rFft = new RealFft(FFT_SIZE);
            spectrumCacheBitmap = new Bitmap(550, 1);

            SampleSource.onDataAvalible += ProcessData;
        }

        public void ProcessData(float[] samples)
        {
            ProcessSpectrum(samples);
        }

        public void ProcessSpectrum(float[] inputSignal)
        {
            if (inputSignal.Length > 0 && inputSignal.Length > FFT_SIZE)
            {
                fftIm = new float[FFT_SIZE];
                fftRe = new float[FFT_SIZE];
                rFft.Direct(inputSignal, fftRe, fftIm);
                double[] result = fftIm.Select((v, i) => Math.Sqrt(v * v + fftRe[i] * fftRe[i])).ToArray();
                //fft.Direct(inputSignal[0..Math.Min(FFT_SIZE, inputSignal.Length)], fftIm);
                //double[] result = fftIm.Select((v, i) => Math.Sqrt(v * v + inputSignal[i] * inputSignal[i])).ToArray();
                double[] actualProcess = result[0..FFT_RANGE];
                if (fftDataset.Count - 1 >= FFT_POS)
                {
                    fftDataset.RemoveAt(0);
                }
                fftDataset.Add(actualProcess);

                points = actualProcess
                    .Select((v, i) => new Point((int)(i * ((float)spectrum.Width / actualProcess.Length)), spectrum.Height - (int)(v * 2) - FFT_POS))
                    .ToArray();   // 将数据转换为一个个的坐标点

                spectrum.Draw((graphics) =>
                {
                    graphics.DrawLines(Pens.Black, points);   // 连接这些点, 画线

                    //GUI
                    graphics.DrawString($"RealFFT[{FFT_SIZE}](Visualizing index range: 0 - 550)\nInput Signal[{inputSignal.Length}]\nOutput RealFFT[{result.Length}]", font, brush, new Point(5, 5));
                    graphics.DrawLine(Pens.Black, new Point(0, spectrum.Height - FFT_POS), new Point(spectrum.Width, spectrum.Height - FFT_POS));
                    for (int i = 0; i < fftDataset.Count; i++)
                    {
                        double[] data = fftDataset[^(i + 1)];
                        for (int j = 0; j < data.Length; j++)
                        {
                            int value = (int)Math.Clamp(data[j] * 5, 0, 255);
                            spectrumCacheBitmap.SetPixel(j, 0, Color.FromArgb(value, 0, 255 / 4));
                        }
                        graphics.DrawImage(spectrumCacheBitmap, 0, spectrum.Height - (FFT_POS - 20) + i, spectrum.Width, 1);
                    }

                    for (int i = 0; i < actualProcess.Length; i += 100)
                    {
                        if (i < actualProcess.Length - 1)
                        {
                            int xPos = (int)(i * ((float)spectrum.Width / actualProcess.Length));
                            graphics.DrawLine(Pens.Black, new Point(xPos, spectrum.Height - FFT_POS + 5), new Point(xPos, spectrum.Height - FFT_POS));
                            graphics.DrawString($"{i * FFT_SAMPLE2FREQ}Hz", font, brush, new Point(xPos, spectrum.Height - FFT_POS + 5));
                        }
                    }
                });
            }
        }
    }
}
