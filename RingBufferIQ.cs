using NWaves.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        public int Length => availableDataCount;

        private int bufferSize = 0;
        public int Size => bufferSize;

        public Complex this[int index]
        {
            get
            {
                int indexOfArr = index + outputIndex;
                if (indexOfArr >= bufferSize)
                    indexOfArr -= bufferSize;

                if (index < availableDataCount)
                    return new Complex(bufferI[indexOfArr], bufferQ[indexOfArr]);
                else
                    throw new IndexOutOfRangeException();
            }
            set
            {
                int indexOfArr = index + outputIndex;
                if (indexOfArr >= bufferSize)
                    indexOfArr -= bufferSize;

                if (index < availableDataCount)
                {
                    float inputI = (float)value.Real;
                    float inputQ = (float)value.Imaginary;
                    bufferI[indexOfArr] = inputI;
                    bufferQ[indexOfArr] = inputQ;
                }
                else
                    throw new IndexOutOfRangeException();
            }
        }

        public RingBufferIQ(int size)
        {
            bufferSize = size;
            bufferI = new float[bufferSize];
            bufferQ = new float[bufferSize];
        }

        public void MoveOutputIndex(int delta)
        {
            if (delta < 0)
                throw new ArgumentException("'delta' must greater than zero");

            outputIndex += delta;

            if (outputIndex >= bufferSize)
            {
                outputIndex -= bufferSize;
            }

            UpdateDataCount();
        }

        public void UpdateDataCount()
        {
            if(inputIndex >= outputIndex)
            {
                availableDataCount = inputIndex - outputIndex + 1;
            }
            else
            {
                availableDataCount = bufferSize - (outputIndex - inputIndex) + 1;
            }
        }

        public void Write(float[] inputSamplesI, float[] inputSamplesQ, int length)
        {
            if (inputSamplesI.Length != inputSamplesQ.Length)
                throw new ArgumentException("inputSamplesI.Length must equals inputSamplesQ.Length");
            if (length > inputSamplesI.Length)
                throw new ArgumentException("The 'length' is to big.");

            int remainedSpace = bufferSize - inputIndex;
            int inputLength = length;
            if(remainedSpace >= inputLength)
            {
                inputSamplesI.FastCopyTo(bufferI, inputLength, 0, inputIndex);
                inputSamplesQ.FastCopyTo(bufferQ, inputLength, 0, inputIndex);
                inputIndex += inputLength;
            }
            else
            {
                int copyLength = inputLength - remainedSpace;
                inputSamplesI.FastCopyTo(bufferI, remainedSpace, 0, inputIndex);
                inputSamplesQ.FastCopyTo(bufferQ, remainedSpace, 0, inputIndex);
                inputIndex = 0;
                inputSamplesI.FastCopyTo(bufferI, copyLength, remainedSpace, inputIndex);
                inputSamplesQ.FastCopyTo(bufferQ, copyLength, remainedSpace, inputIndex);
                inputIndex += copyLength;
            }

            //UpdateDataCount();
            if(availableDataCount < bufferSize)
            {
                availableDataCount += inputLength;
            }

            if (availableDataCount > bufferSize)
            {
                availableDataCount = bufferSize;
            }
        }

        [Obsolete("Please use RingBufferIQ[index] instead")]
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
