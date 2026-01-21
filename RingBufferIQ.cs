using NWaves.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Utils
{
    public class RingBufferIQ
    {
        private float[] bufferI;
        private float[] bufferQ;

        private int inputIndex = 0;
        private int outputIndex = 0;
        private int availableDataCount = 0;
        public int AvailableDataCount => availableDataCount;

        private int bufferSize = 0;
        public int Size => bufferSize;

        public RingBufferIQ(int size)
        {
            bufferSize = size;
            bufferI = new float[bufferSize];
            bufferQ = new float[bufferSize];
        }

        public void Write(float[] inputSamplesI, float[] inputSamplesQ)
        {
            if (inputSamplesI.Length != inputSamplesQ.Length)
                throw new ArgumentException("inputSamplesI.Length must equals inputSamplesQ.Length");

            int remainedSpace = bufferSize - inputIndex;
            int inputLength = inputSamplesI.Length;
            if(remainedSpace >= inputLength)
            {
                inputSamplesI.FastCopyTo(bufferI, inputSamplesI.Length, 0, inputIndex);
                inputSamplesQ.FastCopyTo(bufferQ, inputSamplesQ.Length, 0, inputIndex);
                inputIndex += inputSamplesI.Length;
            }
            else
            {
                int copyLength = inputSamplesI.Length - remainedSpace;
                inputSamplesI.FastCopyTo(bufferI, remainedSpace, 0, inputIndex);
                inputSamplesQ.FastCopyTo(bufferQ, remainedSpace, 0, inputIndex);
                inputIndex = 0;
                inputSamplesI.FastCopyTo(bufferI, copyLength, remainedSpace, inputIndex);
                inputSamplesQ.FastCopyTo(bufferQ, copyLength, remainedSpace, inputIndex);
                inputIndex += copyLength;
            }
            availableDataCount += inputLength;
        }

        public void Read(float[] outputI, float[] outputQ, int startIndex = 0, int length = -1)
        {
            if (outputI.Length != outputQ.Length || length < outputI.Length || length > availableDataCount)
                return;

            if (length == -1)
                length = outputI.Length;

            int relativeIndex = outputIndex + startIndex;

            if(relativeIndex + length <= bufferSize - 1)
            {
                bufferI.FastCopyTo(outputI, length, relativeIndex, 0);
                bufferQ.FastCopyTo(outputQ, length, relativeIndex, 0);
                outputIndex += length;
            }
            else
            {
                int copyLength = bufferSize - outputIndex;
                int remainLength = length - copyLength;
                bufferI.FastCopyTo(outputI, copyLength, outputIndex, 0);
                bufferQ.FastCopyTo(outputQ, copyLength, outputIndex, 0);
                outputIndex = 0;
                bufferI.FastCopyTo(outputI, remainLength, outputIndex, copyLength);
                bufferQ.FastCopyTo(outputQ, remainLength, outputIndex, copyLength);
                outputIndex += remainLength;
            }
            availableDataCount -= length;
        }
    }
}
