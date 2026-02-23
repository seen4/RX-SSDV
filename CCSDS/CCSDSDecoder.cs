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
        private Viterbi.Viterbi viterbiDecoder0;
        private Viterbi.Viterbi viterbiDecoder1;
        private MDecoder mDecoder0;
        private MDecoder mDecoder1;
        private Deframer deframer0;
        private Deframer deframer1;
        private BitDelay delay;

        private bool useMDecode = false;

        private float[] inputBuffer;
        private float[] outputBuffer;

        private float[] hardDecisionBits;

        public const int DIGITAL_BUFFER_SIZE = 1024;

        public CCSDSDecoder(bool useMDecode)
        {
            this.useMDecode = useMDecode;
            InitProcessingFlow();
            CheckProcessOutputArr(DIGITAL_BUFFER_SIZE);
        }

        public void InitProcessingFlow()
        {
            viterbiDecoder0 = new Viterbi.Viterbi();
            viterbiDecoder1 = new Viterbi.Viterbi();
            mDecoder0 = new MDecoder();
            mDecoder1 = new MDecoder();
            deframer0 = new Deframer();
            deframer1 = new Deframer();

            delay = new BitDelay(1);
        }

        private void ConfigureOutput()
        {
            float[] temp = outputBuffer;
            outputBuffer = inputBuffer;
            inputBuffer = temp;
        }

        public static void HardDecision(float[] inputSamplesI, float[] inputSamplesQ, float[] outputBits, int inputSize = -1)
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
            
            HardDecision(inputSamplesI, inputSamplesQ, hardDecisionBits, inputSize);

            //Branch Normal
            int viterbiOutputSize0 = viterbiDecoder0.Process(inputSize, hardDecisionBits, outputBuffer); ConfigureOutput();
            if(useMDecode) { mDecoder0.Process(viterbiOutputSize0, inputBuffer, outputBuffer); ConfigureOutput(); }
            deframer0.Process(viterbiOutputSize0, inputBuffer, outputBuffer); ConfigureOutput();

            //Branch Delay
            delay.Process(inputSize, hardDecisionBits, outputBuffer); ConfigureOutput();
            int viterbiOutputSize1 = viterbiDecoder1.Process(inputSize, inputBuffer, outputBuffer); ConfigureOutput();
            if (useMDecode) { mDecoder1.Process(viterbiOutputSize1, inputBuffer, outputBuffer); ConfigureOutput(); }
            deframer1.Process(viterbiOutputSize1, inputBuffer, outputBuffer);

            //Output
            outputSize = viterbiOutputSize0;
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
