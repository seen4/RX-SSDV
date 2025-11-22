using NAudio.Wave;
using NWaves.Effects.Stereo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RX_SSDV
{
    public class SampleSource
    {
        public enum DataSourceType
        {
            BasebandFile,
            RTLSDR
        }

        public static DataSourceType sourceType = DataSourceType.BasebandFile;
        public static bool audioPathEdited = false;
        public static string audioFilePath = "";
        public static bool IsSourceAvalible => isSourceAvalible;

        private static WaveFileReader wavFileReader;
        private static SampleAggregator sampleAggregator;

        //private static WaveOutEvent waveOutEvent = new WaveOutEvent();
        private static IWavePlayer playbackDevice;

        private static bool directRead = false;
        private static bool directReadPause = false;

        private static bool isSourceAvalible = false;

        public const int WAV_BUFFER_SIZE = 2048;

        public static Action<float[], float[]> onDataAvalible = (samplesReal, samplesImag) => { /*UpdateUI();*/ };
        public static Action<WaveFormat> onSourceChange = (waveFmt) => { };

        public static void Play()
        {
            bool isPathAvalible = CheckPathAvalible(true);
            if (!isPathAvalible)
            {
                return;
            }

            isSourceAvalible = true;

            ReadSampleDirect();
            //PlayAudio();
        }

        public static void Pause()
        {
            PauseDirectRead();
            //PauseAudio();
        }

        public static void Stop()
        {
            StopDirectRead();
            isSourceAvalible = false;
            //StopAudio();
        }

        public static void ReadSampleDirect()
        {
            if (!directRead)
            {
                directRead = true;
                InitSource();

                int offset = 0;
                int sampleRate = sampleAggregator.WaveFormat.SampleRate;
                int bufferSize = sampleAggregator.BufferSize;

                Task.Run(() =>
                {
                    float[] buffer = new float[bufferSize];
                    while (directRead)
                    {
                        if (!directReadPause)
                        {
                            Array.Clear(buffer, 0, bufferSize);
                            sampleAggregator.Read(buffer, 0, bufferSize);
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                });
            }
            else
            {
                directReadPause = false;
            }

            onSourceChange(sampleAggregator.WaveFormat);
        }

        public static void PauseDirectRead()
        {
            directReadPause = true;
        }

        public static void StopDirectRead()
        {
            directRead = false;
            directReadPause = false;
        }

        public static void InitSource()
        {
            wavFileReader = new WaveFileReader(audioFilePath);
            sampleAggregator = new SampleAggregator(wavFileReader.ToSampleProvider(), WAV_BUFFER_SIZE);
            sampleAggregator.processSamples += onDataAvalible;
        }

        private static void EnsureDeviceCreated()
        {
            if (playbackDevice == null)
            {
                CreateDevice();
            }
        }

        private static void CreateDevice()
        {
            playbackDevice = new WaveOut { DesiredLatency = 200 };
        }

        public static bool CheckPathAvalible(bool showMessage = false)
        {
            bool isPathAvalible = File.Exists(audioFilePath);
            if (!isPathAvalible && showMessage)
            {
                MessageBox.Show("File not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return isPathAvalible;
        }

        public static void PlayAudio()
        {
            EnsureDeviceCreated();

            if (audioPathEdited)
            {
                InitSource();
                audioPathEdited = false;
                playbackDevice.Init(sampleAggregator);
                playbackDevice.Play();
            }
            else
            {
                playbackDevice.Play();
            }

            onSourceChange(sampleAggregator.WaveFormat);
        }



        public static void PauseAudio()
        {
            try
            {
                playbackDevice.Pause();
            }
            catch(Exception)
            {
                MessageBox.Show("No audio loaded.");
            }
        }

        public static void StopAudio()
        {
            try
            {
                playbackDevice.Dispose();
                audioPathEdited = true; //if user click 'play' button again, load audio file.
            }
            catch(Exception)
            {
                MessageBox.Show("No audio loaded");
            }
        }

        public static void UpdateUI()
        {
            TimeSpan totalTime = wavFileReader.TotalTime;
            int minTotal = totalTime.Minutes;
            int secTotal = totalTime.Seconds;
            TimeSpan currentTime = wavFileReader.CurrentTime;
            int minCurrent = currentTime.Minutes;
            int secCurrent = currentTime.Seconds;

            double valueOfSlider = (double)currentTime.TotalSeconds / (double)totalTime.TotalSeconds;
            string timeStr = $"{minCurrent}:{secCurrent} / {minTotal}:{secTotal}";
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                //MainWindow.Instance.audioProgressSlider.Value = valueOfSlider;
                //MainWindow.Instance.audioProgress.Content = timeStr;
                //MainWindow.Instance.audioProgress.Content = $"{currentTime.ToString()} / {totalTime.ToString()}";
            });
        }

        public static string GetFormatedTimeString()
        {
            TimeSpan totalTime = wavFileReader.TotalTime;
            int minTotal = totalTime.Minutes;
            int secTotal = totalTime.Seconds;
            TimeSpan currentTime = wavFileReader.CurrentTime;
            int minCurrent = currentTime.Minutes;
            int secCurrent = currentTime.Seconds;

            string timeStr = $"{minCurrent}:{secCurrent} / {minTotal}:{secTotal}";
            return timeStr;
        }
    }
}
