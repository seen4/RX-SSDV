using RX_SSDV.DSP;
using RX_SSDV.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.CCSDS
{
    //DspBlock but no IQ ^-^
    public class DigitalProcessingBlock
    {
        public RingBuffer historyBuffer;

        public DigitalProcessingBlock()
        {
            SetHistory(SampleSource.WAV_BUFFER_SIZE * 2);
        }

        /// <summary>
        /// Clear the buffer and allocate new one.
        /// </summary>
        /// <param name="bufferSize">New buffer size</param>
        public virtual void SetHistory(int bufferSize)
        {
            historyBuffer = new RingBuffer(bufferSize);
        }

        public virtual void Process(float[] inputArr,  float[] outArr, int inputSize)
        {
            historyBuffer.Write(inputArr, inputSize);
        }

        public virtual int Process(int inputSize, float[] inputArr, float[] outputArr)
        {
            Process(inputArr, outputArr, inputSize);
            CompleteProcess(inputSize);
            return outputArr.Length;
        }

        protected void CompleteProcess(int outputSize)
        {
            historyBuffer.MoveOutputIndex(outputSize);
        }
    }
}
