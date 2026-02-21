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
        private MDecoder mDecoder;
        private BitDelay delay;

        private bool useMDecode = false;

        private float[] inputBuffer;
        private float[] outputBuffer;

        public CCSDSDecoder(bool useMDecode)
        {
            InitProcessingFlow();
            this.useMDecode = useMDecode;
        }

        public void InitProcessingFlow()
        {
            viterbiDecoder = new Viterbi.Viterbi();
            mDecoder = new MDecoder();
            delay = new BitDelay(1);
            sync = new FrameSync();
        }

        private void ConfigureOutput()
        {
            float[] temp = outputBuffer;
            outputBuffer = inputBuffer;
            inputBuffer = temp;
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

            HardDecision(inputSamplesI, inputSamplesQ, outputBuffer, inputSize);
            ConfigureOutput();

            int viterbiOutputSize = viterbiDecoder.Process(inputSize, inputBuffer, outputBuffer);
            ConfigureOutput();

            int mDecodeOutputSize = viterbiOutputSize;
            if(useMDecode)
            {
                mDecodeOutputSize = mDecoder.Process(viterbiOutputSize, inputBuffer, outputBuffer);
                ConfigureOutput();
            }

            outputSize = mDecodeOutputSize;

            sync.Process(outputSize, inputBuffer, outputBuffer);
            //ConfigureOutput();

            outputBuffer.FastCopyTo(outputBits, outputSize, 0, 0);
        }

        /// <summary>
        /// Check output if arrays avalible, if not, init array(s) by 'arrSize'.
        /// </summary>
        /// <param name="arrSize">Array size</param>
        public void CheckProcessOutputArr(int arrSize)
        {
            if (ArrayUtil.CheckNeedUpdate(outputBuffer, arrSize))
            {
                outputBuffer = new float[arrSize];
            }

            if (ArrayUtil.CheckNeedUpdate(inputBuffer, arrSize))
            {
                inputBuffer = new float[arrSize];
            }
        }
    }
}
