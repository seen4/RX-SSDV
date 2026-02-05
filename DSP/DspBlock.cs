using RX_SSDV.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.DSP
{
    public abstract class DspBlock
    {
        public RingBufferIQ historyBuffer;

        public DspBlock()
        {
            SetHistory(SampleSource.WAV_BUFFER_SIZE * 2);
        }

        /// <summary>
        /// Clear the buffer and allocate new one.
        /// </summary>
        /// <param name="bufferSize">New buffer size</param>
        public virtual void SetHistory(int bufferSize)
        {
            historyBuffer = new RingBufferIQ(bufferSize);
        }

        public virtual void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outSamplesI, float[] outSamplesQ, int inputSize)
        {
            if(inputSamplesI.Length != inputSamplesQ.Length)
            {
                throw new ArgumentException("inputSamplesI.Length must equals inputSamplesQ.Length");
            }
            if (outSamplesI.Length != outSamplesQ.Length)
            {
                throw new ArgumentException("outputSamplesI.Length must equals outputSamplesQ.Length");
            }

            historyBuffer.Write(inputSamplesI, inputSamplesQ, inputSize);
        }

        public virtual int Process(int inputSize, float[] inputSamplesI, float[] inputSamplesQ, float[] outputSamplesI, float[] outputSamplesQ)
        {
            Process(inputSamplesI, inputSamplesQ, outputSamplesI, outputSamplesQ, inputSize);
            CompleteProcess(inputSize);
            return outputSamplesI.Length;
        }

        protected void CompleteProcess(int outputSize)
        {
            historyBuffer.MoveOutputIndex(outputSize);
        }
    }
}
