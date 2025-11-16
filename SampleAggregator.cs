using NAudio.Wave;
using NWaves.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Numerics;
using System.Threading.Tasks;
using System.Windows;

namespace RX_SSDV
{
    public class SampleAggregator : ISampleProvider
    {
        ISampleProvider source;
        private readonly int channels;
        private int bufferSize;
        private float[] samplesReal;
        private float[] samplesImag;
        private int sampleIndex;

        public int BufferSize => bufferSize;

        public Action<float[], float[]> processSamples = (samplesReal, samplesImag) => { };

        public SampleAggregator(ISampleProvider source, int bufferSize = 1024)
        {
            channels = source.WaveFormat.Channels;
            this.source = source;
            this.bufferSize = bufferSize;
            samplesReal = new float[bufferSize];
            samplesImag = new float[bufferSize];
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        private void AddSample(float sampleReal, float sampleImag)
        {
            samplesReal[sampleIndex] = sampleReal;
            samplesImag[sampleIndex] = sampleImag;
            sampleIndex++;

            if(sampleIndex > bufferSize - 1)
            {
                sampleIndex = 0;
                processSamples(samplesReal, samplesImag);
                ClearBuffer();
            }
        }

        private void ClearBuffer()
        {
            Array.Clear(samplesReal, 0, samplesReal.Length);
            Array.Clear(samplesImag, 0, samplesImag.Length);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n += channels)
            {
                AddSample(buffer[n + offset], buffer[n + 1 + offset]);
            }
            return samplesRead;
        }
    }
}
