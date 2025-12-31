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
using RX_SSDV.Utils;

namespace RX_SSDV
{
    public class MainDSP
    {
        /*Base*/
        private static int sampleRate = 48000;

        /*Drawing*/
        private CanvasGraphicDrawer spectrum;
        private CanvasGraphicDrawer constellation;
        private Point[] points;
        private Point[] pointsOfFilterI;
        private Point[] pointsOfFilterQ;
        private Point[] pointsOfconstellation;
        private Font font = new Font("Arial", 8);
        private SolidBrush brush = new SolidBrush(Color.Black);
        private SolidBrush bfBrush = new SolidBrush(Color.FromArgb(120, 0, 255, 255));

        private bool isDrawerOnline = false;
        private bool drawerUpdateFlag = false;
        public float[] spectrumTempSamplesI;
        public float[] spectrumTempSamplesQ;
        public bool drawerTempLock = false;
        private int currentDrawerTick = 0;

        /*FFT Spectrum*/
        public Fft fft;
        public const int FFT_SIZE = 2048;
        //public const int FFT_MAX = 120;
        //public const int FFT_MIN = 0;
        public const int FFT_POS = 100;
        public const int FFT_RANGE = -1;//2048
        public int spectrumPeriod = 5;
        public int fftDatasetIndex = 0;
        public double[][] fftDataset;
        public Bitmap spectrumCacheBitmap;
        private float freqPerSample = 0;
        private float[] fftReal;
        private float[] fftImag;
        double[] magnitudeSpectrum;

        /*Filter*/
        public bool EnableFilter
        {
            get
            {
                return enableFilter;
            }
            set
            {
                enableFilter = value;
                if (value)
                {
                    UpdateFilter();
                }
            }
        }
        private bool enableFilter = false;
        public int bandwidth = 1;
        public int frequencyShift = 0;
        public ComplexFirFilter bpFilter;
        public const int BP_ORDER = 130;
        private float[] filteredSamplesI;
        private float[] filteredSamplesQ;

        /*SSDV Process*/
        public static int symobolRate = 9600;
        private static int samplesPerSymbol = 5;
        public static int SamplePerSymbol
        {
            get
            {
                return GetSPS();
            }
        }

        public bool EnableProcess
        {
            get
            {
                return enableProcess;
            }
            set
            {
                enableProcess = value;
            }
        }
        private bool enableProcess = false;
        /*BPSK Demod*/
        //public CostasLoop costasLoop;
        public BPSKDemod bpskDemod;
        private float[] demodOutputI;
        private float[] demodOutputQ;
        public float ConstellationMultiply
        {
            get
            {
                return constellationMultiply;
            }
            set
            {
                constellationMultiply = value;
            }
        }
        private int constellationStepsize = 10;
        private float constellationMultiply = 100;

        public MainDSP(CanvasGraphicDrawer spectrumArea, CanvasGraphicDrawer constellationArea)
        {
            spectrum = spectrumArea;
            constellation = constellationArea;

            //int spectrumLength = SampleSource.WAV_BUFFER_SIZE;

            Init();
        }

        private void Init()
        {
            fft = new Fft(FFT_SIZE);
            //bpskDemod = new BPSKDemod(
            //    0.005f, 10, 
            //    0.001f, 2, 2,
            //    5, 0.007f, 5, 0.01f, 0.05f, 5, 11 * 5 * SampleSource.WaveFormat.SampleRate);
            bpskDemod = new BPSKDemod();
            bpskDemod.InitClockSync(5, 0.01f, 5, 0.01f, 0.05f, 5, 11 * 5 * samplesPerSymbol);

            UpdateBitmap(spectrum.Width);

            SampleSource.onDataAvalible += ProcessData;
            SampleSource.onSourceChange += OnSourceChange;
            spectrum.onSizeChange += (w, h) => { UpdateBitmap(w); };

            StartSpectrum();
        }
        
        public static int GetSPS()
        {
            int sps = sampleRate / symobolRate;
            samplesPerSymbol = sps;
            return sps;
        }

        public void OnSourceChange(WaveFormat waveFormat)
        {
            freqPerSample = waveFormat.SampleRate / FFT_SIZE;
            sampleRate = waveFormat.SampleRate;
            bpskDemod.clockRecovery.UpdatePFB(5, 11 * 5 * GetSPS());
            //bpskDemod.equalizer.SamplesPerSymbol = samplesPerSymbol;
        }

        private void UpdateBitmap(int width)
        {
            spectrumCacheBitmap = new Bitmap(width, 1);
            fftDataset = new double[FFT_POS][];
            for (int i = 0; i < fftDataset.Length; i++)
            {
                fftDataset[i] = new double[SampleSource.WAV_BUFFER_SIZE];
            }
        }

        public void UpdateFilter()
        {
            if (!enableFilter)
                return;

            if (SampleSource.IsSourceAvalible)
            {
                try
                {
                    //Calc kernel
                    double lowCutoff = (frequencyShift - bandwidth / 2.0) * 1000 / sampleRate;
                    double highCutoff = (frequencyShift + bandwidth / 2.0) * 1000 / sampleRate;
                    float[] bpKernelReal = ArrayUtil.Double2Float(DesignFilterUtil.FirWinBpReal(BP_ORDER, lowCutoff, highCutoff));
                    float[] bpKernelImag = ArrayUtil.Double2Float(DesignFilterUtil.FirWinBpImag(BP_ORDER, lowCutoff, highCutoff));

                    //Apply the kernel
                    if (bpFilter == null)
                    {
                        bpFilter = new ComplexFirFilter(bpKernelReal, bpKernelImag);
                    }
                    else
                    {
                        bpFilter.ChangeKernel(bpKernelReal, bpKernelImag);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unexpected filter arguments.", "Oh no!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void StartSpectrum()
        {
            if(!isDrawerOnline)
            {
                isDrawerOnline = true;

                Task.Run(() =>
                {
                    while(isDrawerOnline)
                    {
                        if (currentDrawerTick < spectrumPeriod)
                        {
                            currentDrawerTick++;
                            Thread.Sleep(10);
                        }
                        else if (currentDrawerTick >= spectrumPeriod && !drawerTempLock && drawerUpdateFlag)
                        {
                            drawerUpdateFlag = false;
                            currentDrawerTick = 0;
                            ProcessSpectrum(spectrumTempSamplesI, spectrumTempSamplesQ);
                            UpdateConstellation(demodOutputI, demodOutputQ);
                        }
                    }
                });
            }
        }

        public void ProcessData(float[] samplesReal, float[] samplesImag)
        {
            filteredSamplesI = samplesReal;
            filteredSamplesQ = samplesImag;
            if(enableFilter)
                ProcessFilter(samplesReal, samplesImag);

            drawerTempLock = true;
            if (enableProcess)
                ProcessBPSK(filteredSamplesI, filteredSamplesQ);

            if (spectrumTempSamplesI == null || spectrumTempSamplesQ == null)
            {
                spectrumTempSamplesI = new float[samplesReal.Length];
                spectrumTempSamplesQ = new float[samplesImag.Length];
            }
            samplesReal.FastCopyTo(spectrumTempSamplesI, samplesReal.Length);
            samplesImag.FastCopyTo(spectrumTempSamplesQ, samplesImag.Length);
            drawerTempLock = false;
            drawerUpdateFlag = true;

            //currentSpectrumTick++;
            //if (currentSpectrumTick >= spectrumPeriod)
            //{
            //    currentSpectrumTick = 0;
            //    ProcessSpectrum(samplesReal, samplesImag);
            //}
        }

        public void ProcessBPSK(float[] realSignal, float[] imagSignal)
        {
            //float[] outRealSignal = new float[realSignal.Length];
            //float[] outImagSignal = new float[imagSignal.Length];
            //costasLoop.Process(realSignal, imagSignal, outRealSignal, outImagSignal);

            CheckBPSKOutputAvalible(realSignal.Length);

            bpskDemod.Process(realSignal, imagSignal, demodOutputI, demodOutputQ);

            //UpdateConstellation(demodOutputI, demodOutputQ);
        }

        public void ProcessFilter(float[] realSignal, float[] imagSignal)
        {
            if(ArrayUtil.CheckNeedUpdate(filteredSamplesI, realSignal.Length))
                filteredSamplesI = new float[realSignal.Length];
            if(ArrayUtil.CheckNeedUpdate(filteredSamplesQ, imagSignal.Length))
                filteredSamplesQ = new float[imagSignal.Length];

            if (bpFilter != null)
            {
                bpFilter.ProcessOnline(realSignal, imagSignal, filteredSamplesI, filteredSamplesQ);

                //Drawing
                pointsOfFilterI = filteredSamplesI
                    .Select((v, i) => new Point((int)(i * (spectrum.Width * 1f / filteredSamplesI.Length)), spectrum.Height - (int)(v * 100) - FFT_POS - 300))
                    .ToArray();

                pointsOfFilterQ = filteredSamplesQ
                    .Select((v, i) => new Point((int)(i * (spectrum.Width * 1f / filteredSamplesQ.Length)), spectrum.Height - (int)(v * 100) - FFT_POS - 400))
                    .ToArray();

                //graphics.DrawLines(Pens.Black, pointsOfFilter);
            }
        }

        public void ProcessSpectrum(float[] realSignal, float[] imagSignal)
        {
            //FFT
            if ((realSignal.Length > 0 && realSignal.Length >= FFT_SIZE) && (realSignal.Length == imagSignal.Length))
            {
                float maxFreq = realSignal.Length * freqPerSample;

                if (ArrayUtil.CheckNeedUpdate(fftReal, realSignal.Length))
                {
                    fftReal = new float[realSignal.Length];
                    fftImag = new float[imagSignal.Length];
                }
                fft.Direct(realSignal, imagSignal, fftReal, fftImag);

                double[] tempSpectrum = fftReal
                    .Select((v, i) => Math.Sqrt(v * v + fftImag[i] * fftImag[i]))
                    .ToArray();

                //must new object, or use object pool
                if(ArrayUtil.CheckNeedUpdate(magnitudeSpectrum, realSignal.Length))
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

        public void CheckBPSKOutputAvalible(int inputSize)
        {
            //int demodOutputSize = bpskDemod.CalcOutputSize(inputSize);
            int demodOutputSize = 2048;

            if(ArrayUtil.CheckNeedUpdate(demodOutputI, demodOutputSize) || ArrayUtil.CheckNeedUpdate(demodOutputQ, demodOutputSize))
            {
                demodOutputI = new float[demodOutputSize];
                demodOutputQ = new float[demodOutputSize];
            }
        }

        private void UpdateConstellation(float[] samplesReal, float[] samplesImag)
        {
            if (samplesReal.Length != samplesImag.Length)
                return;

            //int sizeOfPointArr = (int)(1f * samplesReal.Length / constellationStepsize) + 1;
            //if (pointsOfconstellation == null || pointsOfconstellation.Length != sizeOfPointArr)
            //{
            //    pointsOfconstellation = new Point[sizeOfPointArr];
            //}
            //for(int i = 0, j = 0; i < samplesReal.Length; i+=constellationStepsize, j++)
            //{
            //    if (i >= samplesReal.Length)
            //        continue;
            //    pointsOfconstellation[j] = new Point((int)(samplesReal[i] * constellationMultiply), (int)(samplesImag[i] * constellationMultiply));
            //}

            constellation.Draw((graphics) =>
            {
                for (int i = 0; i < samplesReal.Length; i += constellationStepsize)
                {
                    int x = (int)(samplesReal[i] * constellationMultiply) + 50;
                    int y = (int)(samplesImag[i] * constellationMultiply) + 50;
                    graphics.DrawRectangle(Pens.Green, x, y, 1, 1);
                }
            });
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

            if (fftDatasetIndex >= FFT_POS)
            {
                fftDatasetIndex = 0;
            }

            //fftDataset.Add(actualProcess);
            actualProcess.FastCopyTo(fftDataset[fftDatasetIndex], actualProcess.Length);
            int readerIndex = fftDatasetIndex;
            fftDatasetIndex++;

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
                //不要担心这里编译器会帮你优化成StringBuilder的
                graphics.DrawString(
                    $"FFT[{FFT_SIZE}](Visualizing index range: {(FFT_RANGE < 0 ? "Unlimited" : " 0 - " + FFT_RANGE)} Freq: -{maxFreq / 2000}kHz - {maxFreq / 2000}kHz )" +
                    $"\nInput Signal[Real {lengthReal}, Imag {lengthImag}]" +
                    $"\nOutput FFT[{magnitudeSpectrum.Length}]" +
                    $"\nBandwidth: {bandwidth}kHz, Frequency Shift: {frequencyShift}kHz" +
                    $"\nTime {SampleSource.GetFormatedTimeString()}" +
                    $"\nCostas Loop [freq = {bpskDemod.costasLoop.Phase}, phase = {bpskDemod.costasLoop.Phase}]",
                    font, brush, new Point(5, 5));

                //Separator
                graphics.DrawLine(Pens.Black, new Point(0, spectrum.Height - FFT_POS), new Point(spectrum.Width, spectrum.Height - FFT_POS));

                //Waterfall
                for (int i = 0, j = readerIndex; i < fftDataset.Length; i++, j--)
                {
                    if(j < 0)
                    {
                        j = fftDataset.Length - 1;
                    }
                    //double[] data = fftDataset[^(i + 1)];
                    double[] data = fftDataset[j];
                    int lastX = -1;
                    for (int k = 0; k < data.Length; k++)
                    {
                        int x = (int)(k * pixelsPerSample);
                        if (x != lastX)
                        {
                            if (x > lastX + 1)
                            {
                                x = lastX + 1;
                            }
                            int value = (int)Math.Clamp(data[k] * 20, 0, 255);
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

                //Debug
                if(pointsOfFilterI != null)
                {
                    graphics.DrawLines(Pens.Black, pointsOfFilterI);
                }
                if (pointsOfFilterQ != null)
                {
                    graphics.DrawLines(Pens.Black, pointsOfFilterQ);
                }
            });
        }
    }
}
