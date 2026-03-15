using NAudio.Wave;
using NWaves.Effects.Stereo;
using RX_SSDV.Base;
using RX_SSDV.DSP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RX_SSDV.IO
{
    public class SampleSource
    {
        public enum DataSourceType
        {
            SoundCard,
            BasebandFile,
            RTLSDR
        }

        public static DataSourceType sourceType = DataSourceType.BasebandFile;
        public static bool audioPathEdited = false;
        public static string audioFilePath = "";

        public static int readPeriod = 0;

        public static bool IsSourceAvalible => isSourceAvalible;

        private static WaveFileReader wavFileReader;
        private static SampleAggregator sampleAggregator;

        //private static WaveOutEvent waveOutEvent = new WaveOutEvent();
        public static WasapiLoopbackCapture capture = new WasapiLoopbackCapture();

        private static IWavePlayer playbackDevice;

        private static bool directRead = false;
        private static bool directReadPause = false;

        private static bool isSourceAvalible = false;

        public const int WAV_BUFFER_SIZE = 2048;
        public static RingBufferIQ recordBuffer;

        public static Action<float[], float[]> onDataAvalible = (samplesReal, samplesImag) => { /*UpdateUI();*/ };
        public static Action<WaveFormat> onSourceChange = (waveFmt) => { };

        public static Action onStart = () => { };
        public static Action onPause = () => { };
        public static Action onStop = () => { };

        public static void Play()
        {
            bool isPathAvalible = CheckPathAvalible(true);
            if (!isPathAvalible)
            {
                return;
            }

            isSourceAvalible = true;

            if (sourceType == DataSourceType.BasebandFile)
                ReadSampleDirect();
            else if (sourceType == DataSourceType.SoundCard)
                StartSoundCardRecord();

            Logger.LogInfo("[SampleSource]Playing");
            //PlayAudio();

            onStart();
        }

        public static void Pause()
        {
            if (sourceType == DataSourceType.BasebandFile)
                PauseDirectRead();
            else if (sourceType == DataSourceType.SoundCard)
                PauseSoundCardRecord();

            Logger.LogInfo("[SampleSource]Paused");
            //PauseAudio();

            onPause();
        }

        public static void Stop()
        {
            if (sourceType == DataSourceType.BasebandFile)
                StopDirectRead();

            isSourceAvalible = false;
            Logger.LogInfo("[SampleSource]Stopped");
            //StopAudio();

            onStop();
        }

        public static void StartSoundCardRecord()
        {
            capture.StartRecording();
        }

        public static void PauseSoundCardRecord()
        {
            capture.StopRecording();
        }

        //read from baseband file
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
                            Thread.Sleep(readPeriod);
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

        //pause
        public static void PauseDirectRead()
        {
            directReadPause = true;
        }

        //stop
        public static void StopDirectRead()
        {
            directRead = false;
            directReadPause = false;
        }

        //Init sample sources
        public static void InitSource()
        {
            wavFileReader = new WaveFileReader(audioFilePath);
            sampleAggregator = new SampleAggregator(wavFileReader.ToSampleProvider(), WAV_BUFFER_SIZE);
            sampleAggregator.processSamples += onDataAvalible;
            
            //capture.DataAvailable 
        }

        public static void ProcessCaptureSamples(float[] inputSamples)
        {
            //recordBuffer.Write(inputSamples, );
        }

        //check output device
        private static void EnsureDeviceCreated()
        {
            if (playbackDevice == null)
            {
                CreateDevice();
            }
        }

        //create output device
        private static void CreateDevice()
        {
            playbackDevice = new WaveOut { DesiredLatency = 200 };
        }

        //check file path
        public static bool CheckPathAvalible(bool showMessage = false)
        {
            bool isPathAvalible = File.Exists(audioFilePath);
            if (!isPathAvalible && showMessage)
            {
                MessageBox.Show("File not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return isPathAvalible;
        }

        //play with audio
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

        //pause
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

        //stop
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

        /// <summary>
        /// Update player UI
        /// </summary>
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
