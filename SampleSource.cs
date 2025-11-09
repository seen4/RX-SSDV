using NAudio.Wave;
using NWaves.Effects.Stereo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private static WaveFileReader wavFileReader;
        private static SampleAggregator sampleAggregator;

        private static WaveOutEvent waveOutEvent = new WaveOutEvent();
        private static IWavePlayer playbackDevice;

        private static bool directRead = false;
        private static bool directReadPause = false;

        public const int WAV_BUFFER_SIZE = 10240;

        public static Action<float[]> onDataAvalible = (samples) => { UpdateUI(); };

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
                    float[] buffer;
                    while (directRead)
                    {
                        if (!directReadPause)
                        {
                            buffer = new float[bufferSize];
                            sampleAggregator.Read(buffer, 0, bufferSize);
                            //offset += bufferSize;
                            //onDataAvalible(buffer);

                            //没那么快
                            //Thread.Sleep((int)((float)bufferSize / (float)sampleRate * 1000f));
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
        }

        public static void PauseDirectRead()
        {
            directReadPause = true;
        }

        public static void StopDirectRead()
        {
            directRead = false;
        }

        public static void InitSource()
        {
            wavFileReader = new WaveFileReader(audioFilePath);
            sampleAggregator = new SampleAggregator(wavFileReader.ToSampleProvider(), WAV_BUFFER_SIZE);
            sampleAggregator.processSamples += onDataAvalible;
        }

        public static void PlayAudio()
        {
            bool isPathAvalible = File.Exists(audioFilePath);
            if(!isPathAvalible)
            {
                MessageBox.Show("File not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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
        }

        private static void EnsureDeviceCreated()
        {
            if(playbackDevice == null)
            {
                CreateDevice();
            }
        }

        private static void CreateDevice()
        {
            playbackDevice = new WaveOut { DesiredLatency = 200 };
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
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                MainWindow.Instance.audioProgressSlider.Value = (double)currentTime.TotalSeconds / (double)totalTime.TotalSeconds;
                MainWindow.Instance.audioProgress.Content = $"{minCurrent}:{secCurrent} / {minTotal}:{secTotal}";
                //MainWindow.Instance.audioProgress.Content = $"{currentTime.ToString()} / {totalTime.ToString()}";
            });
        }
    }
}
