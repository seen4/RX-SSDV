using NWaves.Utils;
using RX_SSDV.Base;
using RX_SSDV.CCSDS.Viterbi;
using RX_SSDV.Utils;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.CCSDS
{
    public class CCSDSDecoder
    {
        private Viterbi.Viterbi viterbiDecoder;
        private FrameSync sync;
        private MDecoder mDecoder;
        private Viterbi.Viterbi viterbiDecoderD;
        private FrameSync syncD;
        private MDecoder mDecoderD;
        private BitDelay delay;

        private bool useMDecode = false;

        private float[] inputBuffer;
        private float[] outputBuffer;

        private float[] hardDecisionBits;

        public CCSDSDecoder(bool useMDecode)
        {
            InitProcessingFlow();
            this.useMDecode = useMDecode;
        }

        public void InitProcessingFlow()
        {
            viterbiDecoder = new Viterbi.Viterbi();
            mDecoder = new MDecoder();
            viterbiDecoderD = new Viterbi.Viterbi();
            mDecoderD = new MDecoder();
            sync = new FrameSync();
            syncD = new FrameSync();

            delay = new BitDelay(1);
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

            //Branch Normal
            HardDecision(inputSamplesI, inputSamplesQ, hardDecisionBits, inputSize);

            int viterbiOutputSize = viterbiDecoder.Process(inputSize, hardDecisionBits, outputBuffer);
            ConfigureOutput();

            int mDecodeOutputSize = viterbiOutputSize;
            if(useMDecode)
            {
                mDecodeOutputSize = mDecoder.Process(viterbiOutputSize, inputBuffer, outputBuffer);
                ConfigureOutput();
            }

            sync.Process(mDecodeOutputSize, inputBuffer, outputBuffer);
            ConfigureOutput();

            //Branch Delay
            delay.Process(inputSize, hardDecisionBits, outputBits);
            ConfigureOutput();

            int viterbiOutputSizeD = viterbiDecoderD.Process(inputSize, inputBuffer, outputBuffer);
            ConfigureOutput();

            int mDecodeOutputSizeD = viterbiOutputSizeD;
            if (useMDecode)
            {
                mDecodeOutputSizeD = mDecoderD.Process(viterbiOutputSizeD, inputBuffer, outputBuffer);
                ConfigureOutput();
            }

            syncD.Process(mDecodeOutputSizeD, inputBuffer, outputBuffer);

            outputSize = mDecodeOutputSize;

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

            if (ArrayUtil.CheckNeedUpdate(hardDecisionBits, arrSize))
            {
                hardDecisionBits = new float[arrSize];
            }
        }
    }
}
