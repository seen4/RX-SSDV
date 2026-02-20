using RX_SSDV.CCSDS.Viterbi;
using RX_SSDV.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System;
using NWaves.Utils;

namespace RX_SSDV.CCSDS
{
    public class CCSDSDecoder
    {
        private Viterbi.Viterbi viterbiDecoder;
        private FrameSync sync;

        private float[] outBuffer1;
        private float[] outBuffer2;

        public CCSDSDecoder()
        {
            viterbiDecoder = new Viterbi.Viterbi();
            sync = new FrameSync();
        }

        public void HardDecision(float[] inputSamplesI, float[] inputSamplesQ, float[] outputBits, int inputSize = -1)
        {
            inputSize = inputSize == -1 ? inputSamplesI.Length : inputSize;
            if (inputSize <= 0)
            {
                throw new ArgumentException("'inputSize' must bigger than zero");
            }

            for (int i = 0; i < inputSize; i++)
            {
                outputBits[i] = inputSamplesI[i] > 0 ? 1 : 0;
            }
        }

        public void Process(float[] inputSamplesI, float[] inputSamplesQ, float[] outputBits, out int outputSize, int inputSize = -1)
        {
            inputSize = inputSize == -1 ? inputSamplesI.Length : inputSize;
            if (inputSize <= 0)
            {
                throw new ArgumentException("'inputSize' must bigger than zero");
            }

            CheckProcessOutputArr(inputSamplesI.Length);

            HardDecision(inputSamplesI, inputSamplesQ, outBuffer1, inputSize);

            outputSize = viterbiDecoder.Process(inputSize, outBuffer1, outBuffer2);
            sync.Process(outputSize, outBuffer2, outBuffer1);
            outBuffer2.FastCopyTo(outputBits, outputSize, 0, 0);

            //outputSize = 1;
        }

        /// <summary>
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(outBuffer1, arrSize))
            {
                outBuffer1 = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(outBuffer2, arrSize))
            {
                outBuffer2 = new float[arrSize];
            }
        }
    }
}
