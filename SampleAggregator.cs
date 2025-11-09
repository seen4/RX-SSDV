using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RX_SSDV
{
    public class SampleAggregator : ISampleProvider
    {
        ISampleProvider source;
        private readonly int channels;
        private int bufferSize;
        private float[] samples;
        private int sampleIndex;

        public int BufferSize => bufferSize;

        public Action<float[]> processSamples = (samples) => { };

        public SampleAggregator(ISampleProvider source, int bufferSize = 1024)
        {
            channels = source.WaveFormat.Channels;
            this.source = source;
            this.bufferSize = bufferSize;
            samples = new float[bufferSize];
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        private void AddSample(float sample)
        {
            samples[sampleIndex] = sample;
            sampleIndex++;

            if(sampleIndex > bufferSize - 1)
            {
                sampleIndex = 0;
                processSamples(samples);
                samples = new float[bufferSize];
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n += channels)
            {
                AddSample(buffer[n + offset]);
            }
            return samplesRead;
        }
    }
}
