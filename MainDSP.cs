using NAudio.Wave;
using NWaves.Filters.Base;
using NWaves.Signals;
using NWaves.Filters.Fda;
using NWaves.Transforms;
using NWaves.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;

namespace RX_SSDV
{
    public class MainDSP
    {
        /*Base*/
        private int sampleRate = 1000;

        /*Drawing*/
        private CanvasGraphicDrawer spectrum;
        private Point[] points;
        private Point[] pointsOfFilter;
        private Font font = new Font("Arial", 8);
        private SolidBrush brush = new SolidBrush(Color.Black);
        private SolidBrush bfBrush = new SolidBrush(Color.FromArgb(120, 0, 255, 255));

        /*FFT Spectrum*/
        public Fft fft;
        //public Fft fft;
        public const int FFT_SIZE = 2048;
        //public const int FFT_MAX = 120;
        //public const int FFT_MIN = 0;
        public const int FFT_POS = 100;
        public const int FFT_RANGE = -1;//2048
        public const int SPECTRUM_UPDATE_RATE = 50;
        public List<double[]> fftDataset = new List<double[]>();
        public Bitmap spectrumCacheBitmap;
        private float freqPerSample = 0;
        private int currentSpectrumTick = 0;
        double[] magnitudeSpectrum;

        /*Filter*/
        public int bandwidth = 1;
        public int frequencyShift = 0;
        public FirFilter bandPassFilter;

        public MainDSP(CanvasGraphicDrawer spectrumArea)
        {
            spectrum = spectrumArea;

            int spectrumLength = SampleSource.WAV_BUFFER_SIZE;

            Init();
        }

        private void Init()
        {
            //rFft = new RealFft(FFT_SIZE);
            fft = new Fft(FFT_SIZE);
            //UpdateFilter();
            UpdateBitmap(spectrum.Width);

            SampleSource.onDataAvalible += ProcessData;
            SampleSource.onSourceChange += OnSourceChange;
            spectrum.onSizeChange += (w, h) => { UpdateBitmap(w); };
        }

        private void UpdateBitmap(int width)
        {
            spectrumCacheBitmap = new Bitmap(width, 1);
        }

        public void UpdateFilter()
        {
            if (frequencyShift - bandwidth / 2f > 0 && SampleSource.IsSourceAvalible)
            {
                try
                {
                    double lowCutoff = (frequencyShift - bandwidth / 2.0) * 1000 / sampleRate;
                    double highCutoff = (frequencyShift + bandwidth / 2.0) * 1000 / sampleRate;
                    double[] bpKernel = DesignFilter.FirWinBp(237, lowCutoff, highCutoff);
                    if (bandPassFilter == null)
                    {
                        bandPassFilter = new FirFilter(bpKernel);
                    }
                    else
                    {
                        //Well... Is this necessary?
                        float[] bpFloatKernel = new float[bpKernel.Length];
                        for (int i = 0; i < bpKernel.Length; i++)
                        {
                            bpFloatKernel[i] = (float)bpKernel[i];
                        }
                        bandPassFilter.ChangeKernel(bpFloatKernel);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unexpected bad filter arguments.", "Oh no!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public void ProcessData(float[] samplesReal, float[] samplesImag)
        {
            ProcessSpectrum(samplesReal, samplesImag);
            ProcessFilter(samplesReal, samplesImag);
        }

        public void OnSourceChange(WaveFormat waveFormat)
        {
            freqPerSample = waveFormat.SampleRate / FFT_SIZE;
            sampleRate = waveFormat.SampleRate;
        }

        public void ProcessFilter(float[] realSignal, float[] imagSignal)
        {
            DiscreteSignal filterInputSignal;
            DiscreteSignal filteredSignal = null;

            if (bandPassFilter != null)
            {
                filterInputSignal = new DiscreteSignal(sampleRate, realSignal);
                filteredSignal = bandPassFilter.FilterOnline(filterInputSignal);
            }

            //Drawing
            //if (filteredSignal != null)
            //{
            //    pointsOfFilter = filteredSignal.Samples
            //        .Select((v, i) => new Point((int)(i * (spectrum.Width * 1f / filteredSignal.Samples.Length)), spectrum.Height - (int)(v * 100) - FFT_POS - 300))
            //        .ToArray();
            //}

            //if (filteredSignal != null)
            //{
            //    graphics.DrawLines(Pens.Black, pointsOfFilter);
            //}
        }

        public void ProcessSpectrum(float[] realSignal, float[] imagSignal)
        {
            currentSpectrumTick++;

            if (currentSpectrumTick >= SPECTRUM_UPDATE_RATE)
            {
                //FFT
                currentSpectrumTick = 0;
                if ((realSignal.Length > 0 && realSignal.Length >= FFT_SIZE) && (realSignal.Length == imagSignal.Length))
                {
                    float maxFreq = realSignal.Length * freqPerSample;

                    fft.Direct(realSignal, imagSignal);

                    double[] tempSpectrum = realSignal
                        .Select((v, i) => Math.Sqrt(v * v + imagSignal[i] * imagSignal[i]))
                        .ToArray();

                    magnitudeSpectrum = new double[realSignal.Length];

                    for (int i = 0, j = tempSpectrum.Length / 2; i < magnitudeSpectrum.Length; i++, j++)
                    {
                        if (j > tempSpectrum.Length - 1)
                        {
                            j = 0;
                        }

                        magnitudeSpectrum[i] = tempSpectrum[j];
                    }

                    UpdateSpectrum(maxFreq, realSignal.Length, imagSignal.Length);
                }
            }
        }

        private void UpdateSpectrum(float maxFreq, int lengthReal, int lengthImag)
        {
            double[] actualProcess;

            //FFT_RANGE < 0 means no range select.(Debug)
            if (FFT_RANGE > 0)
            {
                actualProcess = magnitudeSpectrum[0..FFT_RANGE];
            }
            else
            {
                actualProcess = magnitudeSpectrum;
            }

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
                float samplesPerKHz = (1000 / freqPerSample);
                float pixelsPerSample = ((float)spectrum.Width / actualProcess.Length);
                float scaleCoeff = samplesPerKHz * pixelsPerSample;
                int centerPos = spectrum.Width / 2;

                //Spectrum
                graphics.DrawLines(Pens.Black, points);   // 连接这些点, 画线

                //Band-pass filter
                int bpMin = frequencyShift - bandwidth / 2;
                int bpMax = frequencyShift + bandwidth / 2;

                graphics.DrawLine(Pens.LightCyan, new Point(centerPos + (int)(bpMin * scaleCoeff), 0), new Point(centerPos + (int)(bpMin * scaleCoeff), spectrum.Height - FFT_POS));
                graphics.DrawLine(Pens.LightCyan, new Point(centerPos + (int)(bpMax * scaleCoeff), 0), new Point(centerPos + (int)(bpMax * scaleCoeff), spectrum.Height - FFT_POS));
                graphics.FillRectangle(bfBrush, centerPos + (int)(bpMin * scaleCoeff), 0, (bpMax - bpMin) * scaleCoeff, spectrum.Height - FFT_POS);

                //Analyze
                graphics.DrawString(
                    $"FFT[{FFT_SIZE}](Visualizing index range: {(FFT_RANGE < 0 ? "Unlimited" : " 0 - " + FFT_RANGE)} Freq: -{maxFreq / 2000}kHz - {maxFreq / 2000}kHz )" +
                    $"\nInput Signal[Real {lengthReal}, Imag {lengthImag}]" +
                    $"\nOutput FFT[{magnitudeSpectrum.Length}]" +
                    $"\nBandwidth: {bandwidth}kHz, Frequency Shift: {frequencyShift}kHz" +
                    $"\nTime {SampleSource.GetFormatedTimeString()}",
                    font, brush, new Point(5, 5));

                //Separator
                graphics.DrawLine(Pens.Black, new Point(0, spectrum.Height - FFT_POS), new Point(spectrum.Width, spectrum.Height - FFT_POS));

                //Waterfall
                for (int i = 0; i < fftDataset.Count; i++)
                {
                    double[] data = fftDataset[^(i + 1)];
                    int lastX = -1;
                    for (int j = 0; j < data.Length; j++)
                    {
                        int x = (int)(j * pixelsPerSample);
                        if (x != lastX)
                        {
                            if (x > lastX + 1)
                            {
                                x = lastX + 1;
                            }
                            int value = (int)Math.Clamp(data[j] * 20, 0, 255);
                            spectrumCacheBitmap.SetPixel(x, 0, Color.FromArgb(value, 0, 255 / 4));
                            lastX = x;
                        }
                    }
                    graphics.DrawImage(spectrumCacheBitmap, 0, spectrum.Height - (FFT_POS - 20) + i, spectrum.Width, 1);
                }

                //Scale
                for (int i = 0; i < actualProcess.Length / 2; i += 100)
                {
                    if (i < actualProcess.Length - 1)
                    {
                        int xPos = (int)(i * ((float)spectrum.Width / actualProcess.Length));

                        graphics.DrawLine(Pens.Black, new Point(centerPos + xPos, spectrum.Height - FFT_POS + 5), new Point(centerPos + xPos, spectrum.Height - FFT_POS));
                        graphics.DrawString($"{(i * freqPerSample) / 1000}kHz", font, brush, new Point(centerPos + xPos, spectrum.Height - FFT_POS + 5));

                        if (i != 0)
                        {
                            graphics.DrawLine(Pens.Black, new Point(centerPos - xPos, spectrum.Height - FFT_POS + 5), new Point(centerPos - xPos, spectrum.Height - FFT_POS));
                            graphics.DrawString($"-{(i * freqPerSample) / 1000}kHz", font, brush, new Point(centerPos - xPos, spectrum.Height - FFT_POS + 5));
                        }
                    }
                }
            });
        }
    }
}
